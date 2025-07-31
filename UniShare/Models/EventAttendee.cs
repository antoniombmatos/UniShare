namespace UniShare.Models
{
    public enum AttendanceStatus
    {
        Interested,
        Confirmed,
        Declined
    }

    public class EventAttendee
    {
        public int Id { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public AttendanceStatus Status { get; set; } = AttendanceStatus.Interested;
        public DateTime ResponseDate { get; set; } = DateTime.UtcNow;
    }
}