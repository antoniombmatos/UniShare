using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace UniShare.ViewModels
{
    public class RegisterProfessorViewModel
    {
        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        [StringLength(100)]
        [Display(Name = "Nome Completo")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A password é obrigatória.")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Password")]
        [Compare("Password", ErrorMessage = "As passwords não coincidem.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "O código de acesso é obrigatório.")]
        [Display(Name = "Código de Acesso de Professor")]
        public string AccessCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "O curso é obrigatório.")]
        [Display(Name = "Curso")] 
        public int CourseId { get; set; }
        public IEnumerable<SelectListItem>? AvailableCourses { get; set; }
    }
}
