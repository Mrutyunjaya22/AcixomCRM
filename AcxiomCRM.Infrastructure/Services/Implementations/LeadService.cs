using System.Text.Json;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Infrastructure.Services.Implementations;

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLog;
    private readonly ICustomerService _customerService;

    public LeadService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditLogService auditLog,
        ICustomerService customerService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditLog = auditLog;
        _customerService = customerService;
    }

    private IQueryable<Lead> ApplyScope(IQueryable<Lead> query)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            return query;
        }

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            return query.Where(l => l.AssignedToId == _currentUser.UserId);
        }

        return query.Where(_ => false);
    }

    public async Task<bool> CanAccessLeadAsync(int leadId)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager) return true;
        if (string.IsNullOrEmpty(_currentUser.UserId)) return false;

        return await _context.Leads
            .AsNoTracking()
            .AnyAsync(l => l.LeadId == leadId && l.AssignedToId == _currentUser.UserId);
    }

    public bool IsValidStatusTransition(LeadStatus currentStatus, LeadStatus newStatus)
    {
        if (currentStatus == newStatus) return true;

        // Converted leads are permanent and immutable in status
        if (currentStatus == LeadStatus.Converted) return false;

        // Conversion cannot be done via a simple status dropdown edit; it must go through ConvertLeadAsync
        if (newStatus == LeadStatus.Converted) return false;

        return (currentStatus, newStatus) switch
        {
            (LeadStatus.New, LeadStatus.Contacted) => true,
            (LeadStatus.New, LeadStatus.Qualified) => true,
            (LeadStatus.New, LeadStatus.Unqualified) => true,
            (LeadStatus.New, LeadStatus.Lost) => true,

            (LeadStatus.Contacted, LeadStatus.Qualified) => true,
            (LeadStatus.Contacted, LeadStatus.Unqualified) => true,
            (LeadStatus.Contacted, LeadStatus.Lost) => true,

            (LeadStatus.Qualified, LeadStatus.Unqualified) => true,
            (LeadStatus.Qualified, LeadStatus.Lost) => true,

            (LeadStatus.Unqualified, LeadStatus.Contacted) => true,
            (LeadStatus.Unqualified, LeadStatus.Qualified) => true,

            (LeadStatus.Lost, LeadStatus.Contacted) => true,
            (LeadStatus.Lost, LeadStatus.Qualified) => true,

            _ => false
        };
    }

    public async Task<(List<Lead> Leads, int TotalCount)> GetLeadsAsync(
        string? searchTerm = null,
        LeadStatus? status = null,
        LeadSource? source = null,
        string? assignedToId = null,
        string? sortBy = null,
        bool sortDescending = false,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.Leads
            .Include(l => l.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyScope(query);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(l =>
                l.LeadName.ToLower().Contains(term) ||
                l.Email.ToLower().Contains(term) ||
                l.Phone.ToLower().Contains(term) ||
                (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                l.LeadCode.ToLower().Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (source.HasValue)
        {
            query = query.Where(l => l.Source == source.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedToId))
        {
            query = query.Where(l => l.AssignedToId == assignedToId);
        }

        query = (sortBy?.ToLower()) switch
        {
            "name" => sortDescending ? query.OrderByDescending(l => l.LeadName) : query.OrderBy(l => l.LeadName),
            "status" => sortDescending ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status),
            "expectedvalue" => sortDescending ? query.OrderByDescending(l => l.ExpectedValue) : query.OrderBy(l => l.ExpectedValue),
            "priority" => sortDescending ? query.OrderByDescending(l => l.Priority) : query.OrderBy(l => l.Priority),
            _ => sortDescending ? query.OrderBy(l => l.CreatedDate) : query.OrderByDescending(l => l.CreatedDate)
        };

        var totalCount = await query.CountAsync();

        var leads = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (leads, totalCount);
    }

    public async Task<Lead?> GetLeadByIdAsync(int leadId)
    {
        var query = _context.Leads
            .Include(l => l.AssignedTo)
            .Include(l => l.ConvertedCustomer)
            .Include(l => l.ConvertedOpportunity)
            .AsQueryable();

        query = ApplyScope(query);

        return await query.FirstOrDefaultAsync(l => l.LeadId == leadId);
    }

    public async Task<Lead?> GetLeadWithDetailsAsync(int leadId)
    {
        var query = _context.Leads
            .Include(l => l.AssignedTo)
            .Include(l => l.ConvertedCustomer)
            .Include(l => l.ConvertedOpportunity)
            .Include(l => l.FollowUps.OrderByDescending(f => f.FollowUpDate))
            .Include(l => l.Activities.OrderByDescending(a => a.ActivityDate))
            .AsSplitQuery()
            .AsQueryable();

        query = ApplyScope(query);

        return await query.FirstOrDefaultAsync(l => l.LeadId == leadId);
    }

    public async Task<Lead> CreateLeadAsync(Lead lead)
    {
        lead.LeadName = lead.LeadName.Trim();
        lead.Email = lead.Email.Trim().ToLower();
        lead.Phone = lead.Phone.Trim();

        if (lead.ExpectedValue < 0)
        {
            throw new DomainException("Expected value cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(lead.LeadCode))
        {
            var maxId = (await _context.Leads.MaxAsync(l => (int?)l.LeadId)) ?? 0;
            lead.LeadCode = $"LEAD-{(maxId + 1):D5}";
        }

        lead.CreatedDate = DateTime.UtcNow;

        if (_currentUser.IsSalesExecutive && string.IsNullOrWhiteSpace(lead.AssignedToId))
        {
            lead.AssignedToId = _currentUser.UserId;
        }

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Create",
            module: "Lead",
            entityName: nameof(Lead),
            recordId: lead.LeadId.ToString(),
            oldValue: null,
            newValue: JsonSerializer.Serialize(new { lead.LeadCode, lead.LeadName, lead.Email, lead.Status, lead.ExpectedValue }),
            details: $"Lead {lead.LeadName} ({lead.LeadCode}) created."
        );

        return lead;
    }

    public async Task<Lead> UpdateLeadAsync(Lead lead)
    {
        var existing = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == lead.LeadId);
        if (existing == null)
        {
            throw new DomainException($"Lead with ID {lead.LeadId} was not found.");
        }

        if (!await CanAccessLeadAsync(existing.LeadId))
        {
            throw new UnauthorizedAccessException("You are not authorized to update this lead.");
        }

        if (!IsValidStatusTransition(existing.Status, lead.Status))
        {
            throw new DomainException($"Invalid status transition from '{existing.Status}' to '{lead.Status}'.");
        }

        if (lead.ExpectedValue < 0)
        {
            throw new DomainException("Expected value cannot be negative.");
        }

        var oldSnapshot = JsonSerializer.Serialize(new { existing.LeadName, existing.Email, existing.Phone, existing.Status, existing.Priority, existing.ExpectedValue, existing.AssignedToId });

        existing.LeadName = lead.LeadName.Trim();
        existing.Email = lead.Email.Trim().ToLower();
        existing.Phone = lead.Phone.Trim();
        existing.CompanyName = lead.CompanyName?.Trim();
        existing.Source = lead.Source;
        existing.Status = lead.Status;
        existing.Priority = lead.Priority;
        existing.ExpectedValue = lead.ExpectedValue;
        existing.Notes = lead.Notes?.Trim();
        existing.ModifiedDate = DateTime.UtcNow;

        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            existing.AssignedToId = lead.AssignedToId;
        }

        await _context.SaveChangesAsync();

        var newSnapshot = JsonSerializer.Serialize(new { existing.LeadName, existing.Email, existing.Phone, existing.Status, existing.Priority, existing.ExpectedValue, existing.AssignedToId });

        await _auditLog.LogAsync(
            action: "Update",
            module: "Lead",
            entityName: nameof(Lead),
            recordId: existing.LeadId.ToString(),
            oldValue: oldSnapshot,
            newValue: newSnapshot,
            details: $"Lead {existing.LeadName} ({existing.LeadCode}) updated."
        );

        return existing;
    }

    public async Task<bool> DeleteOrDeactivateLeadAsync(int leadId)
    {
        var existing = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId);
        if (existing == null) return false;

        if (!await CanAccessLeadAsync(leadId))
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this lead.");
        }

        if (existing.Status == LeadStatus.Converted)
        {
            throw new DomainException("Converted leads cannot be deleted or lost; they are part of customer history.");
        }

        existing.Status = LeadStatus.Lost;
        existing.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Deactivate",
            module: "Lead",
            entityName: nameof(Lead),
            recordId: existing.LeadId.ToString(),
            oldValue: null,
            newValue: LeadStatus.Lost.ToString(),
            details: $"Lead {existing.LeadName} ({existing.LeadCode}) marked as Lost."
        );

        return true;
    }

    public async Task<(Customer Customer, Opportunity? Opportunity)> ConvertLeadAsync(int leadId, string? notes = null)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId);
        if (lead == null)
        {
            throw new DomainException($"Lead with ID {leadId} was not found.");
        }

        if (!await CanAccessLeadAsync(leadId))
        {
            throw new UnauthorizedAccessException("You are not authorized to convert this lead.");
        }

        if (lead.Status == LeadStatus.Converted)
        {
            throw new DomainException($"Lead '{lead.LeadName}' ({lead.LeadCode}) is already converted.");
        }

        if (lead.Status == LeadStatus.Lost || lead.Status == LeadStatus.Unqualified)
        {
            throw new DomainException($"Lead '{lead.LeadName}' has status '{lead.Status}' and cannot be converted.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        // 1. Check or Create Customer
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower());
        if (customer == null)
        {
            var maxCustId = (await _context.Customers.MaxAsync(c => (int?)c.CustomerId)) ?? 0;
            customer = new Customer
            {
                CustomerCode = $"CUST-{(maxCustId + 1):D5}",
                CustomerName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Notes = $"Converted from Lead {lead.LeadCode}. " + notes,
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow,
                CreatedById = _currentUser.UserId,
                AssignedToId = lead.AssignedToId
            };

            if (!await _customerService.IsPhoneUniqueAsync(customer.Phone))
            {
                var basePhone = customer.Phone.Length > 15 ? customer.Phone[..15] : customer.Phone;
                customer.Phone = $"{basePhone}-ext";
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        // 2. Create Opportunity if ExpectedValue > 0
        Opportunity? opportunity = null;
        if (lead.ExpectedValue > 0)
        {
            var maxOppId = (await _context.Opportunities.MaxAsync(o => (int?)o.OpportunityId)) ?? 0;
            opportunity = new Opportunity
            {
                OpportunityCode = $"OPP-{(maxOppId + 1):D5}",
                OpportunityName = $"{lead.LeadName} - Deal",
                CustomerId = customer.CustomerId,
                LeadId = lead.LeadId,
                Amount = lead.ExpectedValue,
                Stage = OpportunityStage.Qualification,
                Probability = 25,
                ExpectedCloseDate = DateTime.UtcNow.AddMonths(1),
                Status = OpportunityStatus.Open,
                CreatedDate = DateTime.UtcNow,
                AssignedToId = lead.AssignedToId,
                Notes = $"Created automatically from Lead conversion {lead.LeadCode}."
            };

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();
        }

        // 3. Mark Lead as Converted
        lead.Status = LeadStatus.Converted;
        lead.ConvertedCustomerId = customer.CustomerId;
        lead.ConvertedOpportunityId = opportunity?.OpportunityId;
        lead.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        await _auditLog.LogAsync(
            action: "Convert",
            module: "Lead",
            entityName: nameof(Lead),
            recordId: lead.LeadId.ToString(),
            oldValue: LeadStatus.Qualified.ToString(),
            newValue: LeadStatus.Converted.ToString(),
            details: $"Lead {lead.LeadName} converted to Customer {customer.CustomerCode}" + (opportunity != null ? $" and Opportunity {opportunity.OpportunityCode}." : ".")
        );

        return (customer, opportunity);
    }
}
