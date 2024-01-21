namespace YaeBlog.Core.Models;

public class BlogOptions
{
    public const string OptionName = "Blog";

    public required string Root { get; set; }

    public required string Author { get; set; }

    public required string ProjectName { get; set; }

    public required string BannerImage { get; set; }
}
