using System.Collections;
using System.Collections.Concurrent;

namespace YaeBlog.Abstractions.Models;

public record BlogContents(ConcurrentBag<BlogContent> Drafts, ConcurrentBag<BlogContent> Posts)
    : IEnumerable<BlogContent>
{
    public IEnumerator<BlogContent> GetEnumerator() => Posts.Concat(Drafts).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
