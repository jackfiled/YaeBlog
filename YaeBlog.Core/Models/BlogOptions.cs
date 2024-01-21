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

    /// <summary>
    /// 博客的起始年份
    /// </summary>
    public required int StartYear { get; set; }

    /// <summary>
    /// 博客项目的名称
    /// </summary>
    public required string ProjectName { get; set; }

    /// <summary>
    /// 博客起始页面的背景图片
    /// </summary>
    public required string BannerImage { get; set; }

    /// <summary>
    /// 博客底部是否显示ICP备案信息
    /// </summary>
    public string? RegisterInformation { get; set; }
}
