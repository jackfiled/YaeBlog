using System.Collections;
using System.Collections.Concurrent;

namespace YaeBlog.Models;

public record BlogContents(ConcurrentBag<BlogContent> Drafts, ConcurrentBag<BlogContent> Posts)
    : IEnumerable<BlogContent>
{
    IEnumerator<BlogContent> IEnumerable<BlogContent>.GetEnumerator()
    {
        return Posts.Concat(Drafts).GetEnumerator();
    }

    public IEnumerator GetEnumerator() => ((IEnumerable<BlogContent>)this).GetEnumerator();
}
