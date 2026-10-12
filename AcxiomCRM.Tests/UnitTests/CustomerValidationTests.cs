using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Implementations;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Tests.TestHelpers;
using Moq;

namespace AcxiomCRM.Tests.UnitTests;

public class CustomerValidationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly MockCurrentUserService _currentUser;
    private readonly CustomerService _service;
    private readonly Mock<IAuditLogService> _auditMock;

    public CustomerValidationTests()
    {
        (_context, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        _currentUser = MockCurrentUserService.CreateAdmin();
        _auditMock = new Mock<IAuditLogService>();
        _service = new CustomerService(_context, _currentUser, _auditMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_ThrowsDomainException()
    {
        var cust1 = new Customer
        {
            CustomerName = "First Customer",
            Email = "unique@company.com",
            Phone = "+1-555-1111"
        };
        await _service.CreateCustomerAsync(cust1);

        var cust2 = new Customer
        {
            CustomerName = "Second Customer",
            Email = "unique@company.com", // Duplicate Email
            Phone = "+1-555-2222"
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateCustomerAsync(cust2));
        Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCustomer_DuplicatePhone_ThrowsDomainException()
    {
        var cust1 = new Customer
        {
            CustomerName = "First Customer",
            Email = "first@company.com",
            Phone = "+1-555-3333"
        };
        await _service.CreateCustomerAsync(cust1);

        var cust2 = new Customer
        {
            CustomerName = "Second Customer",
            Email = "second@company.com",
            Phone = "+1-555-3333" // Duplicate Phone
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateCustomerAsync(cust2));
        Assert.Contains("phone", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateCustomer_ChangingToExistingEmail_ThrowsDomainException()
    {
        var cust1 = await _service.CreateCustomerAsync(new Customer
        {
            CustomerName = "Client Alpha",
            Email = "alpha@domain.com",
            Phone = "+1-555-4441"
        });

        var cust2 = await _service.CreateCustomerAsync(new Customer
        {
            CustomerName = "Client Beta",
            Email = "beta@domain.com",
            Phone = "+1-555-4442"
        });

        // Try updating Beta to Alpha's email
        cust2.Email = "alpha@domain.com";

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.UpdateCustomerAsync(cust2));
        Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
