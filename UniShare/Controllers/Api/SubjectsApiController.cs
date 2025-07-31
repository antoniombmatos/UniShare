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
    public class SubjectsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubjectsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ========== SUBJECT ENROLLMENT ==========
        [HttpGet("enrolled")]
        public async Task<IActionResult> GetEnrolledSubjects()
        {
            var user = await _userManager.GetUserAsync(User);

            var subjects = await _context.SubjectEnrollments
                .Include(e => e.Subject)
                    .ThenInclude(s => s.Course)
                .Where(e => e.UserId == user!.Id)
                .Select(e => new
                {
                    e.Subject.Id,
                    e.Subject.Name,
                    e.Subject.Code,
                    Course = e.Subject.Course.Name,
                    e.IsCompleted,
                    e.Grade
                })
                .ToListAsync();

            return Ok(subjects);
        }

        [HttpPost("enroll")]
        public async Task<IActionResult> EnrollInSubject([FromBody] EnrollRequest request)
        {
            var user = await _userManager.GetUserAsync(User);

            var subject = await _context.Subjects
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.Id == request.SubjectId);

            if (subject == null)
                return NotFound("Disciplina não encontrada.");

            if (subject.CourseId != user!.CourseId)
                return Forbid();

            var alreadyEnrolled = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user.Id && e.SubjectId == request.SubjectId);

            if (alreadyEnrolled)
                return BadRequest("Já está inscrito nesta disciplina.");

            var enrollment = new SubjectEnrollment
            {
                UserId = user.Id,
                SubjectId = request.SubjectId
            };

            _context.SubjectEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inscrição efetuada com sucesso." });
        }

        [HttpPatch("enrollment/{id}/complete")]
        public async Task<IActionResult> CompleteSubject(int id, [FromBody] CompleteRequest request)
        {
            var enrollment = await _context.SubjectEnrollments.FindAsync(id);
            if (enrollment == null)
                return NotFound();

            enrollment.Grade = request.Grade;
            enrollment.IsCompleted = true;
            enrollment.CompletionDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Disciplina marcada como concluída." });
        }

        // ========== POSTS ==========
        [HttpGet("{subjectId}/posts")]
        public async Task<IActionResult> GetPosts(int subjectId, int page = 1, int pageSize = 10)
        {
            var user = await _userManager.GetUserAsync(User);

            var isEnrolled = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user!.Id && e.SubjectId == subjectId);

            if (!isEnrolled)
                return Forbid();

            var posts = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Comments.Where(c => c.IsActive))
                    .ThenInclude(c => c.Author)
                .Where(p => p.SubjectId == subjectId && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Content,
                    p.Type,
                    p.FilePath,
                    p.FileName,
                    p.LinkUrl,
                    p.CreatedAt,
                    Author = new { p.Author.FullName, p.Author.Id },
                    Comments = p.Comments.Select(c => new
                    {
                        c.Id,
                        c.Content,
                        c.CreatedAt,
                        Author = new { c.Author.FullName, c.Author.Id }
                    })
                })
                .ToListAsync();

            return Ok(posts);
        }

        [HttpPost("{subjectId}/posts")]
        public async Task<IActionResult> CreatePost(int subjectId, [FromBody] CreatePostRequest request)
        {
            var user = await _userManager.GetUserAsync(User);

            var isEnrolled = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user!.Id && e.SubjectId == subjectId);

            if (!isEnrolled)
                return Forbid();

            var post = new Post
            {
                Content = request.Content,
                Type = request.Type,
                SubjectId = subjectId,
                AuthorId = user!.Id,
                LinkUrl = request.LinkUrl
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var createdPost = await _context.Posts
                .Include(p => p.Author)
                .FirstOrDefaultAsync(p => p.Id == post.Id);

            return Ok(new
            {
                createdPost!.Id,
                createdPost.Content,
                createdPost.Type,
                createdPost.CreatedAt,
                Author = new { createdPost.Author.FullName, createdPost.Author.Id },
                Comments = new List<object>()
            });
        }

        [HttpPost("posts/{postId}/comments")]
        public async Task<IActionResult> CreateComment(int postId, [FromBody] CreateCommentRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            var post = await _context.Posts
                .Include(p => p.Subject)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return NotFound();

            var isEnrolled = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user!.Id && e.SubjectId == post.SubjectId);

            if (!isEnrolled)
                return Forbid();

            var comment = new Comment
            {
                Content = request.Content,
                PostId = postId,
                AuthorId = user.Id
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var createdComment = await _context.Comments
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == comment.Id);

            return Ok(new
            {
                createdComment!.Id,
                createdComment.Content,
                createdComment.CreatedAt,
                Author = new { createdComment.Author.FullName, createdComment.Author.Id }
            });
        }

        // ========== DTOs ==========
        public class CreatePostRequest
        {
            public string Content { get; set; } = string.Empty;
            public PostType Type { get; set; }
            public string? LinkUrl { get; set; }
        }

        public class CreateCommentRequest
        {
            public string Content { get; set; } = string.Empty;
        }

        public class EnrollRequest
        {
            public int SubjectId { get; set; }
        }

        public class CompleteRequest
        {
            public decimal Grade { get; set; }
        }
    }
}
