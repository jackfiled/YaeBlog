using YaeBlog.Abstractions.Models;

namespace YaeBlog.Abstractions;

public interface IPostRenderProcessor
{
    Task<BlogEssay> ProcessAsync(BlogEssay essay);

    string Name { get; }
}
