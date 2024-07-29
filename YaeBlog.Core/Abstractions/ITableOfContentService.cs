using YaeBlog.Core.Models;

namespace YaeBlog.Core.Abstractions;

public interface ITableOfContentService
{
    public IReadOnlyDictionary<string, BlogHeadline> Headlines { get; }
}
