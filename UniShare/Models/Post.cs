using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public enum PostType
    {
        Text,
        Document,
        Link
    }

    public class Post
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public PostType Type { get; set; } = PostType.Text;

        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public string? FileSize { get; set; }
        public string? LinkUrl { get; set; }

        public string AuthorId { get; set; } = string.Empty;
        public ApplicationUser Author { get; set; } = null!;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}