using System.Diagnostics.CodeAnalysis;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Abstractions;

public interface IEssayContentService
{
    public IReadOnlyDictionary<string, BlogEssay> Essays { get; }

    public IReadOnlyDictionary<EssayTag, List<BlogEssay>> Tags { get; }

    public bool SearchByUrlEncodedTag(string tag,[NotNullWhen(true)] out List<BlogEssay>? result);
}
