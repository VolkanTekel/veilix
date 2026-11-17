using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Ada;
using Windows.ApplicationModel.DataTransfer;

using static Veilix.UiLanguage;
namespace Veilix;

public sealed partial class MainWindow
{
    const int ClipboardLimit = 50;
    const int ClipboardTextLimit = 65536;
    static readonly string ClipboardPath = Path.Combine(Settings.DirectoryPath, "clipboard-history.json");
    List<string> clipboardHistory = new();
    int clipboardReadVersion;
    bool clipboardReading, clipboardPending;
    bool clipboardWriteFailed;

    void StartClipboardHistory()
    {
        try
        {
            if (File.Exists(ClipboardPath))
                clipboardHistory = (JsonSerializer.Deserialize<List<string>>(File.ReadAllText(ClipboardPath)) ?? new())
                    .Where(IsClipboardTextAllowed).Distinct().Take(ClipboardLimit).ToList();
        }
        catch (Exception) { clipboardWriteFailed = true; }
        host.ListenToClipboard(prefs.ClipboardHistoryEnabled);
    }

    static bool IsClipboardTextAllowed(string text) => !string.IsNullOrWhiteSpace(text) && text.Length <= ClipboardTextLimit;

    async Task RecordClipboard()
    {
        if (disposed || testing || !prefs.ClipboardHistoryEnabled) return;
        clipboardReadVersion++;
        clipboardPending = true;
        if (clipboardReading) return;
        clipboardReading = true;
        try
        {
            while (clipboardPending && !disposed && prefs.ClipboardHistoryEnabled)
            {
                clipboardPending = false;
                int version = clipboardReadVersion;
                string text = await Task.Run(DesktopClipboard.ReadText);
                if (disposed || version != clipboardReadVersion || !prefs.ClipboardHistoryEnabled ||
                    !RememberClipboardText(text)) continue;
                SaveClipboardHistory();
                if (route == "clipboard") Render();
            }
        }
        finally { clipboardReading = false; }
    }

    bool RememberClipboardText(string text)
    {
        if (!IsClipboardTextAllowed(text) || clipboardHistory.FirstOrDefault() == text) return false;
        clipboardHistory.Remove(text);
        clipboardHistory.Insert(0, text);
        if (clipboardHistory.Count > ClipboardLimit) clipboardHistory.RemoveAt(ClipboardLimit);
        return true;
    }

    void SaveClipboardHistory()
    {
        if (testing) return;
        try
        {
            Directory.CreateDirectory(Settings.DirectoryPath);
            string temp = ClipboardPath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(clipboardHistory));
            File.Move(temp, ClipboardPath, true);
            clipboardWriteFailed = false;
        }
        catch (Exception) { clipboardWriteFailed = true; }
    }

    void RenderClipboard()
    {
        Heading(L("Pano"), L("Son 50 metin kopyası Veilix klasöründe saklanır."));
        Toggle(L("Pano geçmişini kaydet"), prefs.ClipboardHistoryEnabled, enabled =>
        {
            prefs.ClipboardHistoryEnabled = enabled;
            clipboardReadVersion++;
            host.ListenToClipboard(enabled);
            Save();
        });
        if (clipboardWriteFailed) page.Children.Add(Text(L("Pano geçmişi dosyasına erişilemedi."), 13, false, true));
        if (clipboardHistory.Count == 0) page.Children.Add(Text(L("Kayıtlı metin yok."), 14, false, true));
        else
        {
            page.Children.Add(Button(L("Geçmişi temizle"), async () =>
            {
                if (!await Confirm(ConfirmDialog(L("Pano geçmişi temizlensin mi?"), L("Veilix’te kaydedilmiş metinler kaldırılacak."), L("Temizle")))) return;
                clipboardReadVersion++;
                clipboardHistory.Clear();
                SaveClipboardHistory();
                if (route == "clipboard") Render();
            }, "delete"));
            var status = Text("", 12, false, true);
            page.Children.Add(status);
            foreach (string text in clipboardHistory)
            {
                string preview = text.Length > 130 ? text[..130] + "…" : text;
                page.Children.Add(Button(preview, () =>
                {
                    try
                    {
                        var data = new DataPackage();
                        data.SetText(text);
                        Clipboard.SetContent(data);
                        Clipboard.Flush();
                        status.Text = L("Metin yeniden kopyalandı.");
                    }
                    catch (Exception) { status.Text = L("Metin kopyalanamadı. Tekrar dene."); }
                }));
            }
        }
    }
}

internal static class DesktopClipboard
{
    const uint UnicodeText = 13;
    static readonly uint ExcludeFormat = RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing");
    static readonly uint HistoryFormat = RegisterClipboardFormat("CanIncludeInClipboardHistory");

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern uint RegisterClipboardFormat(string name);
    [DllImport("user32.dll")] static extern bool IsClipboardFormatAvailable(uint format);
    [DllImport("user32.dll")] static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] static extern bool CloseClipboard();
    [DllImport("user32.dll")] static extern IntPtr GetClipboardData(uint format);
    [DllImport("kernel32.dll")] static extern IntPtr GlobalLock(IntPtr handle);
    [DllImport("kernel32.dll")] static extern bool GlobalUnlock(IntPtr handle);
    [DllImport("kernel32.dll")] static extern UIntPtr GlobalSize(IntPtr handle);

    internal static async Task<string> ReadText()
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    if (IsClipboardFormatAvailable(ExcludeFormat) || HistoryDisabled()) return null;
                    IntPtr handle = GetClipboardData(UnicodeText);
                    ulong bytes = GlobalSize(handle).ToUInt64();
                    if (bytes < 2 || bytes > 2 * 65537) return null;
                    IntPtr data = GlobalLock(handle);
                    if (data == IntPtr.Zero) return null;
                    try
                    {
                        string text = Marshal.PtrToStringUni(data, (int)(bytes / 2));
                        int terminator = text.IndexOf('\0');
                        return terminator >= 0 ? text[..terminator] : null;
                    }
                    finally { GlobalUnlock(handle); }
                }
                finally { CloseClipboard(); }
            }
            await Task.Delay(60).ConfigureAwait(false);
        }
        return null;
    }

    static bool HistoryDisabled()
    {
        if (!IsClipboardFormatAvailable(HistoryFormat)) return false;
        IntPtr handle = GetClipboardData(HistoryFormat);
        if (GlobalSize(handle).ToUInt64() < 4) return true;
        IntPtr data = GlobalLock(handle);
        if (data == IntPtr.Zero) return true;
        try { return Marshal.ReadInt32(data) == 0; }
        finally { GlobalUnlock(handle); }
    }
}
