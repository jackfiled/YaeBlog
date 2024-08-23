using YaeBlog.Core.Models;

namespace YaeBlog.Core.Abstractions;

public interface IEssayScanService
{
    public Task<BlogContents> ScanContents();

    public Task SaveBlogContent(BlogContent content, bool isDraft = true);
}
