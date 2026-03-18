using DotNext;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using YaeBlog.Models;
using YaeBlog.Services;

namespace YaeBlog.Tests;

public sealed class GiteaFetchServiceTests
{
    private static readonly Mock<IOptions<GiteaOptions>> s_giteaOptionsMock = new();
    private static readonly Mock<ILogger<GiteaFetchService>> s_logger = new();
    private readonly GiteaFetchService _giteaFetchService;

    public GiteaFetchServiceTests()
    {
        s_giteaOptionsMock.SetupGet(o => o.Value)
            .Returns(new GiteaOptions
            {
                BaseAddress = "https://git.rrricardo.top/api/v1/", HeatMapUsername = "jackfiled"
            });

        _giteaFetchService = new GiteaFetchService(s_giteaOptionsMock.Object, new HttpClient(), s_logger.Object);
    }

    [Fact]
    public async Task FetchHeapMapTest()
    {
        Result<List<GitContributionItem>> r = await _giteaFetchService.FetchGiteaContributions("jackfiled");

        Assert.Null(r.Error);
        Assert.True(r.TryGet(out List<GitContributionItem>? items));
        Assert.NotNull(items);

        Assert.True(items.Count > 0);
    }
}
