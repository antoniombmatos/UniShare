using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniShare.Models;

namespace UniShare.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<SubjectEnrollment> SubjectEnrollments { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<StudyGroup> StudyGroups { get; set; }
        public DbSet<StudyGroupMember> StudyGroupMembers { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<VideoCall> VideoCalls { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventAttendee> EventAttendees { get; set; }
        public DbSet<CalendarEntry> CalendarEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUser relationships
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Course)
                .WithMany(c => c.Students)
                .HasForeignKey(u => u.CourseId)
                .OnDelete(DeleteBehavior.SetNull);

            // Subject relationships
            builder.Entity<Subject>()
                .HasOne(s => s.Course)
                .WithMany(c => c.Subjects)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Subject>()
                .HasOne(s => s.Professor)
                .WithMany()
                .HasForeignKey(s => s.ProfessorId)
                .OnDelete(DeleteBehavior.SetNull);

            // SubjectEnrollment relationships
            builder.Entity<SubjectEnrollment>()
                .HasOne(se => se.User)
                .WithMany(u => u.SubjectEnrollments)
                .HasForeignKey(se => se.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SubjectEnrollment>()
                .HasOne(se => se.Subject)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(se => se.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Post relationships
            builder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Post>()
                .HasOne(p => p.Subject)
                .WithMany(s => s.Posts)
                .HasForeignKey(p => p.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Comment relationships
            builder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // StudyGroup relationships
            builder.Entity<StudyGroup>()
                .HasOne(sg => sg.Subject)
                .WithMany(s => s.StudyGroups)
                .HasForeignKey(sg => sg.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudyGroup>()
                .HasOne(sg => sg.Creator)
                .WithMany()
                .HasForeignKey(sg => sg.CreatorId)
                .OnDelete(DeleteBehavior.NoAction);

            // StudyGroupMember relationships
            builder.Entity<StudyGroupMember>()
                .HasOne(sgm => sgm.Group)
                .WithMany(sg => sg.Members)
                .HasForeignKey(sgm => sgm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudyGroupMember>()
                .HasOne(sgm => sgm.User)
                .WithMany(u => u.StudyGroupMemberships)
                .HasForeignKey(sgm => sgm.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Message relationships
            builder.Entity<Message>()
                .HasOne(m => m.Group)
                .WithMany(sg => sg.Messages)
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Message>()
                .HasOne(m => m.Author)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.AuthorId)
                .OnDelete(DeleteBehavior.NoAction);

            // VideoCall relationships
            builder.Entity<VideoCall>()
                .HasOne(vc => vc.Group)
                .WithMany(sg => sg.VideoCalls)
                .HasForeignKey(vc => vc.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VideoCall>()
                .HasOne(vc => vc.Initiator)
                .WithMany()
                .HasForeignKey(vc => vc.InitiatorId)
                .OnDelete(DeleteBehavior.NoAction);

            // News relationships
            builder.Entity<News>()
                .HasOne(n => n.Course)
                .WithMany(c => c.News)
                .HasForeignKey(n => n.CourseId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<News>()
                .HasOne(n => n.Author)
                .WithMany()
                .HasForeignKey(n => n.AuthorId)
                .OnDelete(DeleteBehavior.NoAction);

            // Event relationships
            builder.Entity<Event>()
                .HasOne(e => e.Creator)
                .WithMany(u => u.CreatedEvents)
                .HasForeignKey(e => e.CreatorId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Event>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.SetNull);

            // EventAttendee relationships
            builder.Entity<EventAttendee>()
                .HasOne(ea => ea.Event)
                .WithMany(e => e.Attendees)
                .HasForeignKey(ea => ea.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EventAttendee>()
                .HasOne(ea => ea.User)
                .WithMany(u => u.EventAttendances)
                .HasForeignKey(ea => ea.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // CalendarEntry relationships
            builder.Entity<CalendarEntry>()
                .HasOne(ce => ce.User)
                .WithMany(u => u.CalendarEntries)
                .HasForeignKey(ce => ce.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CalendarEntry>()
                .HasOne(ce => ce.Subject)
                .WithMany(s => s.CalendarEntries)
                .HasForeignKey(ce => ce.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique constraints
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.StudentNumber)
                .IsUnique();

            builder.Entity<Course>()
                .HasIndex(c => c.Code)
                .IsUnique();

            builder.Entity<Subject>()
                .HasIndex(s => new { s.Code, s.CourseId })
                .IsUnique();

            builder.Entity<SubjectEnrollment>()
                .HasIndex(se => new { se.UserId, se.SubjectId })
                .IsUnique();

            builder.Entity<StudyGroupMember>()
                .HasIndex(sgm => new { sgm.GroupId, sgm.UserId })
                .IsUnique();

            builder.Entity<EventAttendee>()
                .HasIndex(ea => new { ea.EventId, ea.UserId })
                .IsUnique();
        }
    }
}