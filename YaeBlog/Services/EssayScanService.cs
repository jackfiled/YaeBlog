using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Imageflow.Bindings;
using Imageflow.Fluent;
using Microsoft.Extensions.Options;
using YaeBlog.Abstraction;
using YaeBlog.Core.Exceptions;
using YaeBlog.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace YaeBlog.Services;

public partial class EssayScanService : IEssayScanService
{
    private readonly BlogOptions _blogOptions;
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;
    private readonly ILogger<EssayScanService> _logger;

    public EssayScanService(ISerializer yamlSerializer,
        IDeserializer yamlDeserializer,
        IOptions<BlogOptions> blogOptions,
        ILogger<EssayScanService> logger)
    {
        _yamlSerializer = yamlSerializer;
        _yamlDeserializer = yamlDeserializer;
        _logger = logger;
        _blogOptions = blogOptions.Value;
        RootDirectory = ValidateRootDirectory();
    }

    private DirectoryInfo RootDirectory { get; }

    public async Task<BlogContents> ScanContents()
    {
        ValidateDirectory(out DirectoryInfo drafts, out DirectoryInfo posts);

        return new BlogContents(
            await ScanContentsInternal(drafts, true),
            await ScanContentsInternal(posts, false));
    }

    public async Task SaveBlogContent(BlogContent content, bool isDraft = true)
    {
        ValidateDirectory(out DirectoryInfo drafts, out DirectoryInfo posts);

        FileInfo targetFile = isDraft
            ? new FileInfo(Path.Combine(drafts.FullName, content.BlogName + ".md"))
            : new FileInfo(Path.Combine(posts.FullName, content.BlogName + ".md"));

        if (targetFile.Exists)
        {
            _logger.LogWarning("Blog {} exists, overriding.", targetFile.Name);
        }

        await using StreamWriter writer = targetFile.CreateText();

        await writer.WriteAsync("---\n");
        await writer.WriteAsync(_yamlSerializer.Serialize(content.Metadata));
        await writer.WriteAsync("---\n");

        if (string.IsNullOrEmpty(content.Content) && isDraft)
        {
            // 如果博客为操作且内容为空
            // 创建简介隔断符号
            await writer.WriteLineAsync("<!--more-->");
        }
        else
        {
            await writer.WriteAsync(content.Content);
        }

        // 保存图片文件
        await Task.WhenAll(from image in content.Images
            select File.WriteAllBytesAsync(image.File.FullName, image.Content));
    }

    private record struct BlogResult(
        FileInfo BlogFile,
        string BlogContent,
        List<BlogImageInfo> Images,
        List<FileInfo> NotFoundImages);

    private async Task<ConcurrentBag<BlogContent>> ScanContentsInternal(DirectoryInfo directory, bool isDraft)
    {
        // 扫描以md结尾且不是隐藏文件的文件
        IEnumerable<FileInfo> markdownFiles = from file in directory.EnumerateFiles()
            where file.Extension == ".md" && !file.Name.StartsWith('.')
            select file;

        ConcurrentBag<BlogResult> fileContents = [];

        await Parallel.ForEachAsync(markdownFiles, async (file, token) =>
        {
            using StreamReader reader = file.OpenText();
            string blogName = file.Name.Split('.')[0];
            string blogContent = await reader.ReadToEndAsync(token);
            ImageResult imageResult =
                await ScanImagePreBlog(directory, blogName,
                    blogContent);

            fileContents.Add(new BlogResult(file, blogContent, imageResult.Images, imageResult.NotfoundImages));
        });

        ConcurrentBag<BlogContent> contents = [];

        await Task.Run(() =>
        {
            foreach (BlogResult blog in fileContents)
            {
                if (blog.BlogContent.Length < 4)
                {
                    // Even not contains a legal header.
                    continue;
                }

                int endPos = blog.BlogContent.IndexOf("---", 4, StringComparison.Ordinal);
                if (!blog.BlogContent.StartsWith("---") || endPos is -1 or 0)
                {
                    _logger.LogWarning("Failed to parse metadata from {}, skipped.", blog.BlogFile.Name);
                    return;
                }

                string metadataString = blog.BlogContent[4..endPos];

                try
                {
                    MarkdownMetadata metadata = _yamlDeserializer.Deserialize<MarkdownMetadata>(metadataString);
                    _logger.LogDebug("Scan metadata title: '{title}' for {name}.", metadata.Title, blog.BlogFile.Name);

                    contents.Add(new BlogContent(blog.BlogFile, metadata, blog.BlogContent[(endPos + 3)..], isDraft,
                        blog.Images, blog.NotFoundImages));
                }
                catch (YamlException e)
                {
                    _logger.LogWarning("Failed to parser metadata from {name} due to {exception}, skipping", blog.BlogFile.Name, e);
                }
            }
        });

        return contents;
    }

