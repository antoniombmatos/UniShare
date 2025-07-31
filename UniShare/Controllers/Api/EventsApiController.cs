using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;

namespace UniShare.Controllers.Api
{

    /// <summary>
    /// Controlador de API para eventos (events).
    /// </summary>
    
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Obtém a lista de eventos ativos, incluindo criador, curso (se aplicável) e contagem de participantes.
        /// </summary>
        /// <returns></returns>
        /// 
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .Include(e => e.Creator)
                .Include(e => e.Course)
                .Include(e => e.Attendees)
                .Where(e => e.IsActive && e.Status == EventStatus.Approved)
                .OrderBy(e => e.StartDateTime)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.Location,
                    e.StartDateTime,
                    e.EndDateTime,
                    Creator = new { e.Creator.FullName },
                    Course = e.Course != null ? new { e.Course.Id, e.Course.Name } : null,
                    AttendeeCount = e.Attendees.Count
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Cria um novo evento.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            var user = await _userManager.GetUserAsync(User);

            var ev = new Event
            {
                Title = request.Title,
                Description = request.Description,
                Location = request.Location,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                CourseId = request.CourseId,
                CreatorId = user!.Id,
                Status = EventStatus.Pending
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return Ok(new { ev.Id });
        }

        /// <summary>
        /// Aprova um evento pendente.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        
        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            ev.Status = EventStatus.Approved;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Evento aprovado." });
        }

        /// <summary>
        /// Regista a presença de um utilizador num evento específico.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        
        [HttpPost("{id}/attend")]
        public async Task<IActionResult> AttendEvent(int id, [FromBody] AttendEventRequest request)
        {
            var user = await _userManager.GetUserAsync(User);

            var exists = await _context.EventAttendees
                .AnyAsync(ea => ea.EventId == id && ea.UserId == user!.Id);

            if (exists)
                return BadRequest("Já tens presença registada.");

            var attendee = new EventAttendee
            {
                EventId = id,
                UserId = user.Id,
                Status = request.Status
            };

            _context.EventAttendees.Add(attendee);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Presença registada." });
        }

        public class CreateEventRequest
        {
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? Location { get; set; }
            public DateTime StartDateTime { get; set; }
            public DateTime EndDateTime { get; set; }
            public int? CourseId { get; set; }
        }

        public class AttendEventRequest
        {
            public AttendanceStatus Status { get; set; } = AttendanceStatus.Interested;
        }
    }
}
