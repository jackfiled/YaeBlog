namespace YaeBlog.Abstractions.Models;

public record GitContributionItem(DateOnly Time, long ContributionCount)
{
    public string ItemId => $"item-{Time:yyyy-MM-dd}";
}

public record GitContributionGroupedByWeek(DateOnly Monday, List<GitContributionItem> Contributions);
