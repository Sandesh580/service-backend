using LoginCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoginCheck.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ServiceNotification> ServiceNotifications => Set<ServiceNotification>();
    public DbSet<InventoryPart> InventoryParts => Set<InventoryPart>();
    public DbSet<ServiceRequestPart> ServiceRequestParts => Set<ServiceRequestPart>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Specialties).IsRequired().HasMaxLength(300);
            entity.Property(e => e.EducationQualification).IsRequired().HasMaxLength(200);
            entity.Property(e => e.VocationalTraining).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PermanentAddress).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.CurrentAddress).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.BloodGroup).IsRequired().HasMaxLength(10);
            entity.Property(e => e.PastExperience).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.AvatarKey).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(30);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<ServiceNotification>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.RequestNumber).HasMaxLength(20);
            entity.Property(item => item.Title).IsRequired().HasMaxLength(160);
            entity.Property(item => item.Message).IsRequired().HasMaxLength(1000);
            entity.HasIndex(item => new { item.RecipientUserId, item.CreatedAt });
        });
        modelBuilder.Entity<InventoryPart>(entity =>
        {
            entity.HasKey(part => part.Id);
            entity.Property(part => part.Sku).IsRequired().HasMaxLength(40);
            entity.Property(part => part.Name).IsRequired().HasMaxLength(150);
            entity.Property(part => part.ApplianceType).IsRequired().HasMaxLength(80);
            entity.Property(part => part.Description).IsRequired().HasMaxLength(500);
            entity.Property(part => part.QuantityInStock).IsRequired();
            entity.Property(part => part.UnitPrice).HasPrecision(10, 2);
            entity.HasIndex(part => part.Sku).IsUnique();
        });
        modelBuilder.Entity<ServiceRequestPart>(entity =>
        {
            entity.HasKey(link => link.Id);
            entity.Property(link => link.AssignedAt).IsRequired();
            entity.HasIndex(link => new { link.ServiceRequestId, link.InventoryPartId }).IsUnique();
            entity.HasOne(link => link.ServiceRequest).WithMany(request => request.Parts).HasForeignKey(link => link.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.InventoryPart).WithMany(part => part.RequestParts).HasForeignKey(link => link.InventoryPartId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Appliance).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Issue).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ProblemDescription).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AmountPaid).HasPrecision(10, 2);
            entity.Property(e => e.ManagerNote).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.AssignedTechnician).HasMaxLength(100);
            entity.Property(e => e.ApplianceBrand).HasMaxLength(100);
            entity.Property(e => e.ApplianceModel).HasMaxLength(100);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.CustomerPhone).HasMaxLength(40);
            entity.Property(e => e.ServiceAddress).HasMaxLength(500);
            entity.Property(e => e.Latitude);
            entity.Property(e => e.Longitude);
            entity.Property(e => e.NextVisitDate);
            entity.Property(e => e.IssueCategory).HasMaxLength(100);
            entity.Property(e => e.TechnicianNote).HasMaxLength(2000);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.Property(e => e.TechnicianActionAt);
            entity.HasIndex(e => e.CustomerUserId);
            entity.HasIndex(e => e.AssignedTechnicianId);
        });
    }
}
