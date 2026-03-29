namespace YaeBlog.Services;

public sealed class StartServerService(ConsoleInfoService consoleInfoService,
    RendererService rendererService,
    BlogHotReloadService blogHotReloadService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        switch (consoleInfoService.Command)
        {
            case ServerCommand.Serve:
                {
                    await rendererService.RenderAsync();
                    break;
                }
            case ServerCommand.Watch:
                {
                    await blogHotReloadService.StartAsync(cancellationToken);
                    break;
                }
            default:
                throw new InvalidOperationException();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
