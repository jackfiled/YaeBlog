using AngleSharp.Dom;

namespace YaeBlog.Extensions;

public static class AngleSharpExtensions
{
    public static IEnumerable<IElement> EnumerateParentElements(this IElement element)
    {
        IElement? e = element.ParentElement;

        while (e is not null)
        {
            IElement c = e;
            e = e.ParentElement;
            yield return c;
        }
    }
}
