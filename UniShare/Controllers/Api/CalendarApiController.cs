using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;
using System.Security.Claims;

namespace UniShare.Controllers.Api
{
    /// <summary>
    /// Controlador de API para o calendário do utilizador.
    /// </summary>
    
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrador")]
    public class CalendarApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CalendarApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Obtém as entradas do calendário do utilizador autenticado.
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        public async Task<IActionResult> GetMyCalendar()
        {
            var user = await _userManager.GetUserAsync(User);

            var entries = await _context.CalendarEntries
                .Include(e => e.Subject)
                .Where(e => e.UserId == user!.Id && e.IsActive)
                .OrderBy(e => e.DateTime)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.Type,
                    e.DateTime,
                    Subject = e.Subject != null ? new { e.Subject.Id, e.Subject.Name } : null
                })
                .ToListAsync();

            return Ok(entries);
        }

        /// <summary>
        /// Cria uma nova entrada no calendário do utilizador.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        
        [HttpPost]
        public async Task<IActionResult> CreateEntry([FromBody] CreateCalendarEntryRequest request)
        {
            var user = await _userManager.GetUserAsync(User);

            if (request.SubjectId.HasValue)
            {
                var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == request.SubjectId.Value);
                if (!subjectExists)
                    return BadRequest("Disciplina inválida.");
            }

            var entry = new CalendarEntry
            {
                UserId = user!.Id,
                SubjectId = request.SubjectId,
                Title = request.Title,
                Description = request.Description,
                DateTime = request.DateTime,
                Type = request.Type
            };

            _context.CalendarEntries.Add(entry);
            await _context.SaveChangesAsync();

            return Ok(new { entry.Id });
        }

        /// <summary>
        /// Atualiza uma entrada do calendário do utilizador.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEntry(int id, [FromBody] CreateCalendarEntryRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            var entry = await _context.CalendarEntries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == user!.Id);

            if (entry == null) return NotFound();

            entry.SubjectId = request.SubjectId;
            entry.Title = request.Title;
            entry.Description = request.Description;
            entry.DateTime = request.DateTime;
            entry.Type = request.Type;

            await _context.SaveChangesAsync();

            return Ok(new { entry.Id });
        }

        /// <summary>
        /// Remove uma entrada do calendário do utilizador.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntry(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var entry = await _context.CalendarEntries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == user!.Id);

            if (entry == null) return NotFound();

            entry.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Entrada removida." });
        }
    }

    // DTO reutilizável para criar/editar entradas
    public class CreateCalendarEntryRequest
    {
        public int? SubjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public CalendarEntryType Type { get; set; }
        public DateTime DateTime { get; set; }
    }
}
