using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        public int TotalECTS { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<ApplicationUser> Students { get; set; } = new List<ApplicationUser>();
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public ICollection<News> News { get; set; } = new List<News>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}