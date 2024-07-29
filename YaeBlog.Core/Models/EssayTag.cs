using System.Text.Encodings.Web;

namespace YaeBlog.Core.Models;

public class EssayTag(string tagName) : IEquatable<EssayTag>
{
    public string TagName { get; } = tagName;

    public string UrlEncodedTagName { get; } = UrlEncoder.Default.Encode(tagName);

    public bool Equals(EssayTag? other) => other is not null && TagName == other.TagName;

    public override bool Equals(object? obj) => obj is EssayTag other && Equals(other);

    public override int GetHashCode() => TagName.GetHashCode();
}
