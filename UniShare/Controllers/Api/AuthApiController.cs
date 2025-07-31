using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;
using System.Security.Claims;

namespace UniShare.Controllers.Api
{
    /// <summary>
    /// Controlador de API para autenticação e registo de utilizadores.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Construtor do controlador de autenticação.
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="signInManager"></param>
        /// <param name="context"></param>
        public AuthApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        /// <summary>
        /// Regista um novo utilizador.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var course = await _context.Courses.FindAsync(request.CourseId);
            if (course == null)
                return BadRequest("Curso inválido.");

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return BadRequest("Email já está em uso.");

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                StudentNumber = request.StudentNumber,
                CourseId = request.CourseId,
                EmailConfirmed = true // podes alterar conforme a tua política
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Podes atribuir um role se estiveres a usar RoleManager
            // await _userManager.AddToRoleAsync(user, "Aluno");

            return Ok(new { message = "Registo efetuado com sucesso." });
        }

        /// <summary>
        /// Autentica um utilizador existente.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _signInManager.PasswordSignInAsync(request.Email, request.Password, false, false);

            if (!result.Succeeded)
                return Unauthorized("Credenciais inválidas.");

            var user = await _userManager.FindByEmailAsync(request.Email);

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.StudentNumber,
                Course = await _context.Courses
                    .Where(c => c.Id == user.CourseId)
                    .Select(c => new { c.Id, c.Name })
                    .FirstOrDefaultAsync()
            });
        }

        /// <summary>
        /// Obtém os detalhes do utilizador autenticado.
        /// </summary>
        /// <returns></returns>

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.Users
                .Include(u => u.Course)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.StudentNumber,
                Course = user.Course != null ? new { user.Course.Id, user.Course.Name } : null
            });
        }

        /// <summary>
        /// Termina a sessão do utilizador autenticado.
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logout efetuado com sucesso." });
        }
    }

    /// <summary>
    /// Modelo de solicitação para registo de utilizador.
    /// </summary>
    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int CourseId { get; set; }
    }

    /// <summary>
    /// Modelo de solicitação para autenticação de utilizador.
    /// </summary>
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }


}
