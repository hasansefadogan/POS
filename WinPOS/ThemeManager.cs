using System;
using System.Linq;
using System.Windows;

namespace WinPOS
{
    public static class ThemeManager
    {
        public static void SetTheme(string theme) // "Light" veya "Dark"
        {
            var uri = new Uri($"Themes/{theme}.xaml", UriKind.Relative);
            var dict = new ResourceDictionary { Source = uri };

            var app = Application.Current;
            var merged = app.Resources.MergedDictionaries;

            // mevcut tema sözlüğünü kaldır
            var old = merged.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.StartsWith("Themes/"));
            if (old != null) merged.Remove(old);

            merged.Add(dict);
        }
    }
}
