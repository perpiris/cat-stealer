using System.Net;
using CatStealer.Application.Common;
using CatStealer.Application.Enums;
using CatStealer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatStealer.Api.Controllers;

[ApiController]
[Route("jobs")]
public class JobController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobController(IJobService jobService)
    {
        ArgumentNullException.ThrowIfNull(jobService);
        
        _jobService = jobService;
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(JobInfo), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetJobStatus(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Job ID must be provided.");
        }

        var jobInfo = await _jobService.GetJobAsync(id);

        if (jobInfo.Status == JobStatus.NotFound)
        {
            return NotFound($"Job with ID '{id}' not found.");
        }

        return Ok(jobInfo);
    }
}