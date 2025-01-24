using Microsoft.Extensions.Options;
using YaeBlog.Models;

namespace YaeBlog.Services;

public sealed class BlogChangeWatcher : IDisposable
{
    private readonly FileSystemWatcher _fileSystemWatcher;
    private readonly ILogger<BlogChangeWatcher> _logger;

    public BlogChangeWatcher(IOptions<BlogOptions> options, ILogger<BlogChangeWatcher> logger)
    {
        _logger = logger;
        _fileSystemWatcher = new FileSystemWatcher(Path.Combine(Environment.CurrentDirectory, options.Value.Root));

        _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName |
                                          NotifyFilters.DirectoryName | NotifyFilters.Size;
        _fileSystemWatcher.IncludeSubdirectories = true;
        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    public async Task<string?> WaitForChange(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<string?> tcs = new();
        cancellationToken.Register(() => tcs.TrySetResult(null));

        _logger.LogDebug("Register file change handle.");
        _fileSystemWatcher.Changed += FileChangedCallback;
        _fileSystemWatcher.Created += FileChangedCallback;
        _fileSystemWatcher.Deleted += FileChangedCallback;
        _fileSystemWatcher.Renamed += FileChangedCallback;

        string? result;
        try
        {
            result = await tcs.Task;
        }
        finally
        {
            _logger.LogDebug("Unregister file change handle.");
            _fileSystemWatcher.Changed -= FileChangedCallback;
            _fileSystemWatcher.Created -= FileChangedCallback;
            _fileSystemWatcher.Deleted -= FileChangedCallback;
            _fileSystemWatcher.Renamed -= FileChangedCallback;
        }

        return result;

        void FileChangedCallback(object _, FileSystemEventArgs e)
        {
            _logger.LogDebug("File {} change detected.", e.Name);
            tcs.TrySetResult(e.Name);
        }
    }

    public void Dispose()
    {
        _fileSystemWatcher.Dispose();
    }
}
