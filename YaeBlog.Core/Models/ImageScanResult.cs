namespace YaeBlog.Core.Models;

public record struct ImageScanResult(List<FileInfo> UnusedImages, List<FileInfo> NotFoundImages);
