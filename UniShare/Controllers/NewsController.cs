using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models; // Supondo que tens um modelo News
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace UniShare.Controllers
{
    [Authorize] // Se quiseres que só usuários logados possam criar notícias
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /News
        public async Task<IActionResult> Index()
        {
            var news = await _context.News
                .Include(n => n.Course)
                .Include(n => n.Author) // Caso tenhas o campo AuthorId e relacionamento
                .OrderByDescending(n => n.PublicationDate)
                .ToListAsync();

            return View(news);
        }

        // GET: /News/Create
        public async Task<IActionResult> Create()
        {
            // Preenche ViewBag com lista de cursos ativos para dropdown
            ViewBag.Courses = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.Courses
                    .Where(c => c.IsActive)
                    .ToListAsync(),
                "Id",
                "Name"
            );

            return View();
        }

        // POST: /News/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(News news)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (ModelState.IsValid)
            {
                news.AuthorId = user.Id;
                news.PublicationDate = DateTime.UtcNow;
                news.IsActive = true;

                _context.News.Add(news);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Notícia criada com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            // Se falhar validação, carrega os cursos novamente para o dropdown
            ViewBag.Courses = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.Courses
                    .Where(c => c.IsActive)
                    .ToListAsync(),
                "Id",
                "Name",
                news.CourseId
            );

            return View(news);
        }
    }
}
