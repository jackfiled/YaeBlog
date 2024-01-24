using YaeBlog.Core.Models;

namespace YaeBlog.Core.Abstractions;

public interface IPreRenderProcessor
{
    Task<BlogContent> ProcessAsync(BlogContent content);

    string Name { get; }
}
