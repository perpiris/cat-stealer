using System.Collections.Concurrent;
using CatStealer.Application.Common;
using CatStealer.Application.Enums;

namespace CatStealer.Application.Services.Implementation;

public class JobService : IJobService
{
    private readonly ConcurrentDictionary<string, JobInfo> _jobs = new();

    public string CreateJob()
    {
        var jobId = Guid.NewGuid().ToString();
        var jobInfo = new JobInfo
        {
            Id = jobId,
            Status = JobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _jobs[jobId] = jobInfo;
        return jobId;
    }

    public Task<JobInfo> GetJobAsync(string jobId)
    {
        return Task.FromResult(_jobs.TryGetValue(jobId, out var jobInfo) ? jobInfo : new JobInfo { Id = jobId, Status = JobStatus.NotFound, CreatedAt = DateTime.MinValue });
    }

    public Task SetJobRunningAsync(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var jobInfo))
        {
            jobInfo.Status = JobStatus.Running;
            jobInfo.StartedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task SetJobSucceededAsync(string jobId, object? result)
    {
        if (_jobs.TryGetValue(jobId, out var jobInfo))
        {
            jobInfo.Status = JobStatus.Succeeded;
            jobInfo.FinishedAt = DateTime.UtcNow;
            jobInfo.Result = result;
        }
        return Task.CompletedTask;
    }

    public Task SetJobFailedAsync(string jobId, string errorMessage)
    {
        if (_jobs.TryGetValue(jobId, out var jobInfo))
        {
            jobInfo.Status = JobStatus.Failed;
            jobInfo.FinishedAt = DateTime.UtcNow;
            jobInfo.ErrorMessage = errorMessage;
        }
        return Task.CompletedTask;
    }
}