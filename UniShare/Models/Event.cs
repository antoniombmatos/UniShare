using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public enum EventStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class Event
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        public string CreatorId { get; set; } = string.Empty;
        public ApplicationUser Creator { get; set; } = null!;

        public int? CourseId { get; set; }
        public Course? Course { get; set; }

        public EventStatus Status { get; set; } = EventStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<EventAttendee> Attendees { get; set; } = new List<EventAttendee>();
    }
}