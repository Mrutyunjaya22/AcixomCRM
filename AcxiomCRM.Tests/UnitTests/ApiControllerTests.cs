using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Web.Controllers.Api;
using AcxiomCRM.Web.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AcxiomCRM.Tests.UnitTests;

public class ApiControllerTests
{
    private readonly Mock<ICustomerService> _customerServiceMock;
    private readonly Mock<ILeadService> _leadServiceMock;
    private readonly Mock<IOpportunityService> _oppServiceMock;

    public ApiControllerTests()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _leadServiceMock = new Mock<ILeadService>();
        _oppServiceMock = new Mock<IOpportunityService>();
    }

    [Fact]
    public async Task CustomersApi_GetCustomerById_ExistingId_Returns200Ok()
    {
        var customer = new Customer
        {
            CustomerId = 1,
            CustomerCode = "CUST-001",
            CustomerName = "John Doe",
            Email = "john@example.com",
            Phone = "+1-555-0100"
        };
        _customerServiceMock.Setup(s => s.GetCustomerByIdAsync(1)).ReturnsAsync(customer);

        var controller = new CustomersApiController(_customerServiceMock.Object);
        var result = await controller.GetCustomerById(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<CustomerDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("John Doe", response.Data?.CustomerName);
    }

    [Fact]
    public async Task CustomersApi_GetCustomerById_NonExistent_Returns404NotFound()
    {
        _customerServiceMock.Setup(s => s.GetCustomerByIdAsync(999)).ReturnsAsync((Customer?)null);
        _customerServiceMock.Setup(s => s.CanAccessCustomerAsync(999)).ReturnsAsync(true);

        var controller = new CustomersApiController(_customerServiceMock.Object);
        var result = await controller.GetCustomerById(999);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task CustomersApi_CreateCustomer_DuplicateEmail_Returns409Conflict()
    {
        var dto = new CustomerCreateDto
        {
            CustomerName = "Dupe Client",
            Email = "dupe@example.com",
            Phone = "+1-555-0199"
        };

        _customerServiceMock.Setup(s => s.CreateCustomerAsync(It.IsAny<Customer>()))
            .ThrowsAsync(new DomainException("Customer with email 'dupe@example.com' already exists."));

        var controller = new CustomersApiController(_customerServiceMock.Object);
        var result = await controller.CreateCustomer(dto);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<string>>(conflictResult.Value);
        Assert.False(response.Success);
        Assert.Contains("already exists", response.Message);
    }

    [Fact]
    public async Task OpportunitiesApi_CreateOpportunity_ZeroAmount_Returns400BadRequest()
    {
        var dto = new OpportunityCreateDto
        {
            OpportunityName = "Zero Deal",
            CustomerId = 1,
            Amount = 0m,
            Probability = 20,
            ExpectedCloseDate = DateTime.UtcNow.AddMonths(1)
        };

        var controller = new OpportunitiesApiController(_oppServiceMock.Object);
        var result = await controller.CreateOpportunity(dto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("greater than 0", response.Message);
    }

    [Fact]
    public async Task OpportunitiesApi_CreateOpportunity_InvalidProbability_Returns400BadRequest()
    {
        var dto = new OpportunityCreateDto
        {
            OpportunityName = "Wild Deal",
            CustomerId = 1,
            Amount = 10000m,
            Probability = 120, // Invalid > 100
            ExpectedCloseDate = DateTime.UtcNow.AddMonths(1)
        };

        var controller = new OpportunitiesApiController(_oppServiceMock.Object);
        var result = await controller.CreateOpportunity(dto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("between 0 and 100", response.Message);
    }
}
