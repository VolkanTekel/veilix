using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ada;
using Microsoft.UI.Xaml.Controls;
using System.Web.Script.Serialization;

namespace Veilix;

public sealed partial class MainWindow
{
    async Task CheckRecentChanges(List<string> report)
    {
        var localTime = new DateTimeOffset(DateTime.Today.AddHours(23).AddMinutes(59));
        var history = new UtilitySession(true, clock: () => localTime);
        history.RecordNotice(new UtilityNotice { Kind = "system-network", Title = "Before midnight" });
        localTime = localTime.AddMinutes(2);
        Check(history.PruneOldNotices() && history.Data.Notices.Count == 0, "Old notices survived local midnight");
        history.RecordNotice(new UtilityNotice { Kind = "system-network", Title = "After midnight" });
        Check(!history.PruneOldNotices() && history.Data.Notices.Count == 1, "Today's notice was removed");
        report.Add("Notification history expires across local midnight and retains new notices.");

        var clips = clipboardHistory;
        clipboardHistory = new List<string>();
        try
        {
            for (int index = 0; index < 55; index++) RememberClipboardText("Clip " + index);
            Check(clipboardHistory.Count == 50 && clipboardHistory[0] == "Clip 54", "Clipboard cap or order");
            RememberClipboardText("Clip 10");
            Check(clipboardHistory.Count == 50 && clipboardHistory[0] == "Clip 10" &&
                clipboardHistory.Count(text => text == "Clip 10") == 1, "Clipboard duplicate remained");
        }
        finally { clipboardHistory = clips; }
        report.Add("Clipboard history keeps the newest 50 unique texts using isolated values.");

        var previousOrder = prefs.CompactOrder;
        try
        {
            prefs.CompactOrder = new List<string> { "date", "clock", "date", "obsolete" };
            ApplyCompactLayout();
            Check(Grid.GetColumn(compactElements["date"]) == 0 && Grid.GetColumn(clockArea) == 1 &&
                header.ColumnDefinitions.Count == 6, "Saved compact order was not applied");
            var serializer = new JavaScriptSerializer();
            var saved = serializer.Deserialize<Settings>(serializer.Serialize(prefs));
            Check(CompactOrder(saved.CompactOrder).SequenceEqual(CompactOrder(prefs.CompactOrder)), "Compact order did not round-trip");
        }
        finally { prefs.CompactOrder = previousOrder; ApplyCompactLayout(); }
        report.Add("Compact slot ordering applies to the real header and round-trips old/duplicate keys.");

        var browser = new TestMediaSession { Key = "alt-browser", AppId = "firefox.exe", Title = "Browser media", Playing = true };
        var spotify = new TestMediaSession { Key = "alt-spotify", AppId = "Spotify.exe", Title = "Spotify media", Playing = false };
        var backend = new TestMediaBackend { Items = new() { spotify, browser }, Current = browser };
        using var service = new MediaService(backend, false, () => DateTimeOffset.UtcNow);
        await service.Start();
        Check(service.Title == spotify.Title && browser.Reads == 0, "Spotify-only media default");
        service.SetOtherSources(true);
        for (int i = 0; i < 30 && service.Title != browser.Title; i++) await Task.Delay(20);
        Check(service.Title == browser.Title && service.Playing, "Other source did not replace paused Spotify");
        service.SetOtherSources(false);
        Check(service.Title != browser.Title, "Disabled media source stayed visible");
        for (int i = 0; i < 30 && service.Title != spotify.Title; i++) await Task.Delay(20);
        Check(service.Title == spotify.Title, "Spotify was not restored");
        Check(miniTitle.Text == "", "Idle header showed a media title");
        report.Add("Media source setting selects playing non-Spotify media and restores Spotify-only selection.");
    }
}
