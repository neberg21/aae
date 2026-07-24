using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Module.AI.Chat.Jobs;

public record ExecuteJobContext<T>(IServiceProvider Services, T Item);

public abstract class ExecuteJob<T> : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExecuteJobChannel<T> _channel;
    private readonly ILogger<ExecuteJob<T>> _logger;

    protected ExecuteJob(
        ILogger<ExecuteJob<T>> logger,
        IServiceProvider serviceProvider,
        ExecuteJobChannel<T> channel)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _channel = channel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting ExecuteJob<{JobType}>", typeof(T).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await _channel.WaitToReadAsync(stoppingToken))
                {
                    var serviceProvider = _serviceProvider.CreateScope().ServiceProvider;
                    var item = await _channel.ReadAsync(stoppingToken);
                    var context = new ExecuteJobContext<T>(serviceProvider, item);
                    await HandleItem(context, stoppingToken);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error in ExecuteJob<{JobType}>", typeof(T).Name);
            }
        }

        _logger.LogInformation("Stopping ExecuteJob<{JobType}>", typeof(T).Name);
    }

    protected abstract Task HandleItem(ExecuteJobContext<T> context, CancellationToken cancellationToken);
}