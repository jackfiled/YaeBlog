using DotNext;
using Microsoft.Extensions.Options;
using YaeBlog.Extensions;
using YaeBlog.Abstractions.Models;

namespace YaeBlog.Services;

public sealed class GitHeapMapService(IServiceProvider serviceProvider, IOptions<GiteaOptions> giteaOptions,
    ILogger<GitHeapMapService> logger)
{
    /// <summary>
    /// 存储贡献列表
    /// 贡献列表采用懒加载和缓存机制，一天之内只请求一次Gitea服务器获得数据并缓存
    /// </summary>
    private List<GitContributionGroupedByWeek> _gitContributionsGroupedByWeek = [];

    /// <summary>
    /// 最后一次更新贡献列表的时间
    /// </summary>
    private DateOnly _updateTime = DateOnly.MinValue;

    public async Task<List<GitContributionGroupedByWeek>> GetGitContributionGroupedByWeek()
    {
        DateOnly today = DateOnly.FromDateTime(DateTimeOffset.Now.DateTime);
        if (_updateTime == today)
        {
            logger.LogDebug("Git contribution grouped by week cache is hit.");
            return _gitContributionsGroupedByWeek;
        }

        // 今天尚未更新
        // 更新一下
        GiteaFetchService giteaFetchService = serviceProvider.GetRequiredService<GiteaFetchService>();
        Result<List<GitContributionItem>> r =
            await giteaFetchService.FetchGiteaContributions(giteaOptions.Value.HeatMapUsername);

        if (!r.TryGet(out List<GitContributionItem>? items))
        {
            logger.LogError("Failed to fetch heatmap data: {}", r.Error);
            return _gitContributionsGroupedByWeek;
        }

        // The contribution is not grouped by day, so group them.
        IEnumerable<GitContributionItem> groupedItems = items
            .GroupBy(i => i.Time)
            .Select(group => new GitContributionItem(group.Key,
                group.Select(i => i.ContributionCount).Sum()));

        List<GitContributionGroupedByWeek> result = new(52);

        // Consider the input data is in order.
        // Start should be one year ago.
        GitContributionGroupedByWeek groupedContribution = new(DateOnly.Today.AddDays(-365 - 7).LastMonday, []);
        logger.LogDebug("Create new item group by week {}.", groupedContribution.Monday);

        foreach ((DateOnly date, long contributions) in groupedItems)
        {
            DateOnly mondayOfItem = date.LastMonday;
            logger.LogDebug("Current date of item: {item}, monday is {monday}", date, mondayOfItem);

            // If current item is in the same week of last item.
            if (mondayOfItem == groupedContribution.Monday)
            {
                // Fill the spacing of empty days with 0 contribution.
                FillSpacing(groupedContribution, date);

                groupedContribution.Contributions.Add(new GitContributionItem(date, contributions));
                continue;
            }

            // Current time is in the next (or much more) week of last item.
            // Fill the spacing, including the last week inner spacing and outer spacing.
            while (groupedContribution.Monday < mondayOfItem)
            {
                FillSpacing(groupedContribution, date);
                result.Add(groupedContribution);
                groupedContribution = new GitContributionGroupedByWeek(groupedContribution.Monday.AddDays(7), []);
                logger.LogDebug("Create new item group by week {}.", groupedContribution.Monday);
            }

            // Now, the inner spacing of one week.
            FillSpacing(groupedContribution, date);
            groupedContribution.Contributions.Add(new GitContributionItem(date, contributions));
        }

        // Not fill the last item and add directly.
        result.Add(groupedContribution);

        _gitContributionsGroupedByWeek = result;
        _updateTime = DateOnly.Today;

        return _gitContributionsGroupedByWeek;
    }

    private static void FillSpacing(GitContributionGroupedByWeek contribution, in DateOnly date)
    {
        if (contribution.Monday == date)
        {
            return;
        }

        if (contribution.Contributions.Count == 0)
        {
            contribution.Contributions.Add(new GitContributionItem(contribution.Monday, 0));
        }

        DateOnly lastDate = contribution.Contributions.Last().Time;
        // The day in one week is 7, so th count of items of one week should not bigger than 7.
        while (contribution.Contributions.Count < 7 && lastDate < date.AddDays(-1))
        {
            lastDate = lastDate.AddDays(1);
            contribution.Contributions.Add(new GitContributionItem(lastDate, 0));
        }
    }
}
