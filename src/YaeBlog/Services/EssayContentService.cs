using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using YaeBlog.Abstraction;
using YaeBlog.Models;

namespace YaeBlog.Services;

public class EssayContentService : IEssayContentService
{
    private readonly ConcurrentDictionary<string, BlogEssay> _essays = new();

    private readonly List<BlogEssay> _sortedEssays = [];

    private readonly Dictionary<EssayTag, List<BlogEssay>> _tags = [];

    private readonly ConcurrentDictionary<string, BlogHeadline> _headlines = new();

    public bool TryAdd(BlogEssay essay)
    {
        _sortedEssays.Add(essay);
        return _essays.TryAdd(essay.FileName, essay);
    }

    public bool TryAddHeadline(string filename, BlogHeadline headline) => _headlines.TryAdd(filename, headline);

    public IEnumerable<BlogEssay> Essays => _sortedEssays;

    public int Count => _sortedEssays.Count;

    public bool TryGetEssay(string filename, [NotNullWhen(true)] out BlogEssay? essay)
    {
        return _essays.TryGetValue(filename, out essay);
    }

    public IReadOnlyDictionary<EssayTag, List<BlogEssay>> Tags => _tags;

    public IReadOnlyDictionary<string, BlogHeadline> Headlines => _headlines;

    public void RefreshTags()
    {
         _tags.Clear();

         foreach (BlogEssay essay in _essays.Values)
         {
             foreach (EssayTag essayTag in essay.Tags.Select(tag => new EssayTag(tag)))
             {
                 if (_tags.TryGetValue(essayTag, out List<BlogEssay>? essays))
                 {
                     essays.Add(essay);
                 }
                 else
                 {
                     _tags.Add(essayTag, [essay]);
                 }
             }
         }
    }

    public bool SearchByUrlEncodedTag(string tag, [NotNullWhen(true)] out List<BlogEssay>? result)
    {
        result = (from item in _tags
            where item.Key.UrlEncodedTagName == tag
            select item.Value).FirstOrDefault();

        return result is not null;
    }

    public void Clear()
    {
        _essays.Clear();
        _tags.Clear();
        _headlines.Clear();
    }
}
