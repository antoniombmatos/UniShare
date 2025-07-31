using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;

namespace UniShare.Controllers.Api
{
    /// <summary>
    /// Controlador de API para grupos de estudo.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrador")]
    public class StudyGroupsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudyGroupsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Cria um novo grupo de estudo.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            var user = await _userManager.GetUserAsync(User);

            var subject = await _context.Subjects.FindAsync(request.SubjectId);
            if (subject == null)
                return BadRequest("Disciplina inválida.");

            var group = new StudyGroup
            {
                Name = request.Name,
                Description = request.Description,
                SubjectId = request.SubjectId,
                CreatorId = user!.Id,
                MaxMembers = request.MaxMembers
            };

            _context.StudyGroups.Add(group);
            await _context.SaveChangesAsync();

            // Adiciona o criador como moderador
            var member = new StudyGroupMember
            {
                GroupId = group.Id,
                UserId = user.Id,
                Role = GroupRole.Moderator
            };
            _context.StudyGroupMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok(new { group.Id, group.Name, group.SubjectId });
        }

        /// <summary>
        /// Obtém grupos de estudo por disciplina.
        /// </summary>
        /// <param name="subjectId"></param>
        /// <returns></returns>

        [HttpGet("/api/subjects/{subjectId}/study-groups")]
        public async Task<IActionResult> GetGroupsBySubject(int subjectId)
        {
            var groups = await _context.StudyGroups
                .Where(g => g.SubjectId == subjectId && g.IsActive)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.Description,
                    g.MaxMembers,
                    MemberCount = g.Members.Count,
                    Creator = new { g.Creator.FullName, g.Creator.Id }
                })
                .ToListAsync();

            return Ok(groups);
        }

        /// <summary>
        /// Envia uma mensagem para um grupo de estudo.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="request"></param>
        /// <returns></returns>

        [HttpPost("{groupId}/messages")]
        public async Task<IActionResult> SendMessage(int groupId, [FromBody] SendMessageRequest request)
        {
            var user = await _userManager.GetUserAsync(User);

            var isMember = await _context.StudyGroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == user!.Id);

            if (!isMember) return Forbid();

            var message = new Message
            {
                GroupId = groupId,
                AuthorId = user.Id,
                Content = request.Content,
                Type = request.Type
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message.Id,
                message.Content,
                message.Type,
                message.CreatedAt,
                Author = new { user.FullName, user.Id }
            });
        }

        /// <summary>
        /// Obtém mensagens de um grupo de estudo.
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>

        [HttpGet("{groupId}/messages")]
        public async Task<IActionResult> GetMessages(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);

            var isMember = await _context.StudyGroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == user!.Id);

            if (!isMember) return Forbid();

            var messages = await _context.Messages
                .Where(m => m.GroupId == groupId && m.IsActive)
                .Include(m => m.Author)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.Content,
                    m.Type,
                    m.CreatedAt,
                    Author = new { m.Author.FullName, m.Author.Id }
                })
                .ToListAsync();

            return Ok(messages);
        }

        /// <summary>
        /// Inicia uma chamada de vídeo para um grupo de estudo.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="request"></param>
        /// <returns></returns>

        [HttpPost("{groupId}/video-call")]
        public async Task<IActionResult> StartVideoCall(int groupId, [FromBody] StartCallRequest request)
        {
            var user = await _userManager.GetUserAsync(User);

            var isMember = await _context.StudyGroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == user!.Id);

            if (!isMember) return Forbid();

            var call = new VideoCall
            {
                GroupId = groupId,
                InitiatorId = user.Id,
                SessionLink = request.SessionLink,
                StartTime = DateTime.UtcNow
            };

            _context.VideoCalls.Add(call);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                call.Id,
                call.SessionLink,
                call.StartTime,
                call.GroupId
            });
        }
    }

    /// <summary>
    /// Modelo de solicitação para criar um grupo de estudo.
    /// </summary>
    public class CreateGroupRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public int MaxMembers { get; set; } = 20;
    }

    /// <summary>
    /// Modelo de solicitação para enviar uma mensagem a um grupo de estudo.
    /// </summary>
    public class SendMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.Text;
    }

    /// <summary>
    /// Modelo de solicitação para iniciar uma chamada de vídeo em um grupo de estudo.
    /// </summary>
    public class StartCallRequest
    {
        public string SessionLink { get; set; } = string.Empty; // ex: link Jitsi/Zoom
    }
}
