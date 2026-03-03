using System.Net.Http.Headers;
using System.Text.Json;
using DotNext;
using Microsoft.Extensions.Options;
using YaeBlog.Core.Exceptions;
using YaeBlog.Models;

namespace YaeBlog.Services;

public sealed class GiteaFetchService
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectRequiredConstructorParameters = true, RespectNullableAnnotations = true
    };

    /// <summary>
    /// For test only.
    /// </summary>
    internal GiteaFetchService(IOptions<GiteaOptions> giteaOptions, HttpClient httpClient)
    {
        _httpClient = httpClient;

        _httpClient.BaseAddress = new Uri(giteaOptions.Value.BaseAddress);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", giteaOptions.Value.ApiKey);
    }

    public GiteaFetchService(IOptions<GiteaOptions> giteaOptions, IHttpClientFactory httpClientFactory) : this(
        giteaOptions, httpClientFactory.CreateClient())
    {
    }

    private record UserHeatmapData(long Contributions, long Timestamp);

    public async Task<Result<List<GitContributionItem>>> FetchGiteaContributions(string username)
    {
        try
        {
            List<UserHeatmapData>? data =
                await _httpClient.GetFromJsonAsync<List<UserHeatmapData>>($"users/{username}/heatmap",
                    s_serializerOptions);

            if (data is null or { Count: 0 })
            {
                return Result.FromException<List<GitContributionItem>>(
                    new GiteaFetchException("Failed to fetch valid data."));
            }

            return Result.FromValue(data.Select(i =>
                new GitContributionItem(DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(i.Timestamp).DateTime),
                    i.Contributions)).ToList());
        }
        catch (HttpRequestException exception)
        {
            return Result.FromException<List<GitContributionItem>>(new GiteaFetchException("Failed to fetch.",
                exception));
        }
    }
}
