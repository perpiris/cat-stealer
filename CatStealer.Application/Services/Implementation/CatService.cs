using CatStealer.Application.Common;
using CatStealer.Application.Data;
using CatStealer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatStealer.Application.Services.Implementation;

public class CatService : ICatService
{
    private readonly DataContext _dataContext;

    public CatService(DataContext dataContext)
    {
        ArgumentNullException.ThrowIfNull(dataContext);
        
        _dataContext = dataContext;
    }

    public async Task<Result<Cat?>> GetCatByIdAsync(int catId)
    {
        if (catId < 1)
        {
            return Result.Failure<Cat?>("Invalid cat id");
        }

        try
        {
            var cat = await _dataContext.Cats.FindAsync(catId);
            return Result.Success(cat);
        }
        catch (Exception ex)
        {
            return Result.Failure<Cat?>($"Error retrieving cat: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<Cat>>> GetPaginatedCatsAsync(int pageNumber, int pageSize, string? tag)
    {
        try
        {
            var query = _dataContext.Cats.AsNoTracking();
            
            if (!string.IsNullOrEmpty(tag))
            {
                if (!tag.All(char.IsLetter))
                {
                    return Result.Failure<PagedResult<Cat>>("Invalid tah");
                }
                
                query = query.Where(c => c.Tags.Any(t => t.Name == tag));
            }
            
            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedResult = new PagedResult<Cat>
            {
                Items = items.ToList(),
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return Result.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return Result.Failure<PagedResult<Cat>>($"Error retrieving cats: {ex.Message}");
        }
    }
}