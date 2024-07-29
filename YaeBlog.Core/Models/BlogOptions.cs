namespace YaeBlog.Core.Models;

public class BlogOptions
{
    public const string OptionName = "Blog";

    /// <summary>
    /// 博客markdown文件的根目录
    /// </summary>
    public required string Root { get; set; }

    /// <summary>
    /// 博客正文的广而告之
    /// </summary>
    public required string Announcement { get; set; }

    /// <summary>
    /// 博客的起始年份
    /// </summary>
    public required int StartYear { get; set; }

    /// <summary>
    /// 博客的友链
    /// </summary>
    public required List<FriendLink> Links { get; set; }
}
