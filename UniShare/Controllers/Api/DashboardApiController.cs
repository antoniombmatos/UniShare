using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;

namespace UniShare.Controllers.Api
{
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
        /// Obtém dados do dashboard do utilizador, incluindo progresso, total de disciplinas, utilizadores e entradas no calendário.
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            var user = await _userManager.GetUserAsync(User);

            // Progresso do utilizador
            var enrollments = await _context.SubjectEnrollments
                .Include(e => e.Subject)
                .Where(e => e.UserId == user!.Id)
                .ToListAsync();

            var completed = enrollments.Where(e => e.IsCompleted).ToList();
            var totalECTS = completed.Sum(e => e.Subject.ECTS);
            var avgGrade = completed.Any() ? completed.Average(e => e.Grade ?? 0) : 0;

            // Total de disciplinas na plataforma
            var totalSubjects = await _context.Subjects.CountAsync();

            // Total de utilizadores (exemplo)
            var totalUsers = await _context.Users.CountAsync();

            // Total de entradas ativas no calendário (exemplo)
            var totalCalendarEntries = await _context.CalendarEntries.CountAsync(e => e.IsActive);

            return Ok(new
            {
                UserProgress = new
                {
                    TotalSubjects = enrollments.Count,
                    CompletedSubjects = completed.Count,
                    AverageGrade = Math.Round(avgGrade, 2),
                    CompletedECTS = totalECTS
                },
                TotalSubjects = totalSubjects,
                TotalUsers = totalUsers,
                TotalCalendarEntries = totalCalendarEntries
            });
        }
    }
}
