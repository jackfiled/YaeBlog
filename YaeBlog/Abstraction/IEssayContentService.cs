using System.Diagnostics.CodeAnalysis;
using YaeBlog.Models;

namespace YaeBlog.Abstraction;

public interface IEssayContentService
{
    public IEnumerable<BlogEssay> Essays { get; }

    public int Count { get; }

    public IReadOnlyDictionary<EssayTag, List<BlogEssay>> Tags { get; }

    public IReadOnlyDictionary<string, BlogHeadline> Headlines { get; }

    public bool TryAddHeadline(string filename, BlogHeadline headline);
    public bool SearchByUrlEncodedTag(string tag, [NotNullWhen(true)] out List<BlogEssay>? result);

    public bool TryAdd(BlogEssay essay);

    public bool TryGetEssay(string filename, [NotNullWhen(true)] out BlogEssay? essay);

    public void RefreshTags();

    public void Clear();
}
