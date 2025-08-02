using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;
using System.Text.RegularExpressions;

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

        // 🔹 MÉTODOS AUXILIARES PRIVADOS

        private async Task<ApplicationUser?> GetCurrentUserAsync() => await _userManager.GetUserAsync(User);

        private async Task<bool> IsUserEnrolledAsync(string userId, int subjectId) =>
            await _context.SubjectEnrollments.AnyAsync(e => e.UserId == userId && e.SubjectId == subjectId);

        private string GetSafeFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            return Guid.NewGuid().ToString() + extension;
        }

        // GET: Subjects
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user?.CourseId == null)
                return RedirectToAction("Index", "Home");

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

            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var subject = await _context.Subjects
                .Include(s => s.Course)
                .Include(s => s.Professor)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            bool isEnrolled = await IsUserEnrolledAsync(user.Id, subject.Id);
            if (!isEnrolled)
                return RedirectToAction("Index");

            var posts = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Comments).ThenInclude(c => c.Author)
                .Where(p => p.SubjectId == id && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var professorDocuments = await _context.Posts
                .Include(p => p.Author)
                .Where(p => p.SubjectId == id && p.Type == PostType.Document && p.Author != null)
                .ToListAsync();

            // Filtra documentos dos professores
            professorDocuments = professorDocuments
                .Where(p => _userManager.IsInRoleAsync(p.Author!, "Professor").Result)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            ViewBag.ProfessorDocuments = professorDocuments;
            ViewBag.Subject = subject;
            ViewBag.Posts = posts;
            ViewBag.IsEnrolled = isEnrolled;

            return View(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int id)
        {
            var user = await GetCurrentUserAsync();
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null || user == null) return NotFound();
            if (subject.CourseId != user.CourseId) return Forbid();

            bool alreadyEnrolled = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user.Id && e.SubjectId == id);

            if (!alreadyEnrolled)
            {
                _context.SubjectEnrollments.Add(new SubjectEnrollment
                {
                    UserId = user.Id,
                    SubjectId = id,
                    EnrollmentDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Inscrição realizada com sucesso!";
            }
            else
            {
                TempData["Info"] = "Já está inscrito nesta disciplina.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unenroll(int id)
        {
            var user = await GetCurrentUserAsync();
            var enrollment = await _context.SubjectEnrollments
                .FirstOrDefaultAsync(e => e.UserId == user!.Id && e.SubjectId == id);

            if (enrollment != null)
            {
                _context.SubjectEnrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Desinscrição concluída.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(int subjectId, string content, PostType type, string? linkUrl, IFormFile? file)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(content) && file == null && string.IsNullOrWhiteSpace(linkUrl))
            {
                TempData["Error"] = "O post não pode estar vazio.";
                return RedirectToAction("Details", new { id = subjectId });
            }

            if (!await IsUserEnrolledAsync(user.Id, subjectId)) return Forbid();

            var post = new Post
            {
                Content = content.Trim(),
                Type = type,
                SubjectId = subjectId,
                AuthorId = user.Id,
                LinkUrl = linkUrl
            };

            if (file != null && file.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsFolder);

                string safeFileName = GetSafeFileName(file.FileName);
                string filePath = Path.Combine(uploadsFolder, safeFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(fileStream);

                post.FilePath = "/uploads/" + safeFileName;
                post.FileName = Path.GetFileName(file.FileName);
                post.FileSize = $"{file.Length / 1024} KB";
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Post criado com sucesso!";
            return RedirectToAction("Details", new { id = subjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateComment(int postId, string content)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "O comentário não pode estar vazio.";
                return RedirectToAction("Details", new { id = postId });
            }

            var post = await _context.Posts.Include(p => p.Subject).FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null) return NotFound();

            if (!await IsUserEnrolledAsync(user.Id, post.SubjectId)) return Forbid();

            _context.Comments.Add(new Comment
            {
                Content = content.Trim(),
                PostId = postId,
                AuthorId = user.Id
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Comentário adicionado!";
            return RedirectToAction("Details", new { id = post.SubjectId });
        }

        public async Task<IActionResult> MySubjects()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var enrollments = await _context.SubjectEnrollments
                .Include(e => e.Subject).ThenInclude(s => s.Course)
                .Where(e => e.UserId == user.Id)
                .OrderBy(e => e.Subject.Year)
                .ThenBy(e => e.Subject.Semester)
                .ThenBy(e => e.Subject.Name)
                .ToListAsync();

            return View(enrollments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSubject(int enrollmentId, decimal grade)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            if (grade < 0 || grade > 20)
            {
                TempData["Error"] = "Nota inválida. Deve estar entre 0 e 20.";
                return RedirectToAction("MySubjects");
            }

            var enrollment = await _context.SubjectEnrollments
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.UserId == user.Id);

            if (enrollment != null)
            {
                enrollment.IsCompleted = true;
                enrollment.Grade = grade;
                enrollment.CompletionDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Disciplina concluída!";
            }

            return RedirectToAction("MySubjects");
        }

        [Authorize(Roles = "Professor")]
        public async Task<IActionResult> TaughtSubjects()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var subjects = await _context.Subjects
                .Include(s => s.Course)
                .Where(s => s.ProfessorId == user.Id && s.IsActive)
                .OrderBy(s => s.Year)
                .ThenBy(s => s.Semester)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return View(subjects);
        }

        [Authorize(Roles = "Professor,Administrador")] // Opcional: só quem pode criar
        [HttpGet]
        public IActionResult Create()
        {
            return View(); // Vai procurar Views/Subjects/Create.cshtml
        }

        [Authorize(Roles = "Professor,Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Subject subject)
        {
            if (ModelState.IsValid)
            {
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(subject);
        }
    }
}
