namespace YaeBlog.Core.Models;

public class BlogHeadline(string title, string selectorId)
{
    public string Title { get; } = title;

    public string SelectorId { get; set; } = selectorId;

    public List<BlogHeadline> Children { get; } = [];
}
