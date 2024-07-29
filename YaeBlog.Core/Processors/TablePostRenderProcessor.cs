using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Processors;

public class TablePostRenderProcessor: IPostRenderProcessor
{
    public async Task<BlogEssay> ProcessAsync(BlogEssay essay)
    {
        BrowsingContext browsingContext = new(Configuration.Default);
        IDocument document = await browsingContext.OpenAsync(
            req => req.Content(essay.HtmlContent));

        IEnumerable<IHtmlTableElement> tableElements = from item in document.All
            where item.LocalName == "table"
            select item as IHtmlTableElement;

        foreach (IHtmlTableElement element in tableElements)
        {
            IHtmlDivElement divElement = document.CreateElement<IHtmlDivElement>();
            divElement.InnerHtml = element.OuterHtml;
            divElement.ClassList.Add("py-2", "table-wrapper");

            element.Replace(divElement);
        }

        return essay.WithNewHtmlContent(document.DocumentElement.OuterHtml);
    }

    public string Name => nameof(TablePostRenderProcessor);
}
