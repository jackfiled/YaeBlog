using DotNext;
using Microsoft.Extensions.Options;
using Moq;
using YaeBlog.Abstractions.Models;
using YaeBlog.Services;

namespace YaeBlog.Tests;

public sealed class GiteaFetchServiceTests
{
    private static readonly Mock<IOptions<GiteaOptions>> s_giteaOptionsMock = new();
    private readonly GiteaFetchService _giteaFetchService;

    public GiteaFetchServiceTests()
    {
        s_giteaOptionsMock.SetupGet(o => o.Value)
            .Returns(new GiteaOptions
            {
                BaseAddress = "https://git.rrricardo.top/api/v1/",
                ApiKey = "7e33617e5d084199332fceec3e0cb04c6ddced55",
                HeatMapUsername = "jackfiled"
            });

        _giteaFetchService = new GiteaFetchService(s_giteaOptionsMock.Object, new HttpClient());
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
