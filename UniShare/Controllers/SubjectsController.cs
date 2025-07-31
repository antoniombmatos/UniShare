using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;

namespace UniShare.Controllers
{
    [Authorize]
    public class SubjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Subjects
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CourseId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var subjects = await _context.Subjects
                .Include(s => s.Course)
                .Include(s => s.Professor)
                .Where(s => s.CourseId == user.CourseId && s.IsActive)
                .OrderBy(s => s.Year)
                .ThenBy(s => s.Semester)
                .ThenBy(s => s.Name)
                .ToListAsync();

            var enrolledSubjectIds = await _context.SubjectEnrollments
                .Where(e => e.UserId == user.Id)
                .Select(e => e.SubjectId)
                .ToListAsync();

            ViewBag.EnrolledSubjectIds = enrolledSubjectIds;
            return View(subjects);
        }

        // GET: Subjects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var subject = await _context.Subjects
                .Include(s => s.Course)
                .Include(s => s.Professor)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            // Check if user is enrolled in this subject
            var isEnrolled = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user!.Id && e.SubjectId == id);

            if (!isEnrolled)
            {
                return RedirectToAction("Index");
            }

            // Get posts for this subject
            var posts = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
                .Where(p => p.SubjectId == id && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Posts = posts;
            ViewBag.IsEnrolled = isEnrolled;

            return View(subject);
        }

        // POST: Subjects/Enroll/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null || user == null)
            {
                return NotFound();
            }

            // Check if user's course matches subject's course
            if (subject.CourseId != user.CourseId)
            {
                return Forbid();
            }

            // Check if already enrolled
            var existingEnrollment = await _context.SubjectEnrollments
                .FirstOrDefaultAsync(e => e.UserId == user.Id && e.SubjectId == id);

            if (existingEnrollment == null)
            {
                var enrollment = new SubjectEnrollment
                {
                    UserId = user.Id,
                    SubjectId = id,
                    EnrollmentDate = DateTime.UtcNow
                };

                _context.SubjectEnrollments.Add(enrollment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // POST: Subjects/Unenroll/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unenroll(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var enrollment = await _context.SubjectEnrollments
                .FirstOrDefaultAsync(e => e.UserId == user!.Id && e.SubjectId == id);

            if (enrollment != null)
            {
                _context.SubjectEnrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // POST: Subjects/CreatePost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(int subjectId, string content, PostType type, string? linkUrl, IFormFile? file)
        {
            var user = await _userManager.GetUserAsync(User);
            
            // Verify user is enrolled in subject
            var isEnrolled = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user!.Id && e.SubjectId == subjectId);

            if (!isEnrolled)
            {
                return Forbid();
            }

            var post = new Post
            {
                Content = content,
                Type = type,
                SubjectId = subjectId,
                AuthorId = user!.Id,
                LinkUrl = linkUrl
            };

            // Handle file upload
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                post.FilePath = "/uploads/" + uniqueFileName;
                post.FileName = file.FileName;
                post.FileSize = (file.Length / 1024).ToString() + " KB";
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = subjectId });
        }

        // POST: Subjects/CreateComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateComment(int postId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            var post = await _context.Posts
                .Include(p => p.Subject)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return NotFound();

            // Verify user is enrolled in subject
            var isEnrolled = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user!.Id && e.SubjectId == post.SubjectId);

            if (!isEnrolled)
            {
                return Forbid();
            }

            var comment = new Comment
            {
                Content = content,
                PostId = postId,
                AuthorId = user!.Id
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = post.SubjectId });
        }

        // GET: Subjects/MySubjects
        public async Task<IActionResult> MySubjects()
        {
            var user = await _userManager.GetUserAsync(User);
            
            var enrollments = await _context.SubjectEnrollments
                .Include(e => e.Subject)
                    .ThenInclude(s => s.Course)
                .Where(e => e.UserId == user!.Id)
                .OrderBy(e => e.Subject.Year)
                .ThenBy(e => e.Subject.Semester)
                .ThenBy(e => e.Subject.Name)
                .ToListAsync();

            return View(enrollments);
        }

        // POST: Subjects/CompleteSubject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSubject(int enrollmentId, decimal grade)
        {
            var user = await _userManager.GetUserAsync(User);
            var enrollment = await _context.SubjectEnrollments
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.UserId == user!.Id);

            if (enrollment != null)
            {
                enrollment.IsCompleted = true;
                enrollment.Grade = grade;
                enrollment.CompletionDate = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MySubjects");
        }
    }
}