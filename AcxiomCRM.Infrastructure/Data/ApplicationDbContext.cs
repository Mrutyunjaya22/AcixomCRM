using AcxiomCRM.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables for clean schema
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.IsActive).HasDefaultValue(true);
            entity.HasIndex(u => u.Email).IsUnique();
        });

        builder.Entity<IdentityRole>(entity => entity.ToTable("Roles"));
        builder.Entity<IdentityUserRole<string>>(entity => entity.ToTable("UserRoles"));
        builder.Entity<IdentityUserClaim<string>>(entity => entity.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<string>>(entity => entity.ToTable("UserLogins"));
        builder.Entity<IdentityRoleClaim<string>>(entity => entity.ToTable("RoleClaims"));
        builder.Entity<IdentityUserToken<string>>(entity => entity.ToTable("UserTokens"));

        // Customer Configuration
        builder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(c => c.CustomerId);

            entity.Property(c => c.CustomerCode).HasMaxLength(20).IsRequired();
            entity.HasIndex(c => c.CustomerCode).IsUnique();

            entity.Property(c => c.CustomerName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Email).HasMaxLength(100).IsRequired();
            entity.HasIndex(c => c.Email).IsUnique();

            entity.Property(c => c.Phone).HasMaxLength(20).IsRequired();
            entity.HasIndex(c => c.Phone).IsUnique();

            entity.Property(c => c.CompanyName).HasMaxLength(100);
            entity.Property(c => c.Address).HasMaxLength(200);
            entity.Property(c => c.City).HasMaxLength(50);
            entity.Property(c => c.State).HasMaxLength(50);
            entity.Property(c => c.Notes).HasMaxLength(1000);

            entity.HasOne(c => c.CreatedBy)
                  .WithMany(u => u.CreatedCustomers)
                  .HasForeignKey(c => c.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.AssignedTo)
                  .WithMany(u => u.AssignedCustomers)
                  .HasForeignKey(c => c.AssignedToId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Lead Configuration
        builder.Entity<Lead>(entity =>
        {
            entity.ToTable("Leads");
            entity.HasKey(l => l.LeadId);

            entity.Property(l => l.LeadCode).HasMaxLength(20).IsRequired();
            entity.HasIndex(l => l.LeadCode).IsUnique();

            entity.Property(l => l.LeadName).HasMaxLength(100).IsRequired();
            entity.Property(l => l.Email).HasMaxLength(100).IsRequired();
            entity.Property(l => l.Phone).HasMaxLength(20).IsRequired();
            entity.Property(l => l.CompanyName).HasMaxLength(100);
            entity.Property(l => l.ExpectedValue).HasPrecision(18, 2);
            entity.Property(l => l.Notes).HasMaxLength(1000);

            entity.HasIndex(l => l.Status);
            entity.HasIndex(l => l.Source);

            entity.HasOne(l => l.AssignedTo)
                  .WithMany(u => u.AssignedLeads)
                  .HasForeignKey(l => l.AssignedToId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(l => l.ConvertedCustomer)
                  .WithMany()
                  .HasForeignKey(l => l.ConvertedCustomerId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(l => l.ConvertedOpportunity)
                  .WithMany()
                  .HasForeignKey(l => l.ConvertedOpportunityId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Opportunity Configuration
        builder.Entity<Opportunity>(entity =>
        {
            entity.ToTable("Opportunities");
            entity.HasKey(o => o.OpportunityId);

            entity.Property(o => o.OpportunityCode).HasMaxLength(20).IsRequired();
            entity.HasIndex(o => o.OpportunityCode).IsUnique();

            entity.Property(o => o.OpportunityName).HasMaxLength(150).IsRequired();
            entity.Property(o => o.Amount).HasPrecision(18, 2);
            entity.Property(o => o.Notes).HasMaxLength(1000);

            entity.HasIndex(o => o.Stage);
            entity.HasIndex(o => o.Status);

            entity.HasOne(o => o.Customer)
                  .WithMany(c => c.Opportunities)
                  .HasForeignKey(o => o.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Lead)
                  .WithMany()
                  .HasForeignKey(o => o.LeadId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(o => o.AssignedTo)
                  .WithMany(u => u.AssignedOpportunities)
                  .HasForeignKey(o => o.AssignedToId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // FollowUp Configuration
        builder.Entity<FollowUp>(entity =>
        {
            entity.ToTable("FollowUps");
            entity.HasKey(f => f.FollowUpId);

            entity.Property(f => f.Subject).HasMaxLength(150).IsRequired();
            entity.Property(f => f.Remarks).HasMaxLength(1000);

            entity.HasIndex(f => f.FollowUpDate);
            entity.HasIndex(f => f.Status);

            entity.HasOne(f => f.Customer)
                  .WithMany(c => c.FollowUps)
                  .HasForeignKey(f => f.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Lead)
                  .WithMany(l => l.FollowUps)
                  .HasForeignKey(f => f.LeadId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Opportunity)
                  .WithMany(o => o.FollowUps)
                  .HasForeignKey(f => f.OpportunityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.AssignedTo)
                  .WithMany(u => u.AssignedFollowUps)
                  .HasForeignKey(f => f.AssignedToId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Activity Configuration
        builder.Entity<Activity>(entity =>
        {
            entity.ToTable("Activities");
            entity.HasKey(a => a.ActivityId);

            entity.Property(a => a.Subject).HasMaxLength(150).IsRequired();
            entity.Property(a => a.Description).HasMaxLength(1000);

            entity.HasIndex(a => a.ActivityDate);

            entity.HasOne(a => a.Customer)
                  .WithMany(c => c.Activities)
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Lead)
                  .WithMany(l => l.Activities)
                  .HasForeignKey(a => a.LeadId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.AssignedTo)
                  .WithMany(u => u.AssignedActivities)
                  .HasForeignKey(a => a.AssignedToId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // AuditLog Configuration
        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(a => a.AuditLogId);

            entity.Property(a => a.UserId).HasMaxLength(450);
            entity.Property(a => a.UserName).HasMaxLength(100);
            entity.Property(a => a.Module).HasMaxLength(50).IsRequired();
            entity.Property(a => a.Action).HasMaxLength(50).IsRequired();
            entity.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
            entity.Property(a => a.RecordId).HasMaxLength(100);
            entity.Property(a => a.Result).HasMaxLength(50).IsRequired();
            entity.Property(a => a.IpAddress).HasMaxLength(50);
            entity.Property(a => a.Details).HasMaxLength(1000);

            entity.HasIndex(a => a.CreatedDate);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.Module);
            entity.HasIndex(a => a.Action);
        });
    }
}
