using System.Text.Json;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Infrastructure.Services.Implementations;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLog;

    public CustomerService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditLogService auditLog)
    {
        _context = context;
        _currentUser = currentUser;
        _auditLog = auditLog;
    }

    private IQueryable<Customer> ApplyScope(IQueryable<Customer> query)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            return query;
        }

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            return query.Where(c => c.AssignedToId == _currentUser.UserId);
        }

        return query.Where(_ => false);
    }

    public async Task<bool> CanAccessCustomerAsync(int customerId)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager) return true;
        if (string.IsNullOrEmpty(_currentUser.UserId)) return false;

        return await _context.Customers
            .AsNoTracking()
            .AnyAsync(c => c.CustomerId == customerId && c.AssignedToId == _currentUser.UserId);
    }

    public async Task<(List<Customer> Customers, int TotalCount)> GetCustomersAsync(
        string? searchTerm = null,
        CustomerStatus? status = null,
        string? assignedToId = null,
        string? sortBy = null,
        bool sortDescending = false,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.Customers
            .Include(c => c.AssignedTo)
            .Include(c => c.CreatedBy)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyScope(query);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerName.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.Phone.ToLower().Contains(term) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                c.CustomerCode.ToLower().Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedToId))
        {
            query = query.Where(c => c.AssignedToId == assignedToId);
        }

        query = (sortBy?.ToLower()) switch
        {
            "name" => sortDescending ? query.OrderByDescending(c => c.CustomerName) : query.OrderBy(c => c.CustomerName),
            "email" => sortDescending ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            "company" => sortDescending ? query.OrderByDescending(c => c.CompanyName) : query.OrderBy(c => c.CompanyName),
            "status" => sortDescending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            _ => sortDescending ? query.OrderBy(c => c.CreatedDate) : query.OrderByDescending(c => c.CreatedDate)
        };

        var totalCount = await query.CountAsync();

        var customers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (customers, totalCount);
    }

    public async Task<Customer?> GetCustomerByIdAsync(int customerId)
    {
        var query = _context.Customers
            .Include(c => c.AssignedTo)
            .Include(c => c.CreatedBy)
            .AsQueryable();

        query = ApplyScope(query);

        return await query.FirstOrDefaultAsync(c => c.CustomerId == customerId);
    }

    public async Task<Customer?> GetCustomerWithDetailsAsync(int customerId)
    {
        var query = _context.Customers
            .Include(c => c.AssignedTo)
            .Include(c => c.CreatedBy)
            .Include(c => c.Opportunities.OrderByDescending(o => o.CreatedDate))
            .Include(c => c.FollowUps.OrderByDescending(f => f.FollowUpDate))
            .Include(c => c.Activities.OrderByDescending(a => a.ActivityDate))
            .AsSplitQuery()
            .AsQueryable();

        query = ApplyScope(query);

        return await query.FirstOrDefaultAsync(c => c.CustomerId == customerId);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null)
    {
        var cleanEmail = email.Trim().ToLower();
        return !await _context.Customers
            .AnyAsync(c => c.Email.ToLower() == cleanEmail && (!excludeCustomerId.HasValue || c.CustomerId != excludeCustomerId.Value));
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null)
    {
        var cleanPhone = phone.Trim();
        return !await _context.Customers
            .AnyAsync(c => c.Phone == cleanPhone && (!excludeCustomerId.HasValue || c.CustomerId != excludeCustomerId.Value));
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        customer.Email = customer.Email.Trim().ToLower();
        customer.Phone = customer.Phone.Trim();
        customer.CustomerName = customer.CustomerName.Trim();

        if (!await IsEmailUniqueAsync(customer.Email))
        {
            throw new DomainException($"A customer with email '{customer.Email}' already exists.");
        }

        if (!await IsPhoneUniqueAsync(customer.Phone))
        {
            throw new DomainException($"A customer with phone number '{customer.Phone}' already exists.");
        }

        if (string.IsNullOrWhiteSpace(customer.CustomerCode))
        {
            var maxId = (await _context.Customers.MaxAsync(c => (int?)c.CustomerId)) ?? 0;
            customer.CustomerCode = $"CUST-{(maxId + 1):D5}";
        }

        customer.CreatedDate = DateTime.UtcNow;
        customer.CreatedById = _currentUser.UserId;

        if (_currentUser.IsSalesExecutive && string.IsNullOrWhiteSpace(customer.AssignedToId))
        {
            customer.AssignedToId = _currentUser.UserId;
        }

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Create",
            module: "Customer",
            entityName: nameof(Customer),
            recordId: customer.CustomerId.ToString(),
            oldValue: null,
            newValue: JsonSerializer.Serialize(new { customer.CustomerCode, customer.CustomerName, customer.Email, customer.Phone, customer.Status }),
            details: $"Customer {customer.CustomerName} ({customer.CustomerCode}) created."
        );

        return customer;
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
        if (existing == null)
        {
            throw new DomainException($"Customer with ID {customer.CustomerId} was not found.");
        }

        if (!await CanAccessCustomerAsync(existing.CustomerId))
        {
            throw new UnauthorizedAccessException("You are not authorized to update this customer.");
        }

        var cleanEmail = customer.Email.Trim().ToLower();
        var cleanPhone = customer.Phone.Trim();

        if (!await IsEmailUniqueAsync(cleanEmail, customer.CustomerId))
        {
            throw new DomainException($"Another customer with email '{cleanEmail}' already exists.");
        }

        if (!await IsPhoneUniqueAsync(cleanPhone, customer.CustomerId))
        {
            throw new DomainException($"Another customer with phone number '{cleanPhone}' already exists.");
        }

        var oldSnapshot = JsonSerializer.Serialize(new { existing.CustomerName, existing.Email, existing.Phone, existing.Status, existing.AssignedToId });

        existing.CustomerName = customer.CustomerName.Trim();
        existing.Email = cleanEmail;
        existing.Phone = cleanPhone;
        existing.CompanyName = customer.CompanyName?.Trim();
        existing.Address = customer.Address?.Trim();
        existing.City = customer.City?.Trim();
        existing.State = customer.State?.Trim();
        existing.Status = customer.Status;
        existing.Notes = customer.Notes?.Trim();
        existing.ModifiedDate = DateTime.UtcNow;

        // Managers and Admins can reassign; Sales Executives retain current assignment
        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            existing.AssignedToId = customer.AssignedToId;
        }

        await _context.SaveChangesAsync();

        var newSnapshot = JsonSerializer.Serialize(new { existing.CustomerName, existing.Email, existing.Phone, existing.Status, existing.AssignedToId });

        await _auditLog.LogAsync(
            action: "Update",
            module: "Customer",
            entityName: nameof(Customer),
            recordId: existing.CustomerId.ToString(),
            oldValue: oldSnapshot,
            newValue: newSnapshot,
            details: $"Customer {existing.CustomerName} ({existing.CustomerCode}) updated."
        );

        return existing;
    }

    public async Task<bool> DeleteOrDeactivateCustomerAsync(int customerId)
    {
        var existing = await _context.Customers
            .Include(c => c.Opportunities)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (existing == null) return false;

        if (!await CanAccessCustomerAsync(customerId))
        {
            throw new UnauthorizedAccessException("You are not authorized to deactivate or delete this customer.");
        }

        // Deactivation pattern preserves audit trail and history
        existing.Status = CustomerStatus.Inactive;
        existing.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Deactivate",
            module: "Customer",
            entityName: nameof(Customer),
            recordId: existing.CustomerId.ToString(),
            oldValue: CustomerStatus.Active.ToString(),
            newValue: CustomerStatus.Inactive.ToString(),
            details: $"Customer {existing.CustomerName} ({existing.CustomerCode}) was deactivated."
        );

        return true;
    }
}
