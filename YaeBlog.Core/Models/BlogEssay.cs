namespace YaeBlog.Core.Models;

public class BlogEssay
{
    public required string Title { get; init; }

    public required DateTime PublishTime { get; init; }

    public required string HtmlContent { get; init; }
}
