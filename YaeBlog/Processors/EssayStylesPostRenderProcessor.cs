using AngleSharp;
using AngleSharp.Dom;
using YaeBlog.Abstraction;
using YaeBlog.Extensions;
using YaeBlog.Models;

namespace YaeBlog.Processors;

/// <summary>
/// 向渲染的HTML中插入Tailwind CSS的渲染后处理器
/// </summary>
public sealed class EssayStylesPostRenderProcessor : IPostRenderProcessor
{
    public string Name => nameof(EssayStylesPostRenderProcessor);

    public async Task<BlogEssay> ProcessAsync(BlogEssay essay)
    {
        BrowsingContext context = new(Configuration.Default);
        IDocument document = await context.OpenAsync(req => req.Content(essay.HtmlContent));

        ApplyGlobalCssStyles(document);
        BeatifyTable(document);
        BeatifyList(document);
        BeatifyInlineCode(document);

        return essay.WithNewHtmlContent(document.DocumentElement.OuterHtml);
    }

    private readonly Dictionary<string, string> _globalCssStyles = new()
    {
        { "pre", "p-4 bg-gray-100 rounded-sm overflow-x-auto" },
        { "h2", "text-3xl font-bold py-4" },
        { "h3", "text-2xl font-bold py-3" },
        { "h4", "text-xl font-bold py-2" },
        { "h5", "text-lg font-bold py-1" },
        { "p", "p-2" },
        { "img", "w-11/12 block mx-auto my-2 rounded-md shadow-md" },
        { "a", "text-blue-600" }
    };

    private void ApplyGlobalCssStyles(IDocument document)
    {
        foreach ((string tag, string style) in _globalCssStyles)
        {
            foreach (IElement element in document.GetElementsByTagName(tag))
            {
                element.ClassList.Add(style);
            }
        }
    }

    private static void BeatifyTable(IDocument document)
    {
        foreach (IElement element in from e in document.All
                 where e.LocalName == "table"
                 select e)
        {
            element.ClassList.Add("mx-auto border-collapse table-auto overflow-x-auto");

            // thead元素
            foreach (IElement headElement in from e in element.Children
                     where e.LocalName == "thead"
                     select e)
            {
                headElement.ClassList.Add("bg-slate-200");

                // tr in thead
                foreach (IElement trElement in from e in headElement.Children
                         where e.LocalName == "tr"
                         select e)
                {
                    trElement.ClassList.Add("border border-slate-300");

                    // th in tr
                    foreach (IElement thElement in from e in trElement.Children
                             where e.LocalName == "th"
                             select e)
                    {
                        thElement.ClassList.Add("px-4 py-1");
                    }
                }
            }

            // tbody元素
            foreach (IElement bodyElement in from e in element.Children
                     where e.LocalName == "tbody"
                     select e)
            {
                // tr in tbody
                foreach (IElement trElement in from e in bodyElement.Children
                         where e.LocalName == "tr"
                         select e)
                {
                    foreach (IElement tdElement in from e in trElement.Children
                             where e.LocalName == "td"
                             select e)
                    {
                        tdElement.ClassList.Add("px-4 py-1 border border-slate-300");
                    }
                }
            }
        }
    }

    private static void BeatifyList(IDocument document)
    {
        foreach (IElement ulElement in from e in document.All
                 where e.LocalName == "ul"
                 select e)
        {
            // 首先给<ul>元素添加样式
            ulElement.ClassList.Add("list-disc ml-10");


            foreach (IElement liElement in from e in ulElement.Children
                     where e.LocalName == "li"
                     select e)
            {
                // 修改<li>元素中的<p>元素样式
                // 默认的p-2间距有点太宽了
                foreach (IElement pElement in from e in liElement.Children
                         where e.LocalName == "p"
                         select e)
                {
                    pElement.ClassList.Remove("p-2");
                    pElement.ClassList.Add("p-1");
                }
            }
        }
    }

    private static void BeatifyInlineCode(IDocument document)
    {
        // 选择不在<pre>元素内的<code>元素
        // 即行内代码
        IEnumerable<IElement> inlineCodes = from e in document.All
            where e.LocalName == "code" && e.EnumerateParentElements().All(p => p.LocalName != "pre")
            select e;

        foreach (IElement e in inlineCodes)
        {
            e.ClassList.Add("bg-gray-100 inline p-1 rounded-xs");
        }
    }
}
