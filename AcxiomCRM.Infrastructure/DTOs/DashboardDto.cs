namespace AcxiomCRM.Infrastructure.DTOs;

public class DashboardDto
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public int PendingFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }

    public List<string> LeadStatusLabels { get; set; } = new();
    public List<int> LeadStatusCounts { get; set; } = new();

    public List<string> OpportunityStageLabels { get; set; } = new();
    public List<decimal> OpportunityStageAmounts { get; set; } = new();
    public List<int> OpportunityStageCounts { get; set; } = new();

    public List<string> MonthlySalesLabels { get; set; } = new();
    public List<decimal> MonthlySalesValues { get; set; } = new();

    public string DateFilter { get; set; } = "All";
}
