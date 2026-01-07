namespace YaeBlog.Models;

public class MarkdownMetadata
{
    public string? Title { get; set; }

    public DateTimeOffset Date { get; set; }

    public DateTimeOffset UpdateTime { get; set; }

    public List<string>? Tags { get; set; }
}
