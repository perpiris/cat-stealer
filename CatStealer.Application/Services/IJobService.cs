using CatStealer.Application.Common;

namespace CatStealer.Application.Services;

public interface IJobService
{
    string CreateJob();
    
    Task<JobInfo> GetJobAsync(string jobId);
    
    Task SetJobRunningAsync(string jobId);
    
    Task SetJobSucceededAsync(string jobId, object? result);
    
    Task SetJobFailedAsync(string jobId, string errorMessage);
}