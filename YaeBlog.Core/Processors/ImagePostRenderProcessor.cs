using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Processors;

public class ImagePostRenderProcessor(ILogger<ImagePostRenderProcessor> logger,
    IOptions<BlogOptions> options)
    : IPostRenderProcessor
{
    private static readonly IConfiguration s_configuration = Configuration.Default;

    private readonly BlogOptions _options = options.Value;

    public async Task<BlogEssay> ProcessAsync(BlogEssay essay)
    {
        BrowsingContext context = new(s_configuration);
        IDocument html = await context.OpenAsync(
            req => req.Content(essay.HtmlContent));

        IEnumerable<IElement> imageElements = from node in html.All
            where node.LocalName == "img"
            select node;

        foreach (IElement element in imageElements)
        {
            IAttr? attr = element.Attributes.GetNamedItem("src");
            if (attr is not null)
            {
                logger.LogDebug("Found image link: '{}'", attr.Value);
                attr.Value = GenerateImageLink(attr.Value, essay.FileName);
            }
            element.ClassList.Add("essay-image");
        }
        return essay.WithNewHtmlContent(html.DocumentElement.OuterHtml);
    }

    public string Name => "ImagePostRenderProcessor";

    private string GenerateImageLink(string filename, string essayFilename)
    {
        if (!filename.Contains(essayFilename))
        {
            filename = Path.Combine(essayFilename, filename);
        }

        filename = Path.Combine(_options.Root, filename);

        if (!Path.Exists(filename))
        {
            logger.LogError("Failed to found image: {}.", filename);
            throw new InvalidOperationException();
        }

        string imageLink = "api/files/" + filename;
        logger.LogDebug("Generate image link '{}' for image file '{}'.",
            imageLink, filename);

        return imageLink;
    }
}
