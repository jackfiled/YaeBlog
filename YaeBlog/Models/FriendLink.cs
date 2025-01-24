namespace YaeBlog.Models;

/// <summary>
/// 友链模型类
/// </summary>
public class FriendLink
{
    /// <summary>
    /// 友链名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 友链的简单介绍
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// 友链地址
    /// </summary>
    public required string Link { get; set; }

    /// <summary>
    /// 头像地址
    /// </summary>
    public required string AvatarImage { get; set; }
}
