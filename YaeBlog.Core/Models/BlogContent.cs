﻿namespace YaeBlog.Core.Models;

public class BlogContent
{
    public required string FileName { get; init; }

    public required MarkdownMetadata Metadata { get; init; }

    public required string FileContent { get; set; }
}
