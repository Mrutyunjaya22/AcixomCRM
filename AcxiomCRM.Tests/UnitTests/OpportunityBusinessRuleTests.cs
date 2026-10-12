using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Implementations;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Tests.TestHelpers;
using Moq;

namespace AcxiomCRM.Tests.UnitTests;

public class OpportunityBusinessRuleTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly MockCurrentUserService _currentUser;
    private readonly OpportunityService _service;
    private readonly Mock<IAuditLogService> _auditMock;
    private readonly CustomerService _customerService;

    public OpportunityBusinessRuleTests()
    {
        (_context, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        _currentUser = MockCurrentUserService.CreateAdmin();
        _auditMock = new Mock<IAuditLogService>();
        _customerService = new CustomerService(_context, _currentUser, _auditMock.Object);
        _service = new OpportunityService(_context, _currentUser, _auditMock.Object, _customerService);

        // Seed a customer for foreign key
        var customer = new Customer
        {
            CustomerId = 1,
            CustomerCode = "CUST-001",
            CustomerName = "Acme Corp",
            Email = "contact@acme.com",
            Phone = "+1-555-0101",
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
    public async Task CreateOpportunity_ZeroOrNegativeAmount_ThrowsDomainException()
    {
        var opp = new Opportunity
        {
            OpportunityName = "Zero Dollar Deal",
            CustomerId = 1,
            Amount = 0m,
            Stage = OpportunityStage.Qualification,
            Probability = 20,
            ExpectedCloseDate = DateTime.UtcNow.AddMonths(1)
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateOpportunityAsync(opp));
        Assert.Contains("must be greater than 0", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOpportunity_ProbabilityGreaterThan100_ThrowsDomainException()
    {
        var opp = new Opportunity
        {
            OpportunityName = "Overconfident Deal",
            CustomerId = 1,
            Amount = 50000m,
            Stage = OpportunityStage.Qualification,
            Probability = 150,
            ExpectedCloseDate = DateTime.UtcNow.AddMonths(1)
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateOpportunityAsync(opp));
        Assert.Contains("between 0 and 100", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOpportunity_ActiveDealWithPastCloseDate_ThrowsDomainException()
    {
        var opp = new Opportunity
        {
            OpportunityName = "Past Due Deal",
            CustomerId = 1,
            Amount = 25000m,
            Stage = OpportunityStage.Qualification,
            Probability = 20,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(-5),
            Status = OpportunityStatus.Open
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateOpportunityAsync(opp));
        Assert.Contains("cannot be in the past", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(100000, 50, 50000)]
    [InlineData(50000, 20, 10000)]
    [InlineData(75000, 75, 56250)]
    [InlineData(12345.67, 33, 4074.07)]
    public void Opportunity_WeightedValueFormula_CalculatesAccurately(decimal amount, int probability, decimal expectedWeighted)
    {
        var opp = new Opportunity
        {
            Amount = amount,
            Probability = probability
        };

        Assert.Equal(expectedWeighted, opp.WeightedValue);
    }

    [Fact]
    public async Task UpdateStage_WonStage_SetsProbability100AndStatusWon()
    {
        var opp = new Opportunity
        {
            OpportunityName = "Closing Deal",
            CustomerId = 1,
            Amount = 30000m,
            Stage = OpportunityStage.Negotiation,
            Probability = 75,
            ExpectedCloseDate = DateTime.UtcNow.AddMonths(1)
        };
        var created = await _service.CreateOpportunityAsync(opp);

        var updated = await _service.UpdateStageAsync(created.OpportunityId, OpportunityStage.Won);

        Assert.Equal(OpportunityStage.Won, updated.Stage);
        Assert.Equal(OpportunityStatus.Won, updated.Status);
        Assert.Equal(100, updated.Probability);
    }

    [Fact]
    public async Task UpdateStage_LostStage_SetsProbability0AndStatusLost()
    {
        var opp = new Opportunity
        {
            OpportunityName = "Lost Deal",
            CustomerId = 1,
            Amount = 15000m,
            Stage = OpportunityStage.Proposal,
            Probability = 50,
            ExpectedCloseDate = DateTime.UtcNow.AddMonths(1)
        };
        var created = await _service.CreateOpportunityAsync(opp);

        var updated = await _service.UpdateStageAsync(created.OpportunityId, OpportunityStage.Lost);

        Assert.Equal(OpportunityStage.Lost, updated.Stage);
        Assert.Equal(OpportunityStatus.Lost, updated.Status);
        Assert.Equal(0, updated.Probability);
    }

    [Fact]
    public async Task Opportunity_SalesExecutive_CannotAccessUnassignedDeal()
    {
        // Deal assigned to sales-2
        var opp = new Opportunity
        {
            OpportunityName = "Competitor Sales Rep Deal",
            CustomerId = 1,
            Amount = 20000m,
            Stage = OpportunityStage.Proposal,
            Probability = 50,
            ExpectedCloseDate = DateTime.UtcNow.AddMonths(1),
            AssignedToId = "sales-2"
        };
        var created = await _service.CreateOpportunityAsync(opp);

        // Now test access as sales-1
        var salesUser = MockCurrentUserService.CreateSalesExecutive("sales-1");
        var restrictedService = new OpportunityService(_context, salesUser, _auditMock.Object, _customerService);

        var canAccess = await restrictedService.CanAccessOpportunityAsync(created.OpportunityId);
        Assert.False(canAccess);
    }

    [Fact]
    public async Task CreateOpportunity_WithNonExistentLeadId_ThrowsDomainException()
    {
        var opp = new Opportunity
        {
            OpportunityName = "Invalid Lead Deal",
            CustomerId = 1,
            LeadId = 99999, // Non-existent lead
            Amount = 15000m,
            Stage = OpportunityStage.Proposal,
            Probability = 30,
            ExpectedCloseDate = DateTime.UtcNow.AddMonths(1)
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateOpportunityAsync(opp));
        Assert.Contains("Selected lead does not exist", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
