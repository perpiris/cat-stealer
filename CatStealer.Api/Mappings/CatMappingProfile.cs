using CatStealer.Api.Contracts.Responses;
using CatStealer.Domain.Entities;

namespace CatStealer.Api.Mappings;

public static class CatMappingProfile
{
    public static CatResponse MapToResponse(this Cat cat)
    {
        return new CatResponse
        {
            Id = cat.Id,
            CatId =  cat.CatId,
            Width =  cat.Width,
            Height = cat.Height,
            Image =  cat.Image,
            Tags =  cat.Tags.Select(x => x.Name).ToList(),
        };
    }
}