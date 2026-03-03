using YaeBlog.Models;

namespace YaeBlog.Abstraction;

public interface IEssayScanService
{
    public Task<BlogContents> ScanContents();

    /// <summary>
    /// 将对应的博客文章保存在磁盘上
    /// </summary>
    /// <param name="content"></param>
    /// <param name="isDraft">指定对应博客文章是否为草稿。因为BlogContent是不可变对象，因此提供该参数以方便publish的实现。</param>
    /// <returns></returns>
    public Task SaveBlogContent(BlogContent content, bool isDraft = true);
}
