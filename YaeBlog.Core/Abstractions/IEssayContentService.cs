using System.Diagnostics.CodeAnalysis;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Abstractions;

public interface IEssayContentService
{
    public IReadOnlyDictionary<string, BlogEssay> Essays { get; }

    public IReadOnlyDictionary<EssayTag, List<BlogEssay>> Tags { get; }

    public IReadOnlyDictionary<string, BlogHeadline> Headlines { get; }

    public bool TryAddHeadline(string filename, BlogHeadline headline);
    public bool SearchByUrlEncodedTag(string tag, [NotNullWhen(true)] out List<BlogEssay>? result);

    public bool TryAdd(BlogEssay essay);

    public void RefreshTags();

    public void Clear();
}
