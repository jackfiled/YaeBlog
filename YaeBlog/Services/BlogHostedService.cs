namespace YaeBlog.Services;

public class BlogHostedService(
    ILogger<BlogHostedService> logger,
    RendererService rendererService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Failed to load cache, render essays.");
        await rendererService.RenderAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
