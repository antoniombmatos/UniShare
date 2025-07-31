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
    [Authorize]
    public class NewsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NewsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetNews()
        {
            var news = await _context.News
                .Include(n => n.Author)
                .Include(n => n.Course)
                .Where(n => n.IsActive)
                .OrderByDescending(n => n.PublicationDate)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Content,
                    n.PublicationDate,
                    Author = new { n.Author.FullName, n.Author.Id },
                    Course = n.Course != null ? new { n.Course.Id, n.Course.Name } : null
                })
                .ToListAsync();

            return Ok(news);
        }

        [HttpPost]
        [Authorize(Roles = "Professor,Admin")]
        public async Task<IActionResult> CreateNews([FromBody] CreateNewsRequest request)
        {
            var user = await _userManager.GetUserAsync(User);

            var news = new News
            {
                Title = request.Title,
                Content = request.Content,
                CourseId = request.CourseId,
                AuthorId = user!.Id,
                PublicationDate = DateTime.UtcNow,
                IsFeatured = request.IsFeatured
            };

            _context.News.Add(news);
            await _context.SaveChangesAsync();

            return Ok(new { news.Id });
        }

        public class CreateNewsRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public int? CourseId { get; set; }
            public bool IsFeatured { get; set; } = false;
        }
    }
}
