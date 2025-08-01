using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using UniShare.Data;
using UniShare.Models;

namespace UniShare.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return View("Landing");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var userRoles = await _userManager.GetRolesAsync(user);

            ViewBag.UserName = user.FullName;
            ViewBag.IsStudent = userRoles.Contains("Aluno");
            ViewBag.IsProfessor = userRoles.Contains("Professor");
            ViewBag.IsAdmin = userRoles.Contains("Administrador");

            if (ViewBag.IsStudent)
            {
                var enrollments = await _context.SubjectEnrollments
                    .Include(e => e.Subject)
                    .Where(e => e.UserId == user.Id && e.Subject.IsActive)
                    .ToListAsync();

                var completedECTS = enrollments.Where(e => e.IsCompleted).Sum(e => e.Subject.ECTS);
                var averageGrade = enrollments.Where(e => e.Grade.HasValue).Average(e => e.Grade) ?? 0;
                var totalECTS = user.Course?.TotalECTS ?? 180;

                ViewBag.EnrolledSubjects = enrollments.Count;
                ViewBag.CompletedECTS = completedECTS;
                ViewBag.TotalECTS = totalECTS;
                ViewBag.AverageGrade = Math.Round(averageGrade, 2);
                ViewBag.ProgressPercentage = totalECTS > 0 ? (completedECTS * 100 / totalECTS) : 0;

                ViewBag.RecentPosts = await _context.Posts
                    .Include(p => p.Author)
                    .Include(p => p.Subject)
                    .Where(p => enrollments.Select(e => e.SubjectId).Contains(p.SubjectId) && p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                ViewBag.UpcomingEvents = await _context.CalendarEntries
                    .Include(c => c.Subject)
                    .Where(c => c.UserId == user.Id && c.DateTime > DateTime.Now && c.IsActive)
                    .OrderBy(c => c.DateTime)
                    .Take(5)
                    .ToListAsync();
            }

            if (ViewBag.IsProfessor)
            {
                ViewBag.ProfessorSubjects = await _context.Subjects
                    .Where(s => s.ProfessorId == user.Id && s.IsActive)
                    .ToListAsync();

                ViewBag.UpcomingEvents = await _context.CalendarEntries
                    .Where(e => e.UserId == user.Id && e.DateTime > DateTime.Now && e.IsActive)
                    .OrderBy(e => e.DateTime)
                    .Take(5)
                    .ToListAsync();
            }

            if (ViewBag.IsAdmin)
            {
                ViewBag.TotalUsers = await _context.Users.CountAsync();
                ViewBag.TotalCourses = await _context.Courses.CountAsync();
                ViewBag.TotalSubjects = await _context.Subjects.CountAsync();
                ViewBag.TotalNews = await _context.News.CountAsync();
                ViewBag.RecentNews = await _context.News
                    .OrderByDescending(n => n.PublicationDate)
                    .Take(5)
                    .ToListAsync();
            }

            ViewBag.News = await _context.News
                .Include(n => n.Course)
                .Where(n => n.IsActive && (n.CourseId == null || n.CourseId == user.CourseId))
                .OrderByDescending(n => n.PublicationDate)
                .Take(3)
                .ToListAsync();

            return View();
        }

        /// <summary>
        /// Displays the user profile page with course information if available.
        /// </summary>
        /// <returns></returns>

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var userWithCourse = await _context.Users
                .Include(u => u.Course)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            return View(userWithCourse);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Handles errors and displays the error view with the request ID for debugging purposes.
        /// </summary>
        /// <returns></returns>

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}