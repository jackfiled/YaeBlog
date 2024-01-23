using YaeBlog.Core.Models;

namespace YaeBlog.Core.Abstractions;

public interface IPreRenderProcessor
{
    BlogContent Process(BlogContent content);

    string Name { get; }
}
