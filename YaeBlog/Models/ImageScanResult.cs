namespace YaeBlog.Models;

public record struct ImageScanResult(List<FileInfo> UnusedImages, List<FileInfo> NotFoundImages);
