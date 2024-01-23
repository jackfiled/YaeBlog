using YaeBlog.Core.Models;

namespace YaeBlog.Core.Abstractions;

public interface IPostRenderProcessor
{
    BlogEssay Process(BlogEssay essay);

    string Name { get; }
}
