using AngleSharp;
using AngleSharp.Dom;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Processors;

public class CodeBlockPostRenderProcessor : IPostRenderProcessor
{
    public async Task<BlogEssay> ProcessAsync(BlogEssay essay)
    {
        BrowsingContext context = new(Configuration.Default);
        IDocument document = await context.OpenAsync(
            req => req.Content(essay.HtmlContent));

        IEnumerable<IElement> preElements = from e in document.All
            where e.LocalName == "pre"
            select e;

        foreach (IElement element in preElements)
        {
            element.ClassList.Add("p-3 text-bg-secondary rounded-1");
        }

        return essay.WithNewHtmlContent(document.DocumentElement.OuterHtml);
    }

    public string Name => nameof(CodeBlockPostRenderProcessor);
}
