using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string StudentNumber { get; set; } = string.Empty;

        public int? CourseId { get; set; }
        public Course? Course { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<SubjectEnrollment> SubjectEnrollments { get; set; } = new List<SubjectEnrollment>();
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<StudyGroupMember> StudyGroupMemberships { get; set; } = new List<StudyGroupMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
        public ICollection<EventAttendee> EventAttendances { get; set; } = new List<EventAttendee>();
        public ICollection<CalendarEntry> CalendarEntries { get; set; } = new List<CalendarEntry>();
    }
}