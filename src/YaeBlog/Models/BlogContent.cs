namespace YaeBlog.Models;

/// <summary>
/// 单个博客文件的所有数据和元数据
/// </summary>
/// <param name="BlogFile">博客文件</param>
/// <param name="Metadata">文件中的MD元数据</param>
/// <param name="Content">文件内容</param>
/// <param name="IsDraft">是否为草稿</param>
/// <param name="Images">博客中使用的文件</param>
public record BlogContent(
    FileInfo BlogFile,
    MarkdownMetadata Metadata,
    string Content,
    bool IsDraft,
    List<BlogImageInfo> Images,
    List<FileInfo> NotfoundImages)
{
    public string BlogName => BlogFile.Name.Split('.')[0];
}
