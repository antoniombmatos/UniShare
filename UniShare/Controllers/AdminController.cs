using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;

namespace UniShare.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Courses Management
        public async Task<IActionResult> Courses()
        {
            var courses = await _context.Courses
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(courses);
        }

        /// <summary>
        /// Exibe a página para criar um novo curso.
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        public IActionResult CreateCourse()
        {
            return View();
        }

        /// <summary>
        /// Cria um novo curso com os dados fornecidos.
        /// </summary>
        /// <param name="course"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction("Courses");
            }
            return View(course);
        }

        /// <summary>
        /// Exibe a página para editar um curso existente.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpGet]
        public async Task<IActionResult> EditCourse(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            return View(course);
        }

        /// <summary>
        /// Atualiza os dados de um curso existente com os dados fornecidos.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="course"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, Course course)
        {
            if (id != course.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction("Courses");
            }
            return View(course);
        }

        /// <summary>
        /// Desativa um curso existente, marcando-o como inativo.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                course.IsActive = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Courses");
        }

        // Subjects Management
        public async Task<IActionResult> Subjects()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Course)
                .Include(s => s.Professor)
                .OrderBy(s => s.Course.Name)
                .ThenBy(s => s.Year)
                .ThenBy(s => s.Semester)
                .ThenBy(s => s.Name)
                .ToListAsync();
            return View(subjects);
        }

        /// <summary>
        /// Exibe a página para criar uma nova disciplina.
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        public async Task<IActionResult> CreateSubject()
        {
            ViewBag.Courses = new SelectList(await _context.Courses.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
            ViewBag.Professors = new SelectList(await GetProfessorsAsync(), "Id", "FullName");
            return View();
        }

        /// <summary>
        /// Cria uma nova disciplina com os dados fornecidos.
        /// </summary>
        /// <param name="subject"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubject(Subject subject)
        {
            if (ModelState.IsValid)
            {
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();
                return RedirectToAction("Subjects");
            }

            ViewBag.Courses = new SelectList(await _context.Courses.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
            ViewBag.Professors = new SelectList(await GetProfessorsAsync(), "Id", "FullName");
            return View(subject);
        }

        /// <summary>
        /// Exibe a página para editar uma disciplina existente.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpGet]
        public async Task<IActionResult> EditSubject(int? id)
        {
            if (id == null) return NotFound();

            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();

            ViewBag.Courses = new SelectList(await _context.Courses.Where(c => c.IsActive).ToListAsync(), "Id", "Name", subject.CourseId);
            ViewBag.Professors = new SelectList(await GetProfessorsAsync(), "Id", "FullName", subject.ProfessorId);
            return View(subject);
        }

        /// <summary>
        /// Atualiza os dados de uma disciplina existente com os dados fornecidos.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="subject"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubject(int id, Subject subject)
        {
            if (id != subject.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(subject);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubjectExists(subject.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction("Subjects");
            }

            ViewBag.Courses = new SelectList(await _context.Courses.Where(c => c.IsActive).ToListAsync(), "Id", "Name", subject.CourseId);
            ViewBag.Professors = new SelectList(await GetProfessorsAsync(), "Id", "FullName", subject.ProfessorId);
            return View(subject);
        }

        // Users Management
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.Course)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var usersWithRoles = new List<(ApplicationUser User, IList<string> Roles)>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add((user, roles));
            }

            return View(usersWithRoles);
        }

        /// <summary>
        /// Exibe a página para editar um utilizador existente.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpGet]
        public async Task<IActionResult> EditUser(string? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            ViewBag.Courses = new SelectList(await _context.Courses.Where(c => c.IsActive).ToListAsync(), "Id", "Name", user.CourseId);
            ViewBag.AllRoles = allRoles;
            ViewBag.UserRoles = userRoles;

            return View(user);
        }

        /// <summary>
        /// Atualiza os dados de um utilizador existente com os dados fornecidos, incluindo as funções selecionadas.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <param name="selectedRoles"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, ApplicationUser model, List<string> selectedRoles)
        {
            if (id != model.Id) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.StudentNumber = model.StudentNumber;
            user.CourseId = model.CourseId;
            user.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Update roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                
                if (selectedRoles != null && selectedRoles.Any())
                {
                    await _userManager.AddToRolesAsync(user, selectedRoles);
                }

                return RedirectToAction("Users");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.Courses = new SelectList(await _context.Courses.Where(c => c.IsActive).ToListAsync(), "Id", "Name", user.CourseId);
            ViewBag.AllRoles = await _roleManager.Roles.ToListAsync();
            ViewBag.UserRoles = await _userManager.GetRolesAsync(user);

            return View(model);
        }

        // Events Management
        public async Task<IActionResult> Events()
        {
            var events = await _context.Events
                .Include(e => e.Creator)
                .Include(e => e.Course)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            return View(events);
        }

        /// <summary>
        /// Exibe a página para criar um novo evento.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveEvent(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem != null)
            {
                eventItem.Status = EventStatus.Approved;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Events");
        }

        /// <summary>
        /// Rejeita um evento existente, marcando-o como rejeitado.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectEvent(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem != null)
            {
                eventItem.Status = EventStatus.Rejected;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Events");
        }

        private async Task<List<ApplicationUser>> GetProfessorsAsync()
        {
            var professors = new List<ApplicationUser>();
            var users = await _userManager.Users.ToListAsync();
            
            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "Professor"))
                {
                    professors.Add(user);
                }
            }
            
            return professors;
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        private bool SubjectExists(int id)
        {
            return _context.Subjects.Any(e => e.Id == id);
        }

        // GET: Admin/News
        public async Task<IActionResult> News()
        {
            var news = await _context.News
                .Include(n => n.Author)
                .Include(n => n.Course)
                .OrderByDescending(n => n.PublicationDate)
                .ToListAsync();

            return View(news);
        }

        // GET: Admin/CreateNews
        [HttpGet]
        public async Task<IActionResult> CreateNews()
        {
            ViewBag.Courses = new SelectList(await _context.Courses.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Admin/CreateNews
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNews(News news)
        {
            var user = await _userManager.GetUserAsync(User);
            if (ModelState.IsValid)
            {
                news.AuthorId = user!.Id;
                news.PublicationDate = DateTime.UtcNow;
                news.IsActive = true;

                _context.News.Add(news);
                await _context.SaveChangesAsync();
                return RedirectToAction("News");
            }

            ViewBag.Courses = new SelectList(await _context.Courses.Where(c => c.IsActive).ToListAsync(), "Id", "Name", news.CourseId);
            return View(news);
        }

    }
}