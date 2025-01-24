using YaeBlog.Models;

namespace YaeBlog.Abstraction;

public interface IEssayScanService
{
    public Task<BlogContents> ScanContents();

    public Task SaveBlogContent(BlogContent content, bool isDraft = true);

    public Task<ImageScanResult> ScanImages();
}
