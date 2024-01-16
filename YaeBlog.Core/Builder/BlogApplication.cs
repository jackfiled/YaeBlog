using Microsoft.Extensions.Hosting;

namespace YaeBlog.Core.Builder;

public class BlogApplication : IHost
{
    private readonly IHost _host;

    internal BlogApplication(IHost host)
    {
        _host = host;
    }

    public static BlogApplicationBuilder Create(string[] args)
    {
        BlogApplicationOptions options = new() { Args = args };
        return new BlogApplicationBuilder(options);
    }

    public Task StartAsync(CancellationToken cancellationToken = new())
    {
        return _host.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = new())
    {
        return _host.StopAsync(cancellationToken);
    }

    public IServiceProvider Services => _host.Services;

    public Task RunAsync() => _host.RunAsync();

    public void Run() => _host.Run();

    public void Dispose()
    {
        _host.Dispose();
        GC.SuppressFinalize(this);
    }
}
