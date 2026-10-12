using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Produces("application/json")]
[Authorize]
[IgnoreAntiforgeryToken]
public class FollowUpsApiController : ControllerBase
{
    private readonly IFollowUpService _followUpService;

    public FollowUpsApiController(IFollowUpService followUpService)
    {
        _followUpService = followUpService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<FollowUpDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowUps([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (followUps, totalCount) = await _followUpService.GetFollowUpsAsync(page: page, pageSize: pageSize);

        var dtos = followUps.Select(f => new FollowUpDto
        {
            FollowUpId = f.FollowUpId,
            RelatedType = f.RelatedType,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer?.CustomerName,
            LeadId = f.LeadId,
            LeadName = f.Lead?.LeadName,
            OpportunityId = f.OpportunityId,
            OpportunityName = f.Opportunity?.OpportunityName,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType,
            Subject = f.Subject,
            Remarks = f.Remarks,
            Status = f.Status,
            AssignedToId = f.AssignedToId,
            AssignedToName = f.AssignedTo?.FullName,
            CreatedDate = f.CreatedDate
        }).ToList();

        return Ok(ApiResponse<List<FollowUpDto>>.Ok(dtos, $"Retrieved {dtos.Count} of {totalCount} follow-ups."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FollowUpDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFollowUp([FromBody] FollowUpCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var followUp = new FollowUp
            {
                RelatedType = dto.RelatedType,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                OpportunityId = dto.OpportunityId,
                FollowUpDate = dto.FollowUpDate,
                FollowUpType = dto.FollowUpType,
                Subject = dto.Subject,
                Remarks = dto.Remarks,
                Status = dto.Status,
                AssignedToId = dto.AssignedToId
            };

            var created = await _followUpService.CreateFollowUpAsync(followUp);

            var resultDto = new FollowUpDto
            {
                FollowUpId = created.FollowUpId,
                RelatedType = created.RelatedType,
                CustomerId = created.CustomerId,
                LeadId = created.LeadId,
                OpportunityId = created.OpportunityId,
                FollowUpDate = created.FollowUpDate,
                FollowUpType = created.FollowUpType,
                Subject = created.Subject,
                Remarks = created.Remarks,
                Status = created.Status,
                AssignedToId = created.AssignedToId,
                CreatedDate = created.CreatedDate
            };

            return StatusCode(StatusCodes.Status201Created, ApiResponse<FollowUpDto>.Ok(resultDto, "Follow-up scheduled."));
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }
}
