using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Implementations;
using AcxiomCRM.Tests.TestHelpers;

namespace AcxiomCRM.Tests.UnitTests;

public class DashboardCalculationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly MockCurrentUserService _currentUser;
    private readonly DashboardService _service;

    public DashboardCalculationTests()
    {
        (_context, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        _currentUser = MockCurrentUserService.CreateAdmin();
        _service = new DashboardService(_context, _currentUser);

        // Seed customer
        var customer = new Customer
        {
            CustomerId = 1,
            CustomerCode = "CUST-001",
            CustomerName = "Enterprise Global",
            Email = "info@enterprise.com",
            Phone = "+1-555-8888",
            Status = CustomerStatus.Active
        };
        _context.Customers.Add(customer);

        // Seed 3 Opportunities:
        // 1. Qualification: $50,000 (Open)
        // 2. Proposal: $100,000 (Open)
        // 3. Won: $200,000 (Won - Should NOT count toward open pipeline)
        // 4. Lost: $30,000 (Lost - Should NOT count toward open pipeline)
        _context.Opportunities.AddRange(
            new Opportunity
            {
                OpportunityCode = "OPP-00001",
                OpportunityName = "Deal 1",
                CustomerId = 1,
                Amount = 50000m,
                Stage = OpportunityStage.Qualification,
                Status = OpportunityStatus.Open,
                Probability = 20,
                ExpectedCloseDate = DateTime.UtcNow.AddMonths(1)
            },
            new Opportunity
            {
                OpportunityCode = "OPP-00002",
                OpportunityName = "Deal 2",
                CustomerId = 1,
                Amount = 100000m,
                Stage = OpportunityStage.Proposal,
                Status = OpportunityStatus.Open,
                Probability = 50,
                ExpectedCloseDate = DateTime.UtcNow.AddMonths(2)
            },
            new Opportunity
            {
                OpportunityCode = "OPP-00003",
                OpportunityName = "Deal 3",
                CustomerId = 1,
                Amount = 200000m,
                Stage = OpportunityStage.Won,
                Status = OpportunityStatus.Won,
                Probability = 100,
                ExpectedCloseDate = DateTime.UtcNow.AddMonths(-1)
            },
            new Opportunity
            {
                OpportunityCode = "OPP-00004",
                OpportunityName = "Deal 4",
                CustomerId = 1,
                Amount = 30000m,
                Stage = OpportunityStage.Lost,
                Status = OpportunityStatus.Lost,
                Probability = 0,
                ExpectedCloseDate = DateTime.UtcNow.AddMonths(-2)
            }
        );

        // Seed Leads:
        // 1 Open, 1 Converted
        _context.Leads.AddRange(
            new Lead
            {
                LeadCode = "LEAD-00001",
                LeadName = "Open Lead",
                Email = "open@lead.com",
                Phone = "+1-555-0011",
                Status = LeadStatus.New
            },
            new Lead
            {
                LeadCode = "LEAD-00002",
                LeadName = "Converted Lead",
                Email = "conv@lead.com",
                Phone = "+1-555-0022",
                Status = LeadStatus.Converted
            }
        );

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetDashboardSummary_OpenPipelineValue_ExcludesWonAndLostOpportunities()
    {
        var summary = await _service.GetDashboardMetricsAsync();

        // Open Pipeline should be 50,000 + 100,000 = 150,000 (NOT including 200,000 Won or 30,000 Lost)
        Assert.Equal(150000m, summary.TotalPipelineValue);
        Assert.Equal(2, summary.OpenOpportunities);
        Assert.Equal(1, summary.WonOpportunities);
        Assert.Equal(1, summary.LostOpportunities);
        Assert.Equal(4, summary.TotalOpportunities);
    }

    [Fact]
    public async Task GetDashboardSummary_LeadCounts_AccuratelyDistinguishTotalAndOpen()
    {
        var summary = await _service.GetDashboardMetricsAsync();

        Assert.Equal(2, summary.TotalLeads);
        Assert.Equal(1, summary.OpenLeads);
    }
}
