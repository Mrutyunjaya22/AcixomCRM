using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Produces("application/json")]
[Authorize]
[IgnoreAntiforgeryToken]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;

    public LeadsApiController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LeadDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeads([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (leads, totalCount) = await _leadService.GetLeadsAsync(
            searchTerm: search,
            page: page,
            pageSize: pageSize);

        var dtos = leads.Select(l => new LeadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            AssignedToId = l.AssignedToId,
            AssignedToName = l.AssignedTo?.FullName,
            ConvertedCustomerId = l.ConvertedCustomerId,
            CreatedDate = l.CreatedDate
        }).ToList();

        return Ok(ApiResponse<List<LeadDto>>.Ok(dtos, $"Retrieved {dtos.Count} of {totalCount} leads."));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeadById(int id)
    {
        var lead = await _leadService.GetLeadByIdAsync(id);
        if (lead == null)
        {
            if (!await _leadService.CanAccessLeadAsync(id)) return Forbid();
            return NotFound(ApiResponse<string>.Fail("Lead not found."));
        }

        var dto = new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            AssignedToId = lead.AssignedToId,
            AssignedToName = lead.AssignedTo?.FullName,
            ConvertedCustomerId = lead.ConvertedCustomerId,
            CreatedDate = lead.CreatedDate
        };

        return Ok(ApiResponse<LeadDto>.Ok(dto));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLead([FromBody] LeadCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var lead = new Lead
            {
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Source = dto.Source,
                Status = dto.Status,
                Priority = dto.Priority,
                ExpectedValue = dto.ExpectedValue,
                Notes = dto.Notes,
                AssignedToId = dto.AssignedToId
            };

            var created = await _leadService.CreateLeadAsync(lead);

            var resultDto = new LeadDto
            {
                LeadId = created.LeadId,
                LeadCode = created.LeadCode,
                LeadName = created.LeadName,
                Email = created.Email,
                Phone = created.Phone,
                CompanyName = created.CompanyName,
                Source = created.Source,
                Status = created.Status,
                Priority = created.Priority,
                ExpectedValue = created.ExpectedValue,
                AssignedToId = created.AssignedToId,
                CreatedDate = created.CreatedDate
            };

            return CreatedAtAction(nameof(GetLeadById), new { id = created.LeadId }, ApiResponse<LeadDto>.Ok(resultDto, "Lead created."));
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLead(int id, [FromBody] LeadUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var lead = new Lead
            {
                LeadId = id,
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Source = dto.Source,
                Status = dto.Status,
                Priority = dto.Priority,
                ExpectedValue = dto.ExpectedValue,
                Notes = dto.Notes,
                AssignedToId = dto.AssignedToId
            };

            var updated = await _leadService.UpdateLeadAsync(lead);

            var resultDto = new LeadDto
            {
                LeadId = updated.LeadId,
                LeadCode = updated.LeadCode,
                LeadName = updated.LeadName,
                Email = updated.Email,
                Phone = updated.Phone,
                CompanyName = updated.CompanyName,
                Source = updated.Source,
                Status = updated.Status,
                Priority = updated.Priority,
                ExpectedValue = updated.ExpectedValue,
                AssignedToId = updated.AssignedToId,
                CreatedDate = updated.CreatedDate
            };

            return Ok(ApiResponse<LeadDto>.Ok(resultDto, "Lead updated."));
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
