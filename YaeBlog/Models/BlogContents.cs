using System.Collections.Concurrent;

namespace YaeBlog.Models;

public sealed class BlogContents(ConcurrentBag<BlogContent> drafts, ConcurrentBag<BlogContent> posts)
{
    public ConcurrentBag<BlogContent> Drafts { get; } = drafts;

    public ConcurrentBag<BlogContent> Posts { get; } = posts;
}
