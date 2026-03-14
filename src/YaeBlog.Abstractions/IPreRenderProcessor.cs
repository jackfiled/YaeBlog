using YaeBlog.Abstractions.Models;

namespace YaeBlog.Abstractions;

public interface IPreRenderProcessor
{
    Task<BlogContent> ProcessAsync(BlogContent content);

    string Name { get; }
}
