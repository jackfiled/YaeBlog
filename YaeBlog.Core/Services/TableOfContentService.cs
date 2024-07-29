using System.Collections.Concurrent;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Services;

public class TableOfContentService : ITableOfContentService
{
    private readonly ConcurrentDictionary<string, BlogHeadline> _headlines = [];

    public IReadOnlyDictionary<string, BlogHeadline> Headlines => _headlines;

    public void AddHeadline(string filename, BlogHeadline headline)
    {
        if (!_headlines.TryAdd(filename, headline))
        {
            throw new InvalidOperationException();
        }
    }
}
