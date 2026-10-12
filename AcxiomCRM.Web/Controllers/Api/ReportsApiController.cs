using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
[Authorize]
[IgnoreAntiforgeryToken]
public class ReportsApiController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsApiController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("pipeline")]
    [ProducesResponseType(typeof(ApiResponse<List<PipelineReportItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPipelineReport()
    {
        var data = await _reportService.GetPipelineReportAsync();
        return Ok(ApiResponse<List<PipelineReportItemDto>>.Ok(data, "Pipeline report data retrieved."));
    }
}
