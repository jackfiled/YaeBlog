namespace YaeBlog.Models;

public class BlogEssay : IComparable<BlogEssay>
{
    public required string Title { get; init; }

    public required string FileName { get; init; }

    public required bool IsDraft { get; init; }

    public required DateTimeOffset PublishTime { get; init; }

    public required DateTimeOffset UpdateTime { get; init; }

    public required string Description { get; init; }

    public required uint WordCount { get; init; }

    public required string ReadTime { get; init; }

    public List<string> Tags { get; } = [];

    public required string HtmlContent { get; init; }

    public BlogEssay WithNewHtmlContent(string newHtmlContent)
    {
        var essay = new BlogEssay
        {
            Title = Title,
            FileName = FileName,
            IsDraft = IsDraft,
            PublishTime = PublishTime,
            UpdateTime = UpdateTime,
            Description = Description,
            WordCount = WordCount,
            ReadTime = ReadTime,
            HtmlContent = newHtmlContent
        };
        essay.Tags.AddRange(Tags);

        return essay;
    }

    public int CompareTo(BlogEssay? other)
    {
        if (other is null)
        {
            return -1;
        }

        // 草稿文章应当排在前面
        if (IsDraft != other.IsDraft)
        {
            return IsDraft ? -1 : 1;
        }

        return other.PublishTime.CompareTo(PublishTime);
    }

    public override string ToString()
    {
        return $"{Title}-{PublishTime}";
    }
}
