using YaeBlog.Core.Models;

namespace YaeBlog.Core.Abstractions;

public interface IPostRenderProcessor
{
    Task<BlogEssay> ProcessAsync(BlogEssay essay);

    string Name { get; }
}
