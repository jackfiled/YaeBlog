using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YaeBlog.Core.Abstractions;

namespace YaeBlog.Core.Services;

public sealed class BlogHotReloadService(
    RendererService rendererService,
    IEssayContentService essayContentService,
    BlogChangeWatcher watcher,
    ILogger<BlogHotReloadService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BlogHotReloadService is starting.");

        await rendererService.RenderAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Watching file changes...");
            string? changFile = await watcher.WaitForChange(stoppingToken);

            if (changFile is null)
            {
                logger.LogInformation("BlogHotReloadService is stopping.");
                break;
            }

            logger.LogInformation("{} changed, re-rendering.", changFile);
            essayContentService.Clear();
            await rendererService.RenderAsync();
        }
    }
}
