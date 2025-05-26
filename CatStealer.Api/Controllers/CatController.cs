using System.Net;
using CatStealer.Api.Contracts.Responses;
using CatStealer.Api.Mappings;
using CatStealer.Api.Responses;
using CatStealer.Application.Common;
using CatStealer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatStealer.Api.Controllers;

[ApiController]
[Route("cats")]
public class CatController : ControllerBase
{
    private readonly ICatService _catService;

    public CatController(ICatService catService)
    {
        ArgumentNullException.ThrowIfNull(catService);
        
        _catService = catService;
    }
    
    [HttpPost("fetch")]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Accepted)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> FetchCats()
    {
        const int numberOfCatsToFetch = 25;
        var result = await _catService.EnqueueFetchCatsJobAsync(numberOfCatsToFetch);

        if (!result.IsSuccess || string.IsNullOrEmpty(result.Data))
        {
            return BadRequest(result.Error);
        }
        
        var jobId = result.Data;
        var statusUrl = Url.Action(nameof(JobController.GetJobStatus), "Job", new { id = jobId }, Request.Scheme);
        
        return Accepted(statusUrl, new JobAcceptedResponse { JobId = jobId, StatusUrl = statusUrl });
    }
    
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CatResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetCatById(int id)
    {
        var result = await _catService.GetCatByIdAsync(id);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }
        
        if (result.Data == null)
        {
            return NotFound($"Cat with ID {id} was not found.");
        }
        
        return Ok(result.Data.MapToResponse());
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CatResponse>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetPaginatedCats(int pageNumber = 1, int pageSize = 10, string? tag = null)
    {
        var result = await _catService.GetPaginatedCatsAsync(pageNumber, pageSize, tag);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }
        
        return Ok(result.Data.Items.Select(x => x.MapToResponse()));
    }
}