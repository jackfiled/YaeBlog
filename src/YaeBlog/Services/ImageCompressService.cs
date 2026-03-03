using Imageflow.Fluent;
using YaeBlog.Abstraction;
using YaeBlog.Core.Exceptions;
using YaeBlog.Models;

namespace YaeBlog.Services;

public sealed class ImageCompressService(IEssayScanService essayScanService, ILogger<ImageCompressService> logger)
{
    private record struct CompressResult(BlogImageInfo ImageInfo, byte[] CompressContent);

    public async Task<List<BlogImageInfo>> ScanUsedImages()
    {
        BlogContents contents = await essayScanService.ScanContents();
        List<BlogImageInfo> originalImages = (from content in contents.Posts.Concat(contents.Drafts)
            from image in content.Images
            where image.IsUsed
            select image).ToList();

        originalImages.Sort();

        return originalImages;
    }

    public async Task Compress(bool dryRun)
    {
        BlogContents contents = await essayScanService.ScanContents();

        // 筛选需要压缩的图片
        // 即图片被博客使用且是jpeg/png格式
        List<BlogContent> needCompressContents = (from content in contents
            where content.Images.Any(i => i is { IsUsed: true } and { File.Extension: ".jpg" or ".jpeg" or ".png" })
            select content).ToList();

        if (needCompressContents.Count == 0)
        {
            return;
        }

        int uncompressedSize = 0;
        int compressedSize = 0;
        List<BlogContent> compressedContent = new(needCompressContents.Count);

        foreach (BlogContent content in needCompressContents)
        {
            List<BlogImageInfo> uncompressedImages = (from image in content.Images
                where image is { IsUsed: true } and { File.Extension: ".jpg" or ".jpeg" or ".png" }
                select image).ToList();

            uncompressedSize += uncompressedImages.Select(i => i.Size).Sum();

            foreach (BlogImageInfo image in uncompressedImages)
            {
                logger.LogInformation("Uncompressed image: {} belonging to blog {}.", image.File.Name,
                    content.BlogName);
            }

            CompressResult[] compressedImages = (await Task.WhenAll(from image in uncompressedImages
                select Task.Run(async () => new CompressResult(image, await ConvertToWebp(image))))).ToArray();

            compressedSize += compressedImages.Select(i => i.CompressContent.Length).Sum();

            // 直接在原有的图片列表上添加图片
            List<BlogImageInfo> images = content.Images.Concat(from r in compressedImages
                select r.ImageInfo with
                {
                    File = new FileInfo(r.ImageInfo.File.FullName.Split('.')[0] + ".webp"),
                    Content = r.CompressContent,
                    MineType = "image/webp"
                }).ToList();
            // 修改文本
            string blogContent = compressedImages.Aggregate(content.Content, (c, r) =>
            {
                string originalName = r.ImageInfo.File.Name;
                string outputName = originalName.Split('.')[0] + ".webp";

                return c.Replace(originalName, outputName);
            });

            compressedContent.Add(content with { Images = images, Content = blogContent });
        }

        logger.LogInformation("Compression ratio: {}%.", (double)compressedSize / uncompressedSize * 100.0);

        if (dryRun is false)
        {
            await Task.WhenAll(from content in compressedContent
                select essayScanService.SaveBlogContent(content, content.IsDraft));
        }
    }

    private static async Task<byte[]> ConvertToWebp(BlogImageInfo image)
    {
        using ImageJob job = new();
        BuildJobResult result = await job.Decode(MemorySource.Borrow(image.Content))
            .Branch(f => f.EncodeToBytes(new WebPLosslessEncoder()))
            .EncodeToBytes(new WebPLossyEncoder(75))
            .Finish()
            .InProcessAsync();

        // 超过128KB的图片使用有损压缩
        // 反之使用无损压缩

        ArraySegment<byte>? losslessImage = result.TryGet(1)?.TryGetBytes();
        ArraySegment<byte>? lossyImage = result.TryGet(2)?.TryGetBytes();

        if (image.Size <= 128 * 1024 && losslessImage.HasValue)
        {
            return losslessImage.Value.ToArray();
        }

        if (lossyImage.HasValue)
        {
            return lossyImage.Value.ToArray();
        }

        throw new BlogCommandException($"Failed to convert {image.File.Name} to webp format: return value is null.");
    }
}
