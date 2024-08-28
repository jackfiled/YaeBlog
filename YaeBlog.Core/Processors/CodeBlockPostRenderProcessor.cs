using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Processors;

public class CodeBlockPostRenderProcessor(ILogger<CodeBlockPostRenderProcessor> logger) : IPostRenderProcessor
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

            IEnumerable<IElement> codeElements = from e in element.Children
                where e.LocalName == "code"
                    select e;

            foreach (IElement code in codeElements)
            {
                string? language = (from c in code.ClassList
                    where c.StartsWith("language-")
                    select c[9..].ToLower()).FirstOrDefault();

                if (language is null)
                {
                    continue;
                }

                logger.LogDebug("Detect code block of language {}.", language);
                code.InnerHtml = HighLightCode(code.InnerHtml, language);
            }
        }

        return essay.WithNewHtmlContent(document.DocumentElement.OuterHtml);
    }

    public string Name => nameof(CodeBlockPostRenderProcessor);

    private static string HighLightCode(string code, string language)
    {
        return code;
    }
}
