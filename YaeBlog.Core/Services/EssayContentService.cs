using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Services;

public class EssayContentService : IEssayContentService
{
    private readonly ConcurrentDictionary<string, BlogEssay> _essays = new();

    private readonly Dictionary<EssayTag, List<BlogEssay>> _tags = [];

    public bool TryAdd(BlogEssay essay) => _essays.TryAdd(essay.FileName, essay);

    public IReadOnlyDictionary<string, BlogEssay> Essays => _essays;

    public IReadOnlyDictionary<EssayTag, List<BlogEssay>> Tags => _tags;

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
}
