using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Produces("application/json")]
[Authorize]
[IgnoreAntiforgeryToken]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;

    public OpportunitiesApiController(IOpportunityService opportunityService)
    {
        _opportunityService = opportunityService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OpportunityDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpportunities([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (opps, totalCount) = await _opportunityService.GetOpportunitiesAsync(
            searchTerm: search,
            page: page,
            pageSize: pageSize);

        var dtos = opps.Select(o => new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityCode = o.OpportunityCode,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer.CustomerName,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            WeightedValue = o.WeightedValue,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            AssignedToId = o.AssignedToId,
            AssignedToName = o.AssignedTo?.FullName,
            CreatedDate = o.CreatedDate
        }).ToList();

        return Ok(ApiResponse<List<OpportunityDto>>.Ok(dtos, $"Retrieved {dtos.Count} of {totalCount} opportunities."));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOpportunityById(int id)
    {
        var opportunity = await _opportunityService.GetOpportunityByIdAsync(id);
        if (opportunity == null)
        {
            if (!await _opportunityService.CanAccessOpportunityAsync(id)) return Forbid();
            return NotFound(ApiResponse<string>.Fail("Opportunity not found."));
        }

        var dto = new OpportunityDto
        {
            OpportunityId = opportunity.OpportunityId,
            OpportunityCode = opportunity.OpportunityCode,
            OpportunityName = opportunity.OpportunityName,
            CustomerId = opportunity.CustomerId,
            CustomerName = opportunity.Customer.CustomerName,
            LeadId = opportunity.LeadId,
            Amount = opportunity.Amount,
            Stage = opportunity.Stage,
            Probability = opportunity.Probability,
            WeightedValue = opportunity.WeightedValue,
            ExpectedCloseDate = opportunity.ExpectedCloseDate,
            Status = opportunity.Status,
            AssignedToId = opportunity.AssignedToId,
            AssignedToName = opportunity.AssignedTo?.FullName,
            CreatedDate = opportunity.CreatedDate
        };

        return Ok(ApiResponse<OpportunityDto>.Ok(dto));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOpportunity([FromBody] OpportunityCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        if (dto.Amount <= 0)
        {
            return BadRequest(ApiResponse<string>.Fail("Opportunity Amount must be greater than 0."));
        }

        if (dto.Probability < 0 || dto.Probability > 100)
        {
            return BadRequest(ApiResponse<string>.Fail("Probability must be between 0 and 100."));
        }

        try
        {
            var opportunity = new Opportunity
            {
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Notes = dto.Notes,
                AssignedToId = dto.AssignedToId
            };

            var created = await _opportunityService.CreateOpportunityAsync(opportunity);

            var resultDto = new OpportunityDto
            {
                OpportunityId = created.OpportunityId,
                OpportunityCode = created.OpportunityCode,
                OpportunityName = created.OpportunityName,
                CustomerId = created.CustomerId,
                CustomerName = created.Customer?.CustomerName ?? "",
                LeadId = created.LeadId,
                Amount = created.Amount,
                Stage = created.Stage,
                Probability = created.Probability,
                WeightedValue = created.WeightedValue,
                ExpectedCloseDate = created.ExpectedCloseDate,
                Status = created.Status,
                AssignedToId = created.AssignedToId,
                CreatedDate = created.CreatedDate
            };

            return CreatedAtAction(nameof(GetOpportunityById), new { id = created.OpportunityId }, ApiResponse<OpportunityDto>.Ok(resultDto, "Opportunity created."));
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOpportunity(int id, [FromBody] OpportunityUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var opportunity = new Opportunity
            {
                OpportunityId = id,
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Notes = dto.Notes,
                AssignedToId = dto.AssignedToId
            };

            var updated = await _opportunityService.UpdateOpportunityAsync(opportunity);

            var resultDto = new OpportunityDto
            {
                OpportunityId = updated.OpportunityId,
                OpportunityCode = updated.OpportunityCode,
                OpportunityName = updated.OpportunityName,
                CustomerId = updated.CustomerId,
                CustomerName = updated.Customer?.CustomerName ?? "",
                LeadId = updated.LeadId,
                Amount = updated.Amount,
                Stage = updated.Stage,
                Probability = updated.Probability,
                WeightedValue = updated.WeightedValue,
                ExpectedCloseDate = updated.ExpectedCloseDate,
                Status = updated.Status,
                AssignedToId = updated.AssignedToId,
                CreatedDate = updated.CreatedDate
            };

            return Ok(ApiResponse<OpportunityDto>.Ok(resultDto, "Opportunity updated."));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }
}
