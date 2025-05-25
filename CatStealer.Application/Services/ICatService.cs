using CatStealer.Application.Common;
using CatStealer.Domain.Entities;

namespace CatStealer.Application.Services;

public interface ICatService
{
    Task<Result<Cat?>> GetCatByIdAsync(int catId);

    Task<Result<PagedResult<Cat>>> GetPaginatedCatsAsync(int pageNumber, int pageSize, string? tag);
}