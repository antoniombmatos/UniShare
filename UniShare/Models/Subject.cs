using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public class Subject
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

        public int ECTS { get; set; }
        public int Semester { get; set; }
        public int Year { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public string? ProfessorId { get; set; }
        public ApplicationUser? Professor { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<SubjectEnrollment> Enrollments { get; set; } = new List<SubjectEnrollment>();
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<StudyGroup> StudyGroups { get; set; } = new List<StudyGroup>();
        public ICollection<CalendarEntry> CalendarEntries { get; set; } = new List<CalendarEntry>();
    }
}