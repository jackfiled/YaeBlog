using System.Collections.Concurrent;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Services;

public class EssayContentService
{
    private readonly ConcurrentDictionary<string, BlogEssay> _essays = new();

    private readonly Dictionary<string, List<BlogEssay>> _tags = [];

    public bool TryGet(string key, out BlogEssay? essay)
        => _essays.TryGetValue(key, out essay);

    public bool TryAdd(string key, BlogEssay essay) => _essays.TryAdd(key, essay);

    public IEnumerable<KeyValuePair<string, BlogEssay>> Essays => _essays;

    public int Count => _essays.Count;

    public void RefreshTags()
    {
        foreach (BlogEssay essay in _essays.Values)
        {
            foreach (string tag in essay.Tags)
            {
                if (_tags.TryGetValue(tag, out var list))
                {
                    list.Add(essay);
                }
                else
                {
                    _tags[tag] = [essay];
                }
            }
        }

        foreach (KeyValuePair<string,List<BlogEssay>> pair in _tags)
        {
            pair.Value.Sort();
        }
    }

    public IEnumerable<KeyValuePair<string, int>> Tags => from item in _tags
        select KeyValuePair.Create(item.Key, item.Value.Count);

    public IEnumerable<BlogEssay> GetTag(string tag)
    {
        if (_tags.TryGetValue(tag, out var list))
        {
            return list;
        }

        throw new KeyNotFoundException("Selected tag not found.");
    }
}
