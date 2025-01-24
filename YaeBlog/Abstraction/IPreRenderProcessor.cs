using YaeBlog.Models;

namespace YaeBlog.Abstraction;

public interface IPreRenderProcessor
{
    Task<BlogContent> ProcessAsync(BlogContent content);

    string Name { get; }
}
