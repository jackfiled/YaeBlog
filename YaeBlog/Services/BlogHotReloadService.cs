using YaeBlog.Abstraction;

namespace YaeBlog.Services;

public sealed class BlogHotReloadService(
    RendererService rendererService,
    IEssayContentService essayContentService,
    BlogChangeWatcher watcher,
    ILogger<BlogHotReloadService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Hot reload is starting...");
        logger.LogInformation("Change essays will lead to hot reload!");
        logger.LogInformation("HINT: draft essays will be included.");

        await rendererService.RenderAsync(true);

        Task[] reloadTasks = [WatchFileAsync(stoppingToken)];
        await Task.WhenAll(reloadTasks);
    }

    private async Task WatchFileAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            logger.LogInformation("Watching file changes...");
            string? changeFile = await watcher.WaitForChange(token);

            if (changeFile is null)
            {
                logger.LogInformation("File watcher is stopping.");
                break;
            }

            FileInfo changeFileInfo = new(changeFile);

            if (changeFileInfo.Name.StartsWith('.'))
            {
                // Ignore dot-started file and directory.
                logger.LogDebug("Ignore hidden file: {}.", changeFile);
                continue;
            }

            logger.LogInformation("{} changed, re-rendering.", changeFile);
            essayContentService.Clear();
            await rendererService.RenderAsync(true);
        }
    }
}
