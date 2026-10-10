using LinchpinBackend.Models;
using LinchpinBackend.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace LinchpinBackend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Simulator> Simulators => Set<Simulator>();
    public DbSet<SimulatorCode> SimulatorCodes => Set<SimulatorCode>();
    public DbSet<SimulatorClass> SimulatorClasses => Set<SimulatorClass>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<LessonGroup> LessonGroups => Set<LessonGroup>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonReport> LessonReports => Set<LessonReport>();
    public DbSet<ScenarioReport> ScenarioReports => Set<ScenarioReport>();
    public DbSet<CleaningReport> CleaningReports => Set<CleaningReport>();
    public DbSet<MaintenanceReport> MaintenanceReports => Set<MaintenanceReport>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(e =>
        {
            e.ToTable("users");
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Login).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Login).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(160).IsRequired();
            e.Property(x => x.Image).HasMaxLength(255);
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Role);
            e.Property(x => x.Task).HasMaxLength(60).IsRequired();
        });

        model.Entity<SimulatorClass>(e =>
        {
            e.ToTable("simulator_classes");
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.ClassImage).HasMaxLength(255);
            e.HasMany(x => x.Simulators)
                .WithOne(x => x.SimulatorClass)
                .HasForeignKey(x => x.SimulatorClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Simulator>(e =>
        {
            e.ToTable("simulators");
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Image).HasMaxLength(255);
            e.Property(x => x.Description).HasColumnType("text");
            e.Property(x => x.TechnicalDescription).HasColumnType("text");
            e.Property(x => x.Category).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.Category);
            e.HasMany(x => x.Codes)
                .WithOne(x => x.Simulator)
                .HasForeignKey(x => x.SimulatorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<SimulatorCode>(e =>
        {
            e.ToTable("simulator_codes");
            e.Property(x => x.Code).HasMaxLength(40).IsRequired();
            e.HasIndex(x => x.Code);
        });

        model.Entity<Course>(e =>
        {
            e.ToTable("courses");
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Label).HasMaxLength(40).IsRequired();
            e.Property(x => x.LessonType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Description).HasColumnType("text");
            e.Property(x => x.UpdateDate).HasMaxLength(20);
        });

        model.Entity<LessonGroup>(e =>
        {
            e.ToTable("lesson_groups");
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Type);
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Value).HasMaxLength(40).IsRequired();
            e.HasMany(x => x.Lessons)
                .WithOne(x => x.LessonGroup)
                .HasForeignKey(x => x.LessonGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Lesson>(e =>
        {
            e.ToTable("lessons");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnType("text");
            e.Property(x => x.UpdateDate).HasMaxLength(20);
        });

        ConfigureReport<LessonReport>(model, "lesson_reports");
        ConfigureReport<ScenarioReport>(model, "scenario_reports");
        ConfigureReport<CleaningReport>(model, "cleaning_reports");
        ConfigureReport<MaintenanceReport>(model, "maintenance_reports");
    }

    private static void ConfigureReport<T>(ModelBuilder model, string table) where T : Models.Reports.Report
    {
        model.Entity<T>(e =>
        {
            e.ToTable(table);
            e.Property(x => x.Data).HasColumnType("json").IsRequired();
            e.Property(x => x.UserName).HasMaxLength(120);
            e.Property(x => x.UserRole).HasMaxLength(20);
            e.HasIndex(x => x.ReportDate);
            e.HasIndex(x => x.UserName);
            e.HasIndex(x => x.CreatedByUserId);
        });
    }
}
