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
}