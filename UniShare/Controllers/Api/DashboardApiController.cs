using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;

namespace UniShare.Controllers.Api
{
    /// <summary>
    /// Controlador de API para o dashboard do utilizador.
    /// </summary>
    
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrador")]
    public class DashboardApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Obtém o progresso do utilizador no dashboard, incluindo total de disciplinas, disciplinas concluídas, média de notas e ECTS concluídos.
        /// </summary>
        /// <returns></returns>
        
        [HttpGet("my-progress")]
        public async Task<IActionResult> GetProgress()
        {
            var user = await _userManager.GetUserAsync(User);

            var enrollments = await _context.SubjectEnrollments
                .Include(e => e.Subject)
                .Where(e => e.UserId == user!.Id)
                .ToListAsync();

            var completed = enrollments.Where(e => e.IsCompleted).ToList();
            var totalECTS = completed.Sum(e => e.Subject.ECTS);

            var avg = completed.Any() ? completed.Average(e => e.Grade ?? 0) : 0;

            return Ok(new
            {
                TotalSubjects = enrollments.Count,
                CompletedSubjects = completed.Count,
                AverageGrade = Math.Round(avg, 2),
                CompletedECTS = totalECTS
            });
        }
    }
}
