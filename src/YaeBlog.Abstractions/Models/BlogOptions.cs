using System.ComponentModel.DataAnnotations;

namespace YaeBlog.Abstractions.Models;

/// <summary>
/// 友链模型类
/// </summary>
public class FriendLink
{
    [Required] public required string Name { get; init; }

    [Required] public required string Description { get; init; }

    [Required] public required string Link { get; init; }

    [Required] public required string AvatarImage { get; init; }
}

public class BlogOptions
{
    public const string OptionName = "Blog";

    [Required] public required string Root { get; init; }

    [Required] public required string Announcement { get; init; }

    [Required] public required int StartYear { get; init; }

    [Required] public required List<FriendLink> Links { get; init; }
}
