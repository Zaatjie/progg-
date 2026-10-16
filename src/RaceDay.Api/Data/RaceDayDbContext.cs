using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Phone).HasMaxLength(20);
        });

        b.Entity<Event>(e =>
        {
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventName).HasMaxLength(150).IsRequired();
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.EventType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Distance).HasPrecision(5, 2);
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
             .HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Category>(e =>
        {
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.CategoryName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Distance).HasPrecision(5, 2);
            e.Property(x => x.EntryFee).HasPrecision(8, 2);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
             .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Enrolment>(e =>
        {
            e.HasKey(x => x.EnrolmentId);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.ParticipantId, x.CategoryId }).IsUnique();
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
             .HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
             .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Result>(e =>
        {
            e.HasKey(x => x.ResultId);
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.EnrolmentId).IsUnique();
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
             .HasForeignKey<Result>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Payment>(e =>
        {
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.Amount).HasPrecision(8, 2);
            e.Property(x => x.PaymentMethod).HasMaxLength(30).IsRequired();
            e.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.EnrolmentId).IsUnique();
            e.HasOne(x => x.Enrolment).WithOne(en => en.Payment)
             .HasForeignKey<Payment>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
