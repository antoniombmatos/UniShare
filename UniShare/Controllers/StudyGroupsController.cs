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
            
            IQueryable<StudyGroup> query = _context.StudyGroups
                .Include(sg => sg.Subject)
                .Include(sg => sg.Creator)
                .Include(sg => sg.Members)
                .Where(sg => sg.IsActive);

            if (subjectId.HasValue)
            {
                query = query.Where(sg => sg.SubjectId == subjectId.Value);
                ViewBag.SubjectId = subjectId.Value;
                
                var subject = await _context.Subjects.FindAsync(subjectId.Value);
                ViewBag.SubjectName = subject?.Name;
            }
            else
            {
                // Show only groups from subjects the user is enrolled in
                var enrolledSubjectIds = await _context.SubjectEnrollments
                    .Where(e => e.UserId == user!.Id)
                    .Select(e => e.SubjectId)
                    .ToListAsync();

                query = query.Where(sg => enrolledSubjectIds.Contains(sg.SubjectId));
            }

            var studyGroups = await query
                .OrderByDescending(sg => sg.CreatedAt)
                .ToListAsync();

            // Get user's memberships
            var userMemberships = await _context.StudyGroupMembers
                .Where(sgm => sgm.UserId == user!.Id && sgm.IsActive)
                .Select(sgm => sgm.GroupId)
                .ToListAsync();

            ViewBag.UserMemberships = userMemberships;

            return View(studyGroups);
        }

        // GET: StudyGroups/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var studyGroup = await _context.StudyGroups
                .Include(sg => sg.Subject)
                .Include(sg => sg.Creator)
                .Include(sg => sg.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(sg => sg.Id == id);

            if (studyGroup == null) return NotFound();

            // Check if user is enrolled in the subject
            var isEnrolledInSubject = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user!.Id && e.SubjectId == studyGroup.SubjectId);

            if (!isEnrolledInSubject)
            {
                return Forbid();
            }

            var isMember = studyGroup.Members.Any(m => m.UserId == user!.Id && m.IsActive);
            ViewBag.IsMember = isMember;

            if (isMember)
            {
                // Get recent messages
                var messages = await _context.Messages
                    .Include(m => m.Author)
                    .Where(m => m.GroupId == id && m.IsActive)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                ViewBag.Messages = messages.OrderBy(m => m.CreatedAt).ToList();
            }

            return View(studyGroup);
        }

        // GET: StudyGroups/Create
        public async Task<IActionResult> Create(int? subjectId)
        {
            var user = await _userManager.GetUserAsync(User);
            
            IQueryable<Subject> subjectsQuery = _context.Subjects
                .Where(s => s.IsActive);

            if (subjectId.HasValue)
            {
                // Verify user is enrolled in this subject
                var isEnrolled = await _context.SubjectEnrollments
                    .AnyAsync(e => e.UserId == user!.Id && e.SubjectId == subjectId.Value);

                if (!isEnrolled)
                {
                    return Forbid();
                }

                ViewBag.SubjectId = subjectId.Value;
                var subject = await _context.Subjects.FindAsync(subjectId.Value);
                ViewBag.SubjectName = subject?.Name;
            }
            else
            {
                // Show only subjects the user is enrolled in
                var enrolledSubjectIds = await _context.SubjectEnrollments
                    .Where(e => e.UserId == user!.Id)
                    .Select(e => e.SubjectId)
                    .ToListAsync();

                subjectsQuery = subjectsQuery.Where(s => enrolledSubjectIds.Contains(s.Id));
            }

            ViewBag.Subjects = await subjectsQuery.ToListAsync();
            return View();
        }

        /// <summary>
        /// Creates a new study group with the provided details and adds the creator as a moderator.
        /// </summary>
        /// <param name="studyGroup"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudyGroup studyGroup)
        {
            var user = await _userManager.GetUserAsync(User);
            
            // Verify user is enrolled in the subject
            var isEnrolled = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user!.Id && e.SubjectId == studyGroup.SubjectId);

            if (!isEnrolled)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                studyGroup.CreatorId = user!.Id;
                _context.StudyGroups.Add(studyGroup);
                await _context.SaveChangesAsync();

                // Add creator as moderator
                var membership = new StudyGroupMember
                {
                    GroupId = studyGroup.Id,
                    UserId = user.Id,
                    Role = GroupRole.Moderator
                };

                _context.StudyGroupMembers.Add(membership);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", new { id = studyGroup.Id });
            }

            // Reload subjects if model is invalid
            var enrolledSubjectIds = await _context.SubjectEnrollments
                .Where(e => e.UserId == user!.Id)
                .Select(e => e.SubjectId)
                .ToListAsync();

            ViewBag.Subjects = await _context.Subjects
                .Where(s => s.IsActive && enrolledSubjectIds.Contains(s.Id))
                .ToListAsync();

            return View(studyGroup);
        }

        /// <summary>
        /// Allows a user to join an existing study group if they are enrolled in the subject and not already a member.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var studyGroup = await _context.StudyGroups
                .Include(sg => sg.Members)
                .FirstOrDefaultAsync(sg => sg.Id == id);

            if (studyGroup == null) return NotFound();

            // Check if user is enrolled in the subject
            var isEnrolled = await _context.SubjectEnrollments
                .AnyAsync(e => e.UserId == user!.Id && e.SubjectId == studyGroup.SubjectId);

            if (!isEnrolled)
            {
                return Forbid();
            }

            // Check if already a member
            var existingMembership = await _context.StudyGroupMembers
                .FirstOrDefaultAsync(sgm => sgm.GroupId == id && sgm.UserId == user.Id);

            if (existingMembership == null)
            {
                // Check if group is full
                if (studyGroup.Members.Count(m => m.IsActive) >= studyGroup.MaxMembers)
                {
                    TempData["Error"] = "O grupo de estudo está cheio.";
                    return RedirectToAction("Details", new { id });
                }

                var membership = new StudyGroupMember
                {
                    GroupId = id,
                    UserId = user.Id,
                    Role = GroupRole.Member
                };

                _context.StudyGroupMembers.Add(membership);
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

        /// <summary>
        /// Allows a user to leave a study group they are a member of.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var membership = await _context.StudyGroupMembers
                .FirstOrDefaultAsync(sgm => sgm.GroupId == id && sgm.UserId == user!.Id);

            if (membership != null)
            {
                membership.IsActive = false;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Sends a message in the specified study group if the user is a member.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="content"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int groupId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            
            // Verify user is a member of the group
            var isMember = await _context.StudyGroupMembers
                .AnyAsync(sgm => sgm.GroupId == groupId && sgm.UserId == user!.Id && sgm.IsActive);

            if (!isMember)
            {
                return Forbid();
            }

            var message = new Message
            {
                Content = content,
                GroupId = groupId,
                AuthorId = user!.Id,
                Type = MessageType.Text
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = groupId });
        }

        // GET: StudyGroups/MyGroups
        public async Task<IActionResult> MyGroups()
        {
            var user = await _userManager.GetUserAsync(User);
            
            var myGroups = await _context.StudyGroupMembers
                .Include(sgm => sgm.Group)
                    .ThenInclude(sg => sg.Subject)
                .Include(sgm => sgm.Group)
                    .ThenInclude(sg => sg.Members)
                .Where(sgm => sgm.UserId == user!.Id && sgm.IsActive)
                .Select(sgm => sgm.Group)
                .ToListAsync();

            return View(myGroups);
        }
    }
}