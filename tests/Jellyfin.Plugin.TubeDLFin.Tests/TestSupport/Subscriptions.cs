using System;
using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Subscriptions;

namespace Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;

internal static class Subscriptions
{
    public static readonly DateTimeOffset Created = new(2026, 10, 18, 8, 0, 0, TimeSpan.Zero);

    public static readonly DownloadProfile Profile = new() { LibraryId = DownloadPlans.Movies.Id, LibraryFolder = "/media/movies" };

    public static Subscription Create(IReadOnlyList<string>? baseline = null, bool newestFirst = true) => new()
    {
        Id = Guid.Parse("8f1d2a3b-4c5d-4e6f-8a9b-0c1d2e3f4a5b"),
        Name = "Blender",
        Url = new Uri("https://www.youtube.com/@BlenderOfficial/videos"),
        ListLimit = 50,
        NewestFirst = newestFirst,
        Profile = Profile,
        CreatedAt = Created,
        NextCheckAt = Created.AddHours(6),
        Baseline = baseline ?? [],
    };

    public static FeedEntry Entry(string id) =>
        new(DownloadArchiveKeys.For("Youtube", id), "Video " + id, new Uri("https://www.youtube.com/watch?v=" + id));
}
