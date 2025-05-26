using System.Net.Http.Json;
using CatStealer.Application.Common;
using CatStealer.Application.Data;
using CatStealer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CatStealer.Application.Services.Implementation;

public class CatService : ICatService
{
    private readonly DataContext _dataContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private const string DefaultImageStorageFolder = "CatImages";
    private readonly string _apiKey;
    
    private class CaasBreed
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Temperament { get; set; }
    }

    private class CaasCatImage
    {
        public required string Id { get; set; }
        public required string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<CaasBreed>? Breeds { get; set; }
    }

    public CatService(DataContext dataContext, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(dataContext);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(configuration);
        
        _dataContext = dataContext;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        
        var apiKey = _configuration["CaaS:ApiKey"];
        ArgumentNullException.ThrowIfNull(apiKey);
        
        _apiKey = apiKey;
    }
    
    public async Task<Result<int>> FetchCatsAsync(int count)
    {
        if (count <= 0)
        {
            return Result.Failure<int>("Number of cats to fetch must be greater than zero.");
        }

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        List<CaasCatImage>? caasImages;

        try
        {
            caasImages = await httpClient.GetFromJsonAsync<List<CaasCatImage>>(
                $"https://api.thecatapi.com/v1/images/search?limit={count}&has_breeds=1&mime_types=jpg,png");
        }
        catch (Exception ex)
        {
            return Result.Failure<int>($"Error fetching images from CaaS API: {ex.Message}");
        }

        if (caasImages == null || caasImages.Count == 0)
        {
            return Result.Success(0);
        }

        var catsToAdd = new List<Cat>();
        
        var existingCatApiIds = _dataContext.Cats.Select(x => x.CatId).ToHashSet();
        var existingTagsMap = await _dataContext.Tags.ToDictionaryAsync(x => x.Name.ToLowerInvariant(), x => x);

        var imageStorageAbsolutePath = GetImageStorageBasePath();
        Directory.CreateDirectory(imageStorageAbsolutePath);

        foreach (var caasImage in caasImages.Where(x => !string.IsNullOrWhiteSpace(x.Id) && !string.IsNullOrWhiteSpace(x.Url) && !existingCatApiIds.Contains(x.Id)))
        {
            string imagePath;
            try
            {
                var imageBytes = await httpClient.GetByteArrayAsync(caasImage.Url);
                if (imageBytes.Length == 0)
                {
                    continue;
                }
                
                var fileExtension = Path.GetExtension(caasImage.Url);
                if (string.IsNullOrEmpty(fileExtension) || fileExtension.Length > 5) 
                {
                    fileExtension = ".jpg"; 
                }
                var fileName = $"{caasImage.Id}{fileExtension}";
                imagePath = Path.Combine(imageStorageAbsolutePath, fileName);
                
                await File.WriteAllBytesAsync(imagePath, imageBytes);
            }
            catch (Exception)
            {
                continue; 
            }

            var newCat = new Cat
            {
                CatId = caasImage.Id,
                Width = caasImage.Width,
                Height = caasImage.Height,
                Image = imagePath,
                Tags = new List<Tag>()
            };

            if (caasImage.Breeds != null && caasImage.Breeds.Count != 0 && !string.IsNullOrWhiteSpace(caasImage.Breeds.First().Temperament))
            {
                var temperamentString = caasImage.Breeds.First().Temperament;
                var tagNames = temperamentString.Split(',')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var tagName in tagNames)
                {
                    var tagNameLower = tagName.ToLowerInvariant();
                    if (existingTagsMap.TryGetValue(tagNameLower, out var existingTag))
                    {
                        newCat.Tags.Add(existingTag);
                    }
                    else
                    {
                        var newTag = new Tag { Name = tagName };
                        existingTagsMap[tagNameLower] = newTag; 
                        newCat.Tags.Add(newTag);
                    }
                }
            }
            catsToAdd.Add(newCat);
            existingCatApiIds.Add(newCat.CatId);
        }

        if (catsToAdd.Count == 0)
        {
            return Result.Success(0);
        }

        try
        {
            await _dataContext.Cats.AddRangeAsync(catsToAdd);
            await _dataContext.SaveChangesAsync();
            var newCatsAddedCount = catsToAdd.Count;
            return Result.Success(newCatsAddedCount);
        }
        catch (Exception ex)
        {
            return Result.Failure<int>($"Error saving cats to database: {ex.Message}");
        }
    }

    public async Task<Result<Cat?>> GetCatByIdAsync(int catId)
    {
        if (catId < 1)
        {
            return Result.Failure<Cat?>("Invalid cat id");
        }
        
        try
        {
            var cat = await _dataContext.Cats
                .Include(x => x.Tags)
                .Where(x => x.Id == catId)
                .FirstOrDefaultAsync();
            
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
            var query = _dataContext.Cats
                .Include(x => x.Tags).AsNoTracking();
            
            if (!string.IsNullOrEmpty(tag))
            {
                if (!tag.All(char.IsLetter))
                {
                    return Result.Failure<PagedResult<Cat>>("Invalid tag");
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
    
    private string GetImageStorageBasePath()
    {
        var configuredPath = _configuration["FileStorage:BasePath"];
        return !string.IsNullOrWhiteSpace(configuredPath) ? Path.GetFullPath(configuredPath) : Path.Combine(AppContext.BaseDirectory, DefaultImageStorageFolder);
    }
}