using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public class VideoCall
    {
        public int Id { get; set; }

        public int GroupId { get; set; }
        public StudyGroup Group { get; set; } = null!;

        [StringLength(200)]
        public string? SessionLink { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string InitiatorId { get; set; } = string.Empty;
        public ApplicationUser Initiator { get; set; } = null!;

        public bool IsActive { get; set; } = true;
    }
}