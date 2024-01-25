using AngleSharp;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Builder;
using YaeBlog.Core.Extensions;
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

    public static void AddImageApiEndpoint(BlogApplicationBuilder builder)
    {
        builder.ConfigureWebApplication((application) =>
        {
            application.MapGet("/api/files/{*filename}", ImageHandler);
        });
    }

    private static Results<FileStreamHttpResult, NotFound> ImageHandler(string filename)
    {
        string contentType = "image/png";
        if (filename.EndsWith("jpg") || filename.EndsWith("jpeg"))
        {
            contentType = "image/jpeg";
        }

        if (!Path.Exists(filename))
        {
            return TypedResults.NotFound();
        }

        Stream imageStream = File.OpenRead(filename);
        return TypedResults.Stream(imageStream, contentType);
    }

    private string GenerateImageLink(string filename, string essayFilename)
    {
        if (!filename.Contains(essayFilename))
        {
            filename = Path.Combine(essayFilename, filename);
        }

        filename = Path.Combine(_options.Root, filename);

        if (!Path.Exists(filename))
        {
            logger.LogWarning("Failed to found image: {}.", filename);
            return _options.BannerImage;
        }

        string imageLink = "api/files/" + filename;
        logger.LogDebug("Generate image link '{}' for image file '{}'.",
            imageLink, filename);

        return imageLink;
    }
}
