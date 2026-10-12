using AcxiomCRM.Core.Entities;
using AcxiomCRM.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Tests.TestHelpers;

public static class TestDbContextFactory
{
    public static (ApplicationDbContext Context, SqliteConnection Connection) CreateInMemoryDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        // Seed test users to satisfy foreign key relationships in SQLite
        context.Users.AddRange(
            new ApplicationUser { Id = "admin-1", UserName = "admin@acxiomcrm.local", Email = "admin@acxiomcrm.local", FullName = "Admin User" },
            new ApplicationUser { Id = "manager-1", UserName = "manager@acxiomcrm.local", Email = "manager@acxiomcrm.local", FullName = "Manager User" },
            new ApplicationUser { Id = "sales-1", UserName = "sales1@acxiomcrm.local", Email = "sales1@acxiomcrm.local", FullName = "Sales User 1" },
            new ApplicationUser { Id = "sales-2", UserName = "sales2@acxiomcrm.local", Email = "sales2@acxiomcrm.local", FullName = "Sales User 2" },
            new ApplicationUser { Id = "sales-99", UserName = "sales99@acxiomcrm.local", Email = "sales99@acxiomcrm.local", FullName = "Sales User 99" },
            new ApplicationUser { Id = "test-user-id", UserName = "testuser@acxiomcrm.local", Email = "testuser@acxiomcrm.local", FullName = "Test User" }
        );
        context.SaveChanges();

        return (context, connection);
    }
}
