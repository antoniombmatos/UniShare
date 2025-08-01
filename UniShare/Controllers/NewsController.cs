using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;

namespace UniShare.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var news = await _context.News
                .Include(n => n.Course)
                .OrderByDescending(n => n.PublicationDate)
                .ToListAsync();

            return View(news);
        }
    }
}
