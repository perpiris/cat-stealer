using CatStealer.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CatStealer.Application.BackgroundServices;

public class CatFetchingBackgroundService : BackgroundService 
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;

    public CatFetchingBackgroundService(
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<IServiceProvider, CancellationToken, Task>? workItem;
            try
            {
                workItem = await _taskQueue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; 
            }
            catch (Exception)
            {
                continue;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}