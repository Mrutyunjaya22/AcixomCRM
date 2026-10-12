using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Implementations;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Tests.TestHelpers;
using Moq;

namespace AcxiomCRM.Tests.UnitTests;

public class FollowUpBusinessRuleTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly MockCurrentUserService _currentUser;
    private readonly FollowUpService _service;
    private readonly Mock<IAuditLogService> _auditMock;

    public FollowUpBusinessRuleTests()
    {
        (_context, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        _currentUser = MockCurrentUserService.CreateAdmin();
        _auditMock = new Mock<IAuditLogService>();
        _service = new FollowUpService(_context, _currentUser, _auditMock.Object);

        // Seed customer
        var customer = new Customer
        {
            CustomerId = 1,
            CustomerCode = "CUST-001",
            CustomerName = "Initech LLC",
            Email = "contact@initech.com",
            Phone = "+1-555-0202",
            CreatedDate = DateTime.UtcNow
        };
        _context.Customers.Add(customer);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateFollowUp_PlannedDateInPast_ThrowsDomainException()
    {
        var followUp = new FollowUp
        {
            CustomerId = 1,
            FollowUpType = FollowUpType.Call,
            Subject = "Catch up call",
            FollowUpDate = DateTime.UtcNow.AddDays(-2),
            Status = FollowUpStatus.Planned
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateFollowUpAsync(followUp));
        Assert.Contains("cannot be earlier than today", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RescheduleFollowUp_NewDateInPast_ThrowsDomainException()
    {
        var followUp = new FollowUp
        {
            CustomerId = 1,
            FollowUpType = FollowUpType.Meeting,
            Subject = "Strategy Review",
            FollowUpDate = DateTime.UtcNow.AddDays(2),
            Status = FollowUpStatus.Planned
        };
        var created = await _service.CreateFollowUpAsync(followUp);

        var pastDate = DateTime.UtcNow.AddDays(-1);
        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.RescheduleFollowUpAsync(created.FollowUpId, pastDate, "Postponed"));
        Assert.Contains("cannot be earlier than today", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteFollowUp_UpdatesStatusToCompleted()
    {
        var followUp = new FollowUp
        {
            CustomerId = 1,
            FollowUpType = FollowUpType.Call,
            Subject = "Product Demo Review",
            FollowUpDate = DateTime.UtcNow.AddDays(1),
            Status = FollowUpStatus.Planned
        };
        var created = await _service.CreateFollowUpAsync(followUp);

        var completed = await _service.CompleteFollowUpAsync(created.FollowUpId, "Demo went great, client requested pricing");

        Assert.Equal(FollowUpStatus.Completed, completed.Status);
        Assert.Contains("Pricing", completed.Remarks, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelFollowUp_UpdatesStatusToCancelled()
    {
        var followUp = new FollowUp
        {
            CustomerId = 1,
            FollowUpType = FollowUpType.Meeting,
            Subject = "Contract Discussion",
            FollowUpDate = DateTime.UtcNow.AddDays(3),
            Status = FollowUpStatus.Planned
        };
        var created = await _service.CreateFollowUpAsync(followUp);

        var cancelled = await _service.CancelFollowUpAsync(created.FollowUpId, "Client cancelled initiative");

        Assert.Equal(FollowUpStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task FollowUp_SalesExecutive_CannotAccessUnassignedFollowUp()
    {
        var followUp = new FollowUp
        {
            CustomerId = 1,
            FollowUpType = FollowUpType.Call,
            Subject = "Confidential Exec Call",
            FollowUpDate = DateTime.UtcNow.AddDays(2),
            Status = FollowUpStatus.Planned,
            AssignedToId = "sales-99"
        };
        var created = await _service.CreateFollowUpAsync(followUp);

        var salesUser = MockCurrentUserService.CreateSalesExecutive("sales-1");
        var restrictedService = new FollowUpService(_context, salesUser, _auditMock.Object);

        var canAccess = await restrictedService.CanAccessFollowUpAsync(created.FollowUpId);
        Assert.False(canAccess);
    }

    [Fact]
    public async Task CreateFollowUp_WithNonExistentCustomerId_ThrowsDomainException()
    {
        var followUp = new FollowUp
        {
            CustomerId = 99999, // Non-existent customer
            FollowUpType = FollowUpType.Call,
            Subject = "Invalid Customer Follow-up",
            FollowUpDate = DateTime.UtcNow.AddDays(2),
            Status = FollowUpStatus.Planned
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateFollowUpAsync(followUp));
        Assert.Contains("Referenced customer does not exist", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
