using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Implementations;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Tests.TestHelpers;
using Moq;

namespace AcxiomCRM.Tests.UnitTests;

public class LeadConversionBusinessRuleTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly MockCurrentUserService _currentUser;
    private readonly LeadService _service;
    private readonly CustomerService _customerService;
    private readonly Mock<IAuditLogService> _auditMock;

    public LeadConversionBusinessRuleTests()
    {
        (_context, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        _currentUser = MockCurrentUserService.CreateAdmin();
        _auditMock = new Mock<IAuditLogService>();
        _customerService = new CustomerService(_context, _currentUser, _auditMock.Object);
        _service = new LeadService(_context, _currentUser, _auditMock.Object, _customerService);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ConvertLead_ValidLead_CreatesCustomerAndOpportunityAndMarksLeadConverted()
    {
        var lead = new Lead
        {
            LeadName = "Global Logistics Ltd",
            Email = "procurement@globallogistics.com",
            Phone = "+1-555-0303",
            CompanyName = "Global Logistics",
            Source = LeadSource.Referral,
            Status = LeadStatus.Qualified,
            ExpectedValue = 75000m
        };
        var createdLead = await _service.CreateLeadAsync(lead);

        var (customer, opportunity) = await _service.ConvertLeadAsync(createdLead.LeadId, "Strategic deal conversion");

        Assert.NotNull(customer);
        Assert.Equal("Global Logistics Ltd", customer.CustomerName);
        Assert.Equal("procurement@globallogistics.com", customer.Email);

        Assert.NotNull(opportunity);
        Assert.Equal(75000m, opportunity.Amount);
        Assert.Equal(customer.CustomerId, opportunity.CustomerId);

        // Verify lead is marked converted
        var refreshedLead = await _service.GetLeadByIdAsync(createdLead.LeadId);
        Assert.NotNull(refreshedLead);
        Assert.Equal(LeadStatus.Converted, refreshedLead.Status);
        Assert.Equal(customer.CustomerId, refreshedLead.ConvertedCustomerId);
        Assert.Equal(opportunity.OpportunityId, refreshedLead.ConvertedOpportunityId);
    }

    [Fact]
    public async Task ConvertLead_AlreadyConvertedLead_ThrowsDomainException()
    {
        var lead = new Lead
        {
            LeadName = "Dupe Convert Corp",
            Email = "contact@dupeconvert.com",
            Phone = "+1-555-0404",
            Status = LeadStatus.Qualified,
            ExpectedValue = 50000m
        };
        var createdLead = await _service.CreateLeadAsync(lead);

        // First conversion succeeds
        await _service.ConvertLeadAsync(createdLead.LeadId);

        // Second conversion attempt must fail
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.ConvertLeadAsync(createdLead.LeadId));

        Assert.Contains("already converted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConvertLead_LostOrUnqualifiedLead_ThrowsDomainException()
    {
        var lead = new Lead
        {
            LeadName = "Lost Prospect",
            Email = "lost@prospect.com",
            Phone = "+1-555-0505",
            Status = LeadStatus.Lost,
            ExpectedValue = 10000m
        };
        var createdLead = await _service.CreateLeadAsync(lead);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.ConvertLeadAsync(createdLead.LeadId));

        Assert.Contains("cannot be converted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
