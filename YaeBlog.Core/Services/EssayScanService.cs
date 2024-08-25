using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Exceptions;
using YaeBlog.Core.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace YaeBlog.Core.Services;

public partial class EssayScanService(
    ISerializer yamlSerializer,
    IDeserializer yamlDeserializer,
    IOptions<BlogOptions> blogOptions,
    ILogger<EssayScanService> logger) : IEssayScanService
{
    private readonly BlogOptions _blogOptions = blogOptions.Value;

    public async Task<BlogContents> ScanContents()
    {
        ValidateDirectory(_blogOptions.Root, out DirectoryInfo drafts, out DirectoryInfo posts);

        return new BlogContents(
            await ScanContentsInternal(drafts),
            await ScanContentsInternal(posts));
    }

    public async Task SaveBlogContent(BlogContent content, bool isDraft = true)
    {
        ValidateDirectory(_blogOptions.Root, out DirectoryInfo drafts, out DirectoryInfo posts);

        FileInfo targetFile = isDraft
            ? new FileInfo(Path.Combine(drafts.FullName, content.FileName + ".md"))
            : new FileInfo(Path.Combine(posts.FullName, content.FileName + ".md"));

        if (!isDraft)
        {
            content.Metadata.Date = DateTime.Now;
        }

        if (targetFile.Exists)
        {
            logger.LogWarning("Blog {} exists, overriding.", targetFile.Name);
        }

        await using StreamWriter writer = targetFile.CreateText();

        await writer.WriteAsync("---\n");
        await writer.WriteAsync(yamlSerializer.Serialize(content.Metadata));
        await writer.WriteAsync("---\n");

        if (isDraft)
        {
            await writer.WriteLineAsync("<!--more-->");
        }
        else
        {
            await writer.WriteAsync(content.FileContent);
        }
    }

    private async Task<ConcurrentBag<BlogContent>> ScanContentsInternal(DirectoryInfo directory)
    {
        IEnumerable<FileInfo> markdownFiles = from file in directory.EnumerateFiles()
            where file.Extension == ".md"
            select file;

        ConcurrentBag<(string, string)> fileContents = [];

        await Parallel.ForEachAsync(markdownFiles, async (file, token) =>
        {
            using StreamReader reader = file.OpenText();
            fileContents.Add((file.Name, await reader.ReadToEndAsync(token)));
        });

        ConcurrentBag<BlogContent> contents = [];

        await Task.Run(() =>
        {
            foreach ((string filename, string content) in fileContents)
            {
                int endPos = content.IndexOf("---", 4, StringComparison.Ordinal);
                if (!content.StartsWith("---") || endPos is -1 or 0)
                {
                    logger.LogWarning("Failed to parse metadata from {}, skipped.", filename);
                    return;
                }

                string metadataString = content[4..endPos];

                try
                {
                    MarkdownMetadata metadata = yamlDeserializer.Deserialize<MarkdownMetadata>(metadataString);
                    logger.LogDebug("Scan metadata title: '{}' for {}.", metadata.Title, filename);

                    contents.Add(new BlogContent
                    {
                        FileName = filename[..^3], Metadata = metadata, FileContent = content[(endPos + 3)..]
                    });
                }
                catch (YamlException e)
                {
                    logger.LogWarning("Failed to parser metadata from {} due to {}, skipping", filename, e);
                }
            }
        });

        return contents;
    }

    public async Task<ImageScanResult> ScanImages()
    {
        BlogContents contents = await ScanContents();
        ValidateDirectory(_blogOptions.Root, out DirectoryInfo drafts, out DirectoryInfo posts);

        List<FileInfo> unusedFiles = [];
        List<FileInfo> notFoundFiles = [];

        ImageScanResult draftResult = await ScanUnusedImagesInternal(contents.Drafts, drafts);
        ImageScanResult postResult = await ScanUnusedImagesInternal(contents.Posts, posts);

        unusedFiles.AddRange(draftResult.UnusedImages);
        notFoundFiles.AddRange(draftResult.NotFoundImages);
        unusedFiles.AddRange(postResult.UnusedImages);
        notFoundFiles.AddRange(postResult.NotFoundImages);

        return new ImageScanResult(unusedFiles, notFoundFiles);
    }

    private static Task<ImageScanResult> ScanUnusedImagesInternal(IEnumerable<BlogContent> contents,
        DirectoryInfo root)
    {
        Regex imageRegex = ImageRegex();
        ConcurrentBag<FileInfo> unusedImage = [];
        ConcurrentBag<FileInfo> notFoundImage = [];

        Parallel.ForEach(contents, content =>
        {
            MatchCollection result = imageRegex.Matches(content.FileContent);
            DirectoryInfo imageDirectory = new(Path.Combine(root.FullName, content.FileName));

            Dictionary<string, bool> usedDictionary;

            if (imageDirectory.Exists)
            {
                usedDictionary = (from file in imageDirectory.EnumerateFiles()
                    select new KeyValuePair<string, bool>(file.FullName, false)).ToDictionary();
            }
            else
            {
                usedDictionary = [];
            }

            foreach (Match match in result)
            {
                string imageName = match.Groups[1].Value;

                FileInfo usedFile = imageName.Contains(content.FileName)
                    ? new FileInfo(Path.Combine(root.FullName, imageName))
                    : new FileInfo(Path.Combine(root.FullName, content.FileName, imageName));

                if (usedDictionary.TryGetValue(usedFile.FullName, out _))
                {
                    usedDictionary[usedFile.FullName] = true;
                }
                else
                {
                    notFoundImage.Add(usedFile);
                }
            }

            foreach (KeyValuePair<string, bool> pair in usedDictionary.Where(p => !p.Value))
            {
                unusedImage.Add(new FileInfo(pair.Key));
            }
        });

        return Task.FromResult(new ImageScanResult(unusedImage.ToList(), notFoundImage.ToList()));
    }

    [GeneratedRegex(@"\!\[.*?\]\((.*?)\)")]
    private static partial Regex ImageRegex();

    private void ValidateDirectory(string root, out DirectoryInfo drafts, out DirectoryInfo posts)
    {
        root = Path.Combine(Environment.CurrentDirectory, root);
        DirectoryInfo rootDirectory = new(root);

        if (!rootDirectory.Exists)
        {
            throw new BlogFileException($"'{root}' is not a directory.");
        }

        if (rootDirectory.EnumerateDirectories().All(dir => dir.Name != "drafts"))
        {
            throw new BlogFileException($"'{root}/drafts' not exists.");
        }

        if (rootDirectory.EnumerateDirectories().All(dir => dir.Name != "posts"))
        {
            throw new BlogFileException($"'{root}/posts' not exists.");
        }

        drafts = new DirectoryInfo(Path.Combine(root, "drafts"));
        posts = new DirectoryInfo(Path.Combine(root, "posts"));
    }
}
