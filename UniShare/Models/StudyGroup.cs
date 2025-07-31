using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public class StudyGroup
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        public string CreatorId { get; set; } = string.Empty;
        public ApplicationUser Creator { get; set; } = null!;

        public int MaxMembers { get; set; } = 20;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<StudyGroupMember> Members { get; set; } = new List<StudyGroupMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<VideoCall> VideoCalls { get; set; } = new List<VideoCall>();
    }
}