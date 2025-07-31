using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public enum MessageType
    {
        Text,
        File,
        System
    }

    public class Message
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public MessageType Type { get; set; } = MessageType.Text;

        public string? FilePath { get; set; }
        public string? FileName { get; set; }

        public int GroupId { get; set; }
        public StudyGroup Group { get; set; } = null!;

        public string AuthorId { get; set; } = string.Empty;
        public ApplicationUser Author { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}