using ExpressBus.Transfer;

namespace ExpressBus.Provider;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topicTracker = new TopicTracker();
        var broker = new BrokerServer(
            new Address("0.0.0.0", 9000), // TODO: configurable via appsettings
            topicTracker,
            connection => new RequestHandler(connection, topicTracker, logger),
            logger);

        logger.LogInformation("Starting ExpressBus broker on 0.0.0.0:9000");
        var listenTask = broker.ListenAsync();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            logger.LogInformation("Shutting down ExpressBus broker");
            broker.StopAsync();
            await listenTask.ConfigureAwait(false);
        }
    }
}
