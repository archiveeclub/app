using Microsoft.EntityFrameworkCore;
using UsbMediaManager.Data;
using UsbMediaManager.Models;

namespace UsbMediaManager.Services;

public record SeriesHolder(
    Customer Customer, Drive Drive,
    int SeasonWatched, int EpisodeWatched,
    int? TotalEpisodes, int EpisodesBehind);

public class MediaService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TmdbService _tmdb;

    public MediaService(IDbContextFactory<AppDbContext> factory, TmdbService tmdb)
    {
        _factory = factory;
        _tmdb = tmdb;
    }

    /// <summary>میدیا رو (از روی TMDB) کش میکنه یا میسازه.</summary>
    public async Task<MediaItem> GetOrCreateMediaAsync(TmdbResult r)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.MediaItems
            .FirstOrDefaultAsync(m => m.TmdbId == r.TmdbId && m.Type == r.Type);
        if (existing != null) return existing;

        int? seasons = r.TotalSeasons, episodes = r.TotalEpisodes;
        if (r.Type == MediaType.Series && episodes == null)
            (seasons, episodes) = await _tmdb.GetSeriesCountsAsync(r.TmdbId);

        var media = new MediaItem
        {
            TmdbId = r.TmdbId, Type = r.Type, Title = r.Title,
            OriginalTitle = r.OriginalTitle, Year = r.Year,
            PosterUrl = r.PosterUrl, Overview = r.Overview,
            TotalSeasons = seasons, TotalEpisodes = episodes
        };
        db.MediaItems.Add(media);
        await db.SaveChangesAsync();
        return media;
    }

    /// <summary>افزودن فیلم/سریال به لیست یک فلش.</summary>
    public async Task AddMediaToDriveAsync(int driveId, int mediaItemId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var exists = await db.DriveMedia
            .AnyAsync(dm => dm.DriveId == driveId && dm.MediaItemId == mediaItemId);
        if (exists) return;
        db.DriveMedia.Add(new DriveMedia { DriveId = driveId, MediaItemId = mediaItemId });
        await db.SaveChangesAsync();
    }

    public async Task<List<MediaItem>> GetDriveMediaAsync(int driveId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.DriveMedia
            .Where(dm => dm.DriveId == driveId)
            .Include(dm => dm.MediaItem)
            .Select(dm => dm.MediaItem)
            .ToListAsync();
    }

    /// <summary>آپدیت پیشرفت دیدن سریال برای مشتری.</summary>
    public async Task SetProgressAsync(int customerId, int mediaItemId, int season, int episode)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var p = await db.SeriesProgress
            .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.MediaItemId == mediaItemId);
        if (p == null)
        {
            p = new SeriesProgress { CustomerId = customerId, MediaItemId = mediaItemId };
            db.SeriesProgress.Add(p);
        }
        p.SeasonWatched = season;
        p.EpisodeWatched = episode;
        p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// سرچ «کیا این سریال رو دارن» — همه‌ی مشتری‌هایی که این سریال
    /// روی یکی از فلش‌هاشون دارن + تا کجا دیدن + چند قسمت عقبن.
    /// </summary>
    public async Task<List<SeriesHolder>> FindHoldersAsync(int mediaItemId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var media = await db.MediaItems.FindAsync(mediaItemId);
        var total = media?.TotalEpisodes;

        var drivesWith = await db.DriveMedia
            .Where(dm => dm.MediaItemId == mediaItemId)
            .Include(dm => dm.Drive).ThenInclude(d => d.Customer)
            .ToListAsync();

        var progressList = await db.SeriesProgress
            .Where(sp => sp.MediaItemId == mediaItemId)
            .ToListAsync();

        var result = new List<SeriesHolder>();
        foreach (var dm in drivesWith)
        {
            var prog = progressList.FirstOrDefault(p => p.CustomerId == dm.Drive.CustomerId);
            var watched = prog?.EpisodeWatched ?? 0;
            var behind = total.HasValue ? Math.Max(0, total.Value - watched) : 0;
            result.Add(new SeriesHolder(
                dm.Drive.Customer, dm.Drive,
                prog?.SeasonWatched ?? 0, watched, total, behind));
        }
        return result;
    }
}