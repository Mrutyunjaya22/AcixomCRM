using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;

namespace AcxiomCRM.Infrastructure.Services.Interfaces;

public interface ICustomerService
{
    Task<(List<Customer> Customers, int TotalCount)> GetCustomersAsync(
        string? searchTerm = null,
        CustomerStatus? status = null,
        string? assignedToId = null,
        string? sortBy = null,
        bool sortDescending = false,
        int page = 1,
        int pageSize = 10);

    Task<Customer?> GetCustomerByIdAsync(int customerId);
    Task<Customer?> GetCustomerWithDetailsAsync(int customerId);
    Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null);
    Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer> UpdateCustomerAsync(Customer customer);
    Task<bool> DeleteOrDeactivateCustomerAsync(int customerId);
    Task<bool> CanAccessCustomerAsync(int customerId);
}
