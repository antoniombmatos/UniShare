using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public enum CalendarEntryType
    {
        Exam,
        Assignment,
        Event,
        Other
    }

    public class CalendarEntry
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int? SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public CalendarEntryType Type { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime DateTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}