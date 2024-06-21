namespace YaeBlog.Core.Models;

public class BlogOptions
{
    public const string OptionName = "Blog";

    /// <summary>
    /// 博客markdown文件的根目录
    /// </summary>
    public required string Root { get; set; }

    /// <summary>
    /// 博客作者
    /// </summary>
    public required string Author { get; set; }

    public required string Announcement { get; set; }

    /// <summary>
    /// 博客的起始年份
    /// </summary>
    public required int StartYear { get; set; }

    /// <summary>
    /// 博客起始页面的背景图片
    /// </summary>
    public required string BannerImage { get; set; }

    /// <summary>
    /// 文章页面的背景图片
    /// </summary>
    public required string EssayImage { get; set; }

    /// <summary>
    /// 博客底部是否显示ICP备案信息
    /// </summary>
    public string? RegisterInformation { get; set; }

    public required AboutInfo About { get; set; }

    public required List<FriendLink> Links { get; set; }
}
