using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

using static Veilix.UiLanguage;
namespace Veilix;

public sealed partial class MainWindow
{
    void RenderWeatherSettings()
    {
        Toggle(L("Hava durumunu göster"), prefs.ShowWeather, value =>
        {
            prefs.ShowWeather = value;
            UpdateHeader();
            Save();
            _ = weather.Refresh(prefs);
        });
        var search = new TextBox { Header = L("Şehir veya ilçe"), Text = prefs.City, PlaceholderText = L("Konum ara…") };
        var results = new StackPanel { Spacing = 6 };
        var status = Text("", 13, false, true);
        int request = 0;
        bool IsCurrent() => !disposed && route == "settings" && settingsTab == L("Hava") && page.Children.Contains(search);

        async Task Search()
        {
            string query = search.Text.Trim();
            int id = ++request;
            results.Children.Clear();
            if (query.Length < 2)
            {
                status.Text = L("En az iki harf yaz.");
                return;
            }
            status.Text = L("Konum aranıyor…");
            try
            {
                var places = await weather.Search(query,English?"en":"tr");
                if (!IsCurrent() || id != request || search.Text.Trim() != query) return;
                status.Text = places.Count == 0 ? L("Konum bulunamadı. İl adıyla tekrar dene.") : L("Listeden konumu seç.");
                foreach (var place in places)
                    results.Children.Add(Button(place.Label, () =>
                    {
                        var previous = (prefs.City, prefs.Latitude, prefs.Longitude);
                        (prefs.City, prefs.Latitude, prefs.Longitude) = (place.Name, place.Latitude, place.Longitude);
                        try { if (!testing) prefs.Save(); }
                        catch (Exception)
                        {
                            (prefs.City, prefs.Latitude, prefs.Longitude) = previous;
                            status.Text = L("Konum kaydedilemedi. Veilix klasörünün yazılabilir olduğunu kontrol et.");
                            return;
                        }
                        search.Text = place.Name;
                        results.Children.Clear();
                        status.Text = Format("{0} kaydedildi.",place.Name);
                        lastForecast = DateTime.UtcNow;
                        _ = weather.Refresh(prefs);
                        UpdateHeader();
                    }));
            }
            catch (Exception ex)
            {
                if (!IsCurrent() || id != request) return;
                Program.Trace("Weather location lookup: " + ex.GetType().Name + " " + ex.HResult.ToString("X8"));
                status.Text = L("Konum servisine ulaşılamadı. İnternet bağlantısını kontrol edip tekrar dene.");
            }
        }
        search.TextChanged += (_, _) =>
        {
            request++;
            results.Children.Clear();
            status.Text = "";
        };
        search.KeyDown += (_, args) =>
        {
            if (args.Key != VirtualKey.Enter) return;
            args.Handled = true;
            _ = Search();
        };
        page.Children.Add(search);
        page.Children.Add(Button(L("Ara"), () => _ = Search(), "search"));
        page.Children.Add(status);
        page.Children.Add(results);
    }
}
