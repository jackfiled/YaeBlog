namespace YaeBlog.Models;

public record GitContributionItem(DateOnly Time, long ContributionCount);

public record GitContributionGroupedByWeek(DateOnly Monday, List<GitContributionItem> Contributions);
