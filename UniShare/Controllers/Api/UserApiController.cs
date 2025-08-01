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
    [Authorize(Roles = "Administrador")] // Usa só esta, para evitar conflito
    public class UsersApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UsersApiController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Obtém todos os utilizadores.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Include(u => u.Course)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.StudentNumber,
                    Course = u.Course != null ? new { u.Course.Id, u.Course.Name } : null
                })
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Elimina um utilizador pelo ID.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Erro ao eliminar o utilizador." });
            }

            return Ok(new { message = "Utilizador eliminado com sucesso." });
        }
    }
}
