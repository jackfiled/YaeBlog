using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Services;

public class EssayContentService : IEssayContentService
{
    private readonly ConcurrentDictionary<string, BlogEssay> _essays = new();

    private readonly Dictionary<EssayTag, List<BlogEssay>> _tags = [];

    private readonly ConcurrentDictionary<string, BlogHeadline> _headlines = new();

    public bool TryAdd(BlogEssay essay) => _essays.TryAdd(essay.FileName, essay);

    public bool TryAddHeadline(string filename, BlogHeadline headline) => _headlines.TryAdd(filename, headline);

    public IReadOnlyDictionary<string, BlogEssay> Essays => _essays;

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
