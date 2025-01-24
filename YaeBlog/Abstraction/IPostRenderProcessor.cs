using YaeBlog.Models;

namespace YaeBlog.Abstraction;

public interface IPostRenderProcessor
{
    Task<BlogEssay> ProcessAsync(BlogEssay essay);

    string Name { get; }
}
