using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Exceptions;
using YaeBlog.Core.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace YaeBlog.Core.Services;

public class EssayScanService(
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

        if (targetFile.Exists)
        {
            logger.LogWarning("Blog {} exists, overriding.", targetFile.Name);
        }

        await using StreamWriter writer = targetFile.CreateText();

        await writer.WriteAsync("---\n");
        await writer.WriteAsync(yamlSerializer.Serialize(content.Metadata));
        await writer.WriteAsync("---\n");
        await writer.WriteAsync("<!--more-->\n");
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
