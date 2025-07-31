namespace UniShare.Models
{
    public enum GroupRole
    {
        Member,
        Moderator
    }

    public class StudyGroupMember
    {
        public int Id { get; set; }

        public int GroupId { get; set; }
        public StudyGroup Group { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public GroupRole Role { get; set; } = GroupRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}