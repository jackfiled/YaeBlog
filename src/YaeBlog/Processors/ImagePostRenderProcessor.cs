using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Options;
using YaeBlog.Abstractions;
using YaeBlog.Core.Exceptions;
using YaeBlog.Abstractions.Models;

namespace YaeBlog.Processors;

/// <summary>
/// 图片地址路径后处理器
/// 将本地图片地址修改为图片API地址
/// </summary>
/// <param name="logger"></param>
/// <param name="options"></param>
public class ImagePostRenderProcessor(
    ILogger<ImagePostRenderProcessor> logger,
    IOptions<BlogOptions> options)
    : IPostRenderProcessor
{
    private readonly BlogOptions _options = options.Value;

    public async Task<BlogEssay> ProcessAsync(BlogEssay essay)
    {
        BrowsingContext context = new(Configuration.Default);
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
                attr.Value = GenerateImageLink(attr.Value, essay.FileName, essay.IsDraft);
            }
        }

        return essay with { HtmlContent = html.DocumentElement.OuterHtml };
    }

    public string Name => nameof(ImagePostRenderProcessor);

    private string GenerateImageLink(string filename, string essayFilename, bool isDraft)
    {
        // 如果图片路径中没有包含文件名
        // 则添加文件名
        if (!filename.Contains(essayFilename))
        {
            filename = Path.Combine(essayFilename, filename);
        }

        filename = isDraft
            ? Path.Combine(_options.Root, "drafts", filename)
            : Path.Combine(_options.Root, "posts", filename);

        if (!Path.Exists(filename))
        {
            logger.LogError("Failed to found image: {}.", filename);
            throw new BlogFileException($"Image {filename} doesn't exist.");
        }

        string imageLink = "api/files/" + filename;
        logger.LogDebug("Generate image link '{link}' for image file '{filename}'.",
            imageLink, filename);

        return imageLink;
    }
}
