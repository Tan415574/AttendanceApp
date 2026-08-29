using AttendanceApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingSession> MeetingSessions => Set<MeetingSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AttendanceRecord>()
            .HasIndex(a => new { a.MeetingSessionId, a.StudentId })
            .IsUnique(); // one attendance record per student per session — enforces no duplicate check-ins at the DB level

        builder.Entity<MeetingSession>()
            .HasOne(s => s.Meeting)
            .WithMany(m => m.Sessions)
            .HasForeignKey(s => s.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AttendanceRecord>()
            .HasOne(a => a.MeetingSession)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(a => a.MeetingSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
