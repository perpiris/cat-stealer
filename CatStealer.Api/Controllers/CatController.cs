using System.Net;
using CatStealer.Application.Common;
using CatStealer.Application.Services;
using CatStealer.Domain.Entities;
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
    
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Cat), (int)HttpStatusCode.OK)]
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
        
        return Ok(result.Data);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Cat>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetPaginatedCats(int pageNumber = 1, int pageSize = 10, string? tag = null)
    {
        var result = await _catService.GetPaginatedCatsAsync(pageNumber, pageSize, tag);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }
        
        return Ok(result.Data);
    }
}