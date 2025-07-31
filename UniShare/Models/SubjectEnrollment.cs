using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public class SubjectEnrollment
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
        
        [Range(0, 20)]
        public decimal? Grade { get; set; }
        
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletionDate { get; set; }
    }
}