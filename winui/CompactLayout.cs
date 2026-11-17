using System;
using System.Collections.Generic;
using System.Linq;
using Ada;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

using static Veilix.UiLanguage;
namespace Veilix;

public sealed partial class MainWindow
{
    static readonly string[] DefaultCompactOrder = { "cover", "media", "clock", "battery", "weather", "date" };
    const string CompactDragFormat = "Veilix.CompactLayout";
    readonly Dictionary<string, FrameworkElement> compactElements = new();

    internal static List<string> CompactOrder(IEnumerable<string> saved)
    {
        return (saved ?? Array.Empty<string>()).Where(key => DefaultCompactOrder.Contains(key))
            .Concat(DefaultCompactOrder).Distinct().ToList();
    }

    void ApplyCompactLayout()
    {
        var order = CompactOrder(prefs.CompactOrder);
        header.ColumnDefinitions.Clear();
        for (int index = 0; index < order.Count; index++)
        {
            string key = order[index];
            var column = new ColumnDefinition
            {
                Width = key switch
                {
                    "cover" => new GridLength(36),
                    "media" => new GridLength(1, GridUnitType.Star),
                    "clock" => new GridLength(98),
                    "battery" => new GridLength(28),
                    _ => GridLength.Auto
                }
            };
            header.ColumnDefinitions.Add(column);
            Grid.SetColumn(compactElements[key], index);
        }
    }

    void RenderCompactLayout()
    {
        Heading(L("Küçük panel düzeni"), L("Öğeyi bir yuvanın üzerine sürükle. Bıraktığında iki öğe yer değiştirir."));
        var names = new Dictionary<string, string>
        {
            ["cover"] = L("Kapak"), ["media"] = L("Medya"), ["clock"] = L("Saat"),
            ["battery"] = L("Pil"), ["weather"] = L("Hava"), ["date"] = L("Tarih")
        };
        var order = CompactOrder(prefs.CompactOrder);
        var preview = new Grid { ColumnSpacing = 4 };
        string dragging = null;
        var tiles = new Dictionary<string, Border>();
        foreach (string key in order)
        {
            preview.ColumnDefinitions.Add(new ColumnDefinition());
            var label = Text(names[key], 12, true);
            label.TextAlignment = TextAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            var tile = new Border
            {
                Child = label, Height = 44, CornerRadius = new CornerRadius(8),
                Background = Tile(), CanDrag = true, AllowDrop = true
            };
            tiles.Add(key, tile);
            Grid.SetColumn(tile, order.IndexOf(key));
            preview.Children.Add(tile);
            tile.DragStarting += (_, args) =>
            {
                dragging = key;
                args.Data.SetData(CompactDragFormat, key);
                args.Data.RequestedOperation = DataPackageOperation.Move;
            };
            tile.DragOver += (_, args) =>
            {
                if (dragging == null || dragging == key || !args.DataView.Contains(CompactDragFormat)) return;
                args.AcceptedOperation = DataPackageOperation.Move;
                tile.Background = B(ThemePalette.Get(prefs).Light ? "#C6DFFF" : "#244972");
                args.Handled = true;
            };
            tile.DragLeave += (_, _) => tile.Background = Tile();
            tile.DropCompleted += (_, _) =>
            {
                dragging = null;
                foreach (var item in tiles.Values) item.Background = Tile();
            };
            tile.Drop += (_, args) =>
            {
                if (dragging == null || !args.DataView.Contains(CompactDragFormat)) return;
                args.Handled = true;
                int from = order.IndexOf(dragging), to = order.IndexOf(key);
                (order[from], order[to]) = (order[to], order[from]);
                foreach (var pair in tiles)
                {
                    Grid.SetColumn(pair.Value, order.IndexOf(pair.Key));
                    pair.Value.Background = Tile();
                }
                prefs.CompactOrder = order.ToList();
                ApplyCompactLayout();
                Save();
            };
        }
        page.Children.Add(preview);
        page.Children.Add(Button(L("Varsayılan sıraya dön"), () =>
        {
            prefs.CompactOrder = new List<string>();
            ApplyCompactLayout();
            Save();
            Render();
        }, "refresh"));
    }
}