    private record struct ImageResult(List<BlogImageInfo> Images, List<FileInfo> NotfoundImages);

    private async Task<ImageResult> ScanImagePreBlog(DirectoryInfo directory, string blogName, string content)
    {
        MatchCollection matchResult = ImagePattern.Matches(content);
        DirectoryInfo imageDirectory = new(Path.Combine(directory.FullName, blogName));

        Dictionary<string, bool> usedImages = imageDirectory.Exists
            ? imageDirectory.EnumerateFiles().ToDictionary(file => file.FullName, _ => false)
            : [];
        List<FileInfo> notFoundImages = [];

        foreach (Match match in matchResult)
        {
            string imageName = match.Groups[1].Value;

            // 判断md文件中的图片名称中是否包含文件夹名称
            // 例如 blog-1/image.png 或者 image.png
            // 如果不带文件夹名称
            // 默认添加同博客名文件夹
            FileInfo usedFile = imageName.Contains(blogName)
                ? new FileInfo(Path.Combine(directory.FullName, imageName))
                : new FileInfo(Path.Combine(directory.FullName, blogName, imageName));

            if (usedImages.TryGetValue(usedFile.FullName, out _))
            {
                usedImages[usedFile.FullName] = true;
            }
            else
            {
                notFoundImages.Add(usedFile);
            }
        }

        List<BlogImageInfo> images = (await Task.WhenAll((from pair in usedImages
            select GetImageInfo(new FileInfo(pair.Key), pair.Value)).ToArray())).ToList();

        return new ImageResult(images, notFoundImages);
    }

    private static async Task<BlogImageInfo> GetImageInfo(FileInfo file, bool isUsed)
    {
        byte[] image = await File.ReadAllBytesAsync(file.FullName);

        if (file.Extension is ".jpg" or ".jpeg" or ".png")
        {
            ImageInfo imageInfo =
                await ImageJob.GetImageInfoAsync(MemorySource.Borrow(image), SourceLifetime.NowOwnedAndDisposedByTask);

            return new BlogImageInfo(file, imageInfo.ImageWidth, imageInfo.ImageWidth, imageInfo.PreferredMimeType,
                image, isUsed);
        }

        return new BlogImageInfo(file, 0, 0, file.Extension switch
        {
            "svg" => "image/svg",
            "avif" => "image/avif",
            _ => string.Empty
        }, image, isUsed);
    }

    [GeneratedRegex(@"\!\[.*?\]\((.*?)\)")]
    private static partial Regex ImagePattern { get; }


    private DirectoryInfo ValidateRootDirectory()
    {
        DirectoryInfo rootDirectory = new(Path.Combine(Environment.CurrentDirectory, _blogOptions.Root));

        if (!rootDirectory.Exists)
        {
            throw new BlogFileException($"'{_blogOptions.Root}' is not a directory.");
        }

        return rootDirectory;
    }

    private void ValidateDirectory(out DirectoryInfo drafts, out DirectoryInfo posts)
    {
        if (RootDirectory.EnumerateDirectories().All(dir => dir.Name != "drafts"))
        {
            throw new BlogFileException($"'{_blogOptions.Root}/drafts' not exists.");
        }

        if (RootDirectory.EnumerateDirectories().All(dir => dir.Name != "posts"))
        {
            throw new BlogFileException($"'{_blogOptions.Root}/posts' not exists.");
        }

        drafts = new DirectoryInfo(Path.Combine(_blogOptions.Root, "drafts"));
        posts = new DirectoryInfo(Path.Combine(_blogOptions.Root, "posts"));
    }
}
