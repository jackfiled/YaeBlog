﻿namespace YaeBlog.Core.Models;

public class BlogEssay
{
    public required string Title { get; init; }

    public required DateTime PublishTime { get; init; }

    public List<string> Tags { get; } = [];

    public required string HtmlContent { get; init; }

    public override string ToString()
    {
        return $"{Title}-{PublishTime}";
    }
}