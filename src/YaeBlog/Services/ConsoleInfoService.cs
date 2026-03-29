namespace YaeBlog.Services;

public enum ServerCommand
{
    Serve,
    Watch
}

public sealed class ConsoleInfoService
{
    public bool IsOneShotCommand { get; set; }

    public ServerCommand Command { get; set; }
}
