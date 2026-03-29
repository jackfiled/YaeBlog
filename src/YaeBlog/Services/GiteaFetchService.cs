using System.Net.Http.Headers;
using System.Text.Json;
using DotNext;
using Microsoft.Extensions.Options;
using YaeBlog.Core.Exceptions;
using YaeBlog.Abstractions.Models;

namespace YaeBlog.Services;

public sealed class GiteaFetchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GiteaFetchService> _logger;

    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true
    };

    /// <summary>
    /// For test only.
    /// </summary>
    internal GiteaFetchService(IOptions<GiteaOptions> giteaOptions, HttpClient httpClient,
        ILogger<GiteaFetchService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(giteaOptions.Value.BaseAddress);
        if (string.IsNullOrWhiteSpace(giteaOptions.Value.ApiKey))
        {
            return;
        }

        logger.LogInformation("Api Token is set.");
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("token", giteaOptions.Value.ApiKey);
    }

    public GiteaFetchService(IOptions<GiteaOptions> giteaOptions, IHttpClientFactory httpClientFactory,
        ILogger<GiteaFetchService> logger) : this(giteaOptions, httpClientFactory.CreateClient(), logger)
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

            _logger.LogInformation("Fetch new user heat map data.");
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
