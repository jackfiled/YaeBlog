namespace YaeBlog.Models;

public record BlogEssay(
    string Title,
    string FileName,
    bool IsDraft,
    DateTimeOffset PublishTime,
    DateTimeOffset UpdateTime,
    string Description,
    uint WordCount,
    string ReadTime,
    List<string> Tags,
    string HtmlContent) : IComparable<BlogEssay>
{
    public string EssayLink => $"/blog/essays/{FileName}";

    public override string ToString() => $"{Title}-{PublishTime}";

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
}
