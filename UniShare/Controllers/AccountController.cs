using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;
using UniShare.ViewModels;

using System.Text.RegularExpressions;

namespace UniShare.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        /// <summary>
        /// Exibe o formulário de registo de novo utilizador.
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        public async Task<IActionResult> Register()
        {

            var courses = await _context.Courses
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Courses = new SelectList(courses, "Id", "Name");
            return View();
        }

        /// <summary>
        /// Regista um novo utilizador.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // ✔️ Verifica domínio @ipt.pt
                if (!model.Email.EndsWith("@ipt.pt", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("Email", "Só são permitidos emails do domínio @ipt.pt.");
                    goto ReturnView;
                }

                // ✔️ Verifica duplicação de número de aluno
                var existingUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.StudentNumber == model.StudentNumber);

                if (existingUser != null)
                {
                    ModelState.AddModelError("StudentNumber", "Este número de aluno já está em uso.");
                    goto ReturnView;
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    StudentNumber = model.StudentNumber,
                    CourseId = model.CourseId
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Aluno");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

        ReturnView:
            var courses = await _context.Courses
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.Courses = new SelectList(courses, "Id", "Name");

            return View(model);
        }

        /// <summary>
        /// Exibe o formulário de login de utilizador.
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Realiza o login de um utilizador.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                    return RedirectToLocal(returnUrl);

                ModelState.AddModelError(string.Empty, "Tentativa de login inválida.");
            }

            return View(model);
        }

        /// <summary>
        /// Realiza o logout do utilizador autenticado.
        /// </summary>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
    }
}