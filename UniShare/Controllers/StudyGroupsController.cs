using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;

namespace UniShare.Controllers
{
    [Authorize]
    public class StudyGroupsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudyGroupsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: StudyGroups
        public async Task<IActionResult> Index(int? subjectId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); // Caso o utilizador não esteja autenticado corretamente

            IQueryable<StudyGroup> query = _context.StudyGroups
                .Include(sg => sg.Subject)
                .Include(sg => sg.Creator)
                .Include(sg => sg.Members)
                .Where(sg => sg.IsActive);

            if (subjectId.HasValue)
            {
                query = query.Where(sg => sg.SubjectId == subjectId.Value);
                ViewBag.SubjectId = subjectId.Value;

                var subject = await _context.Subjects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == subjectId.Value);
                ViewBag.SubjectName = subject?.Name;
            }
            else
            {
                // Apenas grupos de disciplinas em que o utilizador está inscrito
                var enrolledSubjectIds = await _context.SubjectEnrollments
                    .Where(e => e.UserId == user.Id)
                    .Select(e => e.SubjectId)
                    .ToListAsync();

                query = query.Where(sg => enrolledSubjectIds.Contains(sg.SubjectId));
            }

            var studyGroups = await query
                .OrderByDescending(sg => sg.CreatedAt)
                .ToListAsync();

            // Grupos em que o utilizador já é membro
            var userMemberships = await _context.StudyGroupMembers
                .Where(sgm => sgm.UserId == user.Id && sgm.IsActive)
                .Select(sgm => sgm.GroupId)
                .ToListAsync();

            ViewBag.UserMemberships = userMemberships;

            return View(studyGroups);
        }

        // GET: StudyGroups/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var studyGroup = await _context.StudyGroups
                .Include(sg => sg.Subject)
                .Include(sg => sg.Creator)
                .Include(sg => sg.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(sg => sg.Id == id);

            if (studyGroup == null) return NotFound();

            // Verifica se o utilizador está inscrito na disciplina
            bool isEnrolledInSubject = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user.Id && e.SubjectId == studyGroup.SubjectId);

            if (!isEnrolledInSubject) return Forbid();

            bool isMember = studyGroup.Members.Any(m => m.UserId == user.Id && m.IsActive);
            ViewBag.IsMember = isMember;

            if (isMember)
            {
                var messages = await _context.Messages
                    .Include(m => m.Author)
                    .Where(m => m.GroupId == id && m.IsActive)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(20)
                    .AsNoTracking()
                    .ToListAsync();

                ViewBag.Messages = messages.OrderBy(m => m.CreatedAt).ToList();
            }

            return View(studyGroup);
        }

        // GET: StudyGroups/Create
        public async Task<IActionResult> Create(int? subjectId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            IQueryable<Subject> subjectsQuery = _context.Subjects.Where(s => s.IsActive);

            if (subjectId.HasValue)
            {
                bool isEnrolled = await _context.SubjectEnrollments
                    .AnyAsync(e => e.UserId == user.Id && e.SubjectId == subjectId.Value);

                if (!isEnrolled) return Forbid();

                ViewBag.SubjectId = subjectId.Value;
                var subject = await _context.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == subjectId.Value);
                ViewBag.SubjectName = subject?.Name;
            }
            else
            {
                var enrolledSubjectIds = await _context.SubjectEnrollments
                    .Where(e => e.UserId == user.Id)
                    .Select(e => e.SubjectId)
                    .ToListAsync();

                subjectsQuery = subjectsQuery.Where(s => enrolledSubjectIds.Contains(s.Id));
            }

            ViewBag.Subjects = await subjectsQuery.AsNoTracking().ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudyGroup studyGroup)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            bool isEnrolled = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user.Id && e.SubjectId == studyGroup.SubjectId);

            if (!isEnrolled) return Forbid();

            if (!ModelState.IsValid)
            {
                var enrolledSubjectIds = await _context.SubjectEnrollments
                    .Where(e => e.UserId == user.Id)
                    .Select(e => e.SubjectId)
                    .ToListAsync();

                ViewBag.Subjects = await _context.Subjects
                    .Where(s => s.IsActive && enrolledSubjectIds.Contains(s.Id))
                    .ToListAsync();

                return View(studyGroup);
            }

            studyGroup.CreatorId = user.Id;
            _context.StudyGroups.Add(studyGroup);
            await _context.SaveChangesAsync();

            // Adiciona o criador como moderador
            var membership = new StudyGroupMember
            {
                GroupId = studyGroup.Id,
                UserId = user.Id,
                Role = GroupRole.Moderator,
                IsActive = true
            };

            _context.StudyGroupMembers.Add(membership);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = studyGroup.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var studyGroup = await _context.StudyGroups
                .Include(sg => sg.Members)
                .FirstOrDefaultAsync(sg => sg.Id == id);

            if (studyGroup == null) return NotFound();

            bool isEnrolled = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user.Id && e.SubjectId == studyGroup.SubjectId);

            if (!isEnrolled) return Forbid();

            var existingMembership = await _context.StudyGroupMembers
                .FirstOrDefaultAsync(sgm => sgm.GroupId == id && sgm.UserId == user.Id);

            if (existingMembership == null)
            {
                if (studyGroup.Members.Count(m => m.IsActive) >= studyGroup.MaxMembers)
                {
                    TempData["Error"] = "O grupo de estudo está cheio.";
                    return RedirectToAction("Details", new { id });
                }

                _context.StudyGroupMembers.Add(new StudyGroupMember
                {
                    GroupId = id,
                    UserId = user.Id,
                    Role = GroupRole.Member,
                    IsActive = true
                });

                await _context.SaveChangesAsync();
            }
            else if (!existingMembership.IsActive)
            {
                existingMembership.IsActive = true;
                existingMembership.JoinedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var membership = await _context.StudyGroupMembers
                .FirstOrDefaultAsync(sgm => sgm.GroupId == id && sgm.UserId == user.Id);

            if (membership != null)
            {
                membership.IsActive = false;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int groupId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            bool isMember = await _context.StudyGroupMembers
                .AnyAsync(sgm => sgm.GroupId == groupId && sgm.UserId == user.Id && sgm.IsActive);

            if (!isMember) return Forbid();

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "A mensagem não pode estar vazia.";
                return RedirectToAction("Details", new { id = groupId });
            }

            _context.Messages.Add(new Message
            {
                Content = content.Trim(),
                GroupId = groupId,
                AuthorId = user.Id,
                Type = MessageType.Text
            });

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = groupId });
        }

        public async Task<IActionResult> MyGroups()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var myGroups = await _context.StudyGroupMembers
                .Include(sgm => sgm.Group).ThenInclude(sg => sg.Subject)
                .Include(sgm => sgm.Group).ThenInclude(sg => sg.Members)
                .Where(sgm => sgm.UserId == user.Id && sgm.IsActive)
                .Select(sgm => sgm.Group)
                .AsNoTracking()
                .ToListAsync();

            return View(myGroups);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProfessorToGroup(int groupId, string professorEmail)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var group = await _context.StudyGroups
                .Include(g => g.Subject)
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return NotFound();

            bool isMember = await _context.StudyGroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == user.Id && m.IsActive);
            if (!isMember) return Forbid();

            var professor = await _userManager.FindByEmailAsync(professorEmail);
            if (professor == null || !(await _userManager.IsInRoleAsync(professor, "Professor")))
            {
                TempData["Error"] = "O utilizador não é um professor válido.";
                return RedirectToAction("Details", new { id = groupId });
            }

            bool alreadyMember = await _context.StudyGroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == professor.Id);

            if (alreadyMember)
            {
                TempData["Error"] = "Este professor já pertence ao grupo.";
                return RedirectToAction("Details", new { id = groupId });
            }

            _context.StudyGroupMembers.Add(new StudyGroupMember
            {
                GroupId = groupId,
                UserId = professor.Id,
                Role = GroupRole.Member,
                IsActive = true
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Professor adicionado com sucesso.";

            return RedirectToAction("Details", new { id = groupId });
        }
    }
}
