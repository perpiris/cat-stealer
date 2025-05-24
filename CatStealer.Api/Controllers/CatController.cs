using CatStealer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatStealer.Api.Controllers;

[Route("cats")]
public class CatController : ControllerBase
{
    private readonly ICatService _catService;

    public CatController(ICatService catService)
    {
        ArgumentNullException.ThrowIfNull(catService);
        
        _catService = catService;
    }
}