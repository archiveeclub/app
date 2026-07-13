using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using UsbMediaManager.Models;

namespace UsbMediaManager.Services;

public record TmdbResult(
    int TmdbId, MediaType Type, string Title, string? OriginalTitle,
    int? Year, string? PosterUrl, string? Overview,
    int? TotalSeasons, int? TotalEpisodes);

public class TmdbService
{
    private readonly HttpClient _http = new();
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _imageBaseUrl;
    private readonly string _language;

    public TmdbService(IConfiguration config)
    {
        _apiKey = config["Tmdb:ApiKey"] ?? string.Empty;
        _baseUrl = config["Tmdb:BaseUrl"] ?? "https://api.themoviedb.org/3";
        _imageBaseUrl = config["Tmdb:ImageBaseUrl"] ?? "https://image.tmdb.org/t/p/w200";
        _language = config["Tmdb:Language"] ?? "en-US";
    }

    /// <summary>سرچ ترکیبی فیلم + سریال (multi search).</summary>
    public async Task<List<TmdbResult>> SearchAsync(string query)
    {
        var url = $"{_baseUrl}/search/multi?api_key={_apiKey}" +
                  $"&language={_language}&query={Uri.EscapeDataString(query)}";
        var resp = await _http.GetFromJsonAsync<TmdbSearchResponse>(url);
        var list = new List<TmdbResult>();
        if (resp?.Results == null) return list;

        foreach (var r in resp.Results)
        {
            if (r.MediaType is not ("movie" or "tv")) continue;
            var type = r.MediaType == "tv" ? MediaType.Series : MediaType.Movie;
            var title = r.Title ?? r.Name ?? "(بدون عنوان)";
            var date = r.ReleaseDate ?? r.FirstAirDate;
            int? year = null;
            if (!string.IsNullOrEmpty(date) && date.Length >= 4 &&
                int.TryParse(date[..4], out var y)) year = y;

            list.Add(new TmdbResult(
                r.Id, type, title, r.OriginalTitle ?? r.OriginalName, year,
                r.PosterPath != null ? _imageBaseUrl + r.PosterPath : null,
                r.Overview, null, null));
        }
        return list;
    }

    /// <summary>جزئیات سریال: تعداد کل فصل و قسمت (برای ردیابی پیشرفت).</summary>
    public async Task<(int seasons, int episodes)> GetSeriesCountsAsync(int tvId)
    {
        var url = $"{_baseUrl}/tv/{tvId}?api_key={_apiKey}&language={_language}";
        var d = await _http.GetFromJsonAsync<TmdbTvDetail>(url);
        return (d?.NumberOfSeasons ?? 0, d?.NumberOfEpisodes ?? 0);
    }

    // ---- DTOهای پاسخ TMDB ----
    private class TmdbSearchResponse
    {
        [JsonPropertyName("results")] public List<TmdbItem>? Results { get; set; }
    }
    private class TmdbItem
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("media_type")] public string? MediaType { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("original_title")] public string? OriginalTitle { get; set; }
        [JsonPropertyName("original_name")] public string? OriginalName { get; set; }
        [JsonPropertyName("release_date")] public string? ReleaseDate { get; set; }
        [JsonPropertyName("first_air_date")] public string? FirstAirDate { get; set; }
        [JsonPropertyName("poster_path")] public string? PosterPath { get; set; }
        [JsonPropertyName("overview")] public string? Overview { get; set; }
    }
    private class TmdbTvDetail
    {
        [JsonPropertyName("number_of_seasons")] public int NumberOfSeasons { get; set; }
        [JsonPropertyName("number_of_episodes")] public int NumberOfEpisodes { get; set; }
    }
}