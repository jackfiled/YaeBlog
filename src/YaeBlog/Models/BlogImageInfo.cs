using System.Text;

namespace YaeBlog.Models;

public record BlogImageInfo(FileInfo File, long Width, long Height, string MineType, byte[] Content, bool IsUsed)
    : IComparable<BlogImageInfo>
{
    public int Size => Content.Length;

    public override string ToString()
    {
        StringBuilder builder = new();

        builder.AppendLine($"Blog image {File.Name}:");
        builder.AppendLine($"\tWidth: {Width}; Height: {Height}");
        builder.AppendLine($"\tSize: {FormatSize()}");
        builder.AppendLine($"\tImage Format: {MineType}");

        return builder.ToString();
    }

    public int CompareTo(BlogImageInfo? other)
    {
        if (other is null)
        {
            return -1;
        }

        return other.Size.CompareTo(Size);
    }

    private string FormatSize()
    {
        double size = Size;
        if (size / 1024 > 3)
        {
            size /= 1024;

            return size / 1024 > 3 ? $"{size / 1024}MB" : $"{size}KB";
        }

        return $"{size}B";
    }
}
