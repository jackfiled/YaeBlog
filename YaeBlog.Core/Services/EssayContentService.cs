using System.Collections.Concurrent;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Services;

public class EssayContentService
{
    private readonly ConcurrentDictionary<string, BlogEssay> _essays = new();

    public bool TryGet(string key, out BlogEssay? essay)
        => _essays.TryGetValue(key, out essay);

    public bool TryAdd(string key, BlogEssay essay) => _essays.TryAdd(key, essay);

    public IEnumerable<KeyValuePair<string, BlogEssay>> Essays => _essays;

    public int Count => _essays.Count;
}
