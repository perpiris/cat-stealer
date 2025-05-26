using System.Net.Http.Json;
using CatStealer.Application.Common;
using CatStealer.Application.Data;
using CatStealer.Application.Enums;
using CatStealer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CatStealer.Application.Services.Implementation;

public class CatService : ICatService
{
    private readonly DataContext _dataContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IJobService _jobService;
    private const string DefaultImageStorageFolder = "CatImages";
    private readonly string _apiKey;

    public CatService(DataContext dataContext, IHttpClientFactory httpClientFactory, IConfiguration configuration, 
        IBackgroundTaskQueue taskQueue, IJobService jobService)
    {
        ArgumentNullException.ThrowIfNull(dataContext);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(taskQueue);
        ArgumentNullException.ThrowIfNull(jobService);
        
        _dataContext = dataContext;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _taskQueue = taskQueue;
        _jobService = jobService;

        var apiKey = _configuration["CaaS:ApiKey"];
        ArgumentNullException.ThrowIfNull(apiKey);
        
        _apiKey = apiKey;
    }
    
    public async Task<Result<string>> EnqueueFetchCatsJobAsync(int count)
    {
        if (count <= 0)
        {
            return Result.Failure<string>("Number of cats to fetch must be greater than zero.");
        }

        var jobId = _jobService.CreateJob();

        _taskQueue.QueueBackgroundWorkItem(async (serviceProvider, cancellationToken) =>
        {
            ICatService scopedCatService;
            try
            {
                scopedCatService = serviceProvider.GetRequiredService<ICatService>();
            }
            catch (Exception)
            {
                await _jobService.SetJobFailedAsync(jobId, "Internal setup error: Could not resolve cat service for processing.");
                return;
            }

            try
            {
                await scopedCatService.ProcessFetchCatsJobAsync(jobId, count, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await _jobService.SetJobFailedAsync(jobId, "Job processing was cancelled.");
            }
            catch (Exception ex)
            {
                await _jobService.SetJobFailedAsync(jobId, $"Critical error during job execution: {ex.Message}");
            }
        });

        return await Task.FromResult(Result.Success(jobId));
    }
    
    public async Task ProcessFetchCatsJobAsync(string jobId, int count, CancellationToken cancellationToken)
    {
        await _jobService.SetJobRunningAsync(jobId);

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            List<CaasCatImage>? caasImages;

            try
            {
                caasImages = await httpClient.GetFromJsonAsync<List<CaasCatImage>>(
                    $"https://api.thecatapi.com/v1/images/search?limit={count}&has_breeds=1&mime_types=jpg,png", cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await _jobService.SetJobFailedAsync(jobId, "Fetching from CaaS API was cancelled.");
                return;
            }
            catch (Exception ex)
            {
                await _jobService.SetJobFailedAsync(jobId, $"Error fetching images from CaaS API: {ex.Message}");
                return;
            }

            if (caasImages == null || caasImages.Count == 0)
            {
                await _jobService.SetJobSucceededAsync(jobId, 0);
                return;
            }
            
            cancellationToken.ThrowIfCancellationRequested();

            var catsToAdd = new List<Cat>();
            var existingCatApiIdList = await _dataContext.Cats.Select(x => x.CatId).ToListAsync(cancellationToken);
            var existingTagsMap = await _dataContext.Tags.ToDictionaryAsync(x => x.Name.ToLowerInvariant(), x => x, cancellationToken);

            var imageStorageAbsolutePath = GetImageStorageBasePath();
            Directory.CreateDirectory(imageStorageAbsolutePath);

            foreach (var caasImage in caasImages.Where(x => !string.IsNullOrWhiteSpace(x.Id) && !string.IsNullOrWhiteSpace(x.Url) && !existingCatApiIdList.Contains(x.Id)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string imagePath;
                try
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(caasImage.Url, cancellationToken);
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
                    
                    await File.WriteAllBytesAsync(imagePath, imageBytes, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    await _jobService.SetJobFailedAsync(jobId, "Image processing was cancelled.");
                    return;
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
                    Tags = new List<Tag>(),
                    Created = DateTime.UtcNow
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
                            var newTag = new Tag { Name = tagName, Created = DateTime.UtcNow };
                            existingTagsMap[tagNameLower] = newTag; 
                            newCat.Tags.Add(newTag);
                        }
                    }
                }
                catsToAdd.Add(newCat);
                existingCatApiIdList.Add(newCat.CatId);
            }

            if (catsToAdd.Count == 0)
            {
                await _jobService.SetJobSucceededAsync(jobId, 0);
                return;
            }

            try
            {
                await _dataContext.Cats.AddRangeAsync(catsToAdd, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);
                var newCatsAddedCount = catsToAdd.Count;
                await _jobService.SetJobSucceededAsync(jobId, newCatsAddedCount);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await _jobService.SetJobFailedAsync(jobId, "Database save operation was cancelled.");
            }
            catch (Exception ex)
            {
                await _jobService.SetJobFailedAsync(jobId, $"Error saving cats to database: {ex.Message}");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var job = await _jobService.GetJobAsync(jobId);
            if (job.Status == JobStatus.Running)
            {
                 await _jobService.SetJobFailedAsync(jobId, "Job processing was cancelled.");
            }
        }
        catch (Exception ex)
        {
            var job = await _jobService.GetJobAsync(jobId);
            if (job.Status == JobStatus.Running)
            {
                await _jobService.SetJobFailedAsync(jobId, $"An unexpected error occurred: {ex.Message}");
            }
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