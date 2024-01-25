namespace YaeBlog.Core.Models;

public class BlogEssay
{
    public required string Title { get; init; }

    public required string FileName { get; init; }

    public required DateTime PublishTime { get; init; }

    public required string Description { get; init; }

    public required uint WordCount { get; init; }

    public List<string> Tags { get; } = [];

    public required string HtmlContent { get; init; }

    public BlogEssay WithNewHtmlContent(string newHtmlContent)
    {
        var essay = new BlogEssay
        {
            Title = Title,
            FileName = FileName,
            PublishTime = PublishTime,
            Description = Description,
            WordCount = WordCount,
            HtmlContent = newHtmlContent
        };
        essay.Tags.AddRange(Tags);

        return essay;
    }

    public override string ToString()
    {
        return $"{Title}-{PublishTime}";
    }
}
