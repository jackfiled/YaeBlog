using AngleSharp;
using AngleSharp.Dom;
using YaeBlog.Abstraction;
using YaeBlog.Models;

namespace YaeBlog.Processors;

public class HeadlinePostRenderProcessor(
    AngleSharp.IConfiguration angleConfiguration,
    IEssayContentService essayContentService,
    ILogger<HeadlinePostRenderProcessor> logger) : IPostRenderProcessor
{
    public async Task<BlogEssay> ProcessAsync(BlogEssay essay)
    {
        BrowsingContext browsingContext = new(angleConfiguration);
        IDocument document = await browsingContext.OpenAsync(req => req.Content(essay.HtmlContent));

        IEnumerable<IElement> elements = from item in document.All
            where item.LocalName is "h2" or "h3" or "h4"
            select item;

        BlogHeadline topHeadline = new(essay.Title, "#title");
        List<BlogHeadline> level2List = [];
        List<BlogHeadline> level3List = [];
        List<BlogHeadline> level4List = [];

        foreach (IElement element in elements)
        {
            switch (element.LocalName)
            {
                case "h2":
                    {
                        FindParentHeadline(topHeadline, level2List, level3List).Children.AddRange(level4List);
                        level4List.Clear();
                        FindParentHeadline(topHeadline, level2List).Children.AddRange(level3List);
                        level3List.Clear();

                        BlogHeadline headline = ParserHeadlineElement(element);
                        level2List.Add(headline);
                        break;
                    }
                case "h3":
                    {
                        FindParentHeadline(topHeadline, level2List, level3List).Children.AddRange(level4List);
                        level4List.Clear();

                        BlogHeadline headline = ParserHeadlineElement(element);
                        level3List.Add(headline);
                        break;
                    }
                case "h4":
                    {
                        BlogHeadline headline = ParserHeadlineElement(element);
                        level4List.Add(headline);
                        break;
                    }
            }
        }

        // 太抽象了（（（
        FindParentHeadline(topHeadline, level2List, level3List).Children.AddRange(level4List);
        FindParentHeadline(topHeadline, level2List).Children.AddRange(level3List);
        topHeadline.Children.AddRange(level2List);

        if (!essayContentService.TryAddHeadline(essay.FileName, topHeadline))
        {
            logger.LogWarning("Failed to add headline of {}.", essay.FileName);
        }

        return essay.WithNewHtmlContent(document.DocumentElement.OuterHtml);
    }

    private static BlogHeadline ParserHeadlineElement(IElement element)
    {
        element.Id ??= element.TextContent;
        return new BlogHeadline(element.TextContent, element.Id);
    }

    /// <summary>
    /// 找到h4标题的父级标题
    /// </summary>
    /// <param name="topHeadline"></param>
    /// <param name="level2"></param>
    /// <param name="level3"></param>
    /// <returns></returns>
    private static BlogHeadline FindParentHeadline(BlogHeadline topHeadline, List<BlogHeadline> level2,
        List<BlogHeadline> level3)
    {
        BlogHeadline? result = level3.LastOrDefault();
        if (result is not null)
        {
            return result;
        }

        return level2.LastOrDefault() ?? topHeadline;
    }

    /// <summary>
    /// 找到h3标题的父级标题
    /// </summary>
    /// <param name="topHeadline"></param>
    /// <param name="level2"></param>
    /// <returns></returns>
    private static BlogHeadline FindParentHeadline(BlogHeadline topHeadline, List<BlogHeadline> level2) =>
        FindParentHeadline(topHeadline, level2, []);

    public string Name => nameof(HeadlinePostRenderProcessor);
}
