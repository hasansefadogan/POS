using System;
using System.Linq;
using System.Windows;

namespace WinPOS.Languages
{
    public static class LanguageManager
    {
        public static void SetLanguage(string code)
        {
            // code: "de-CH", "fr-CH", "en", "tr"
            var fileName = $"Strings.{code}.xaml";
            var uri = new Uri($"/Languages/{fileName}", UriKind.Relative);

            var dict = new ResourceDictionary { Source = uri };

            // Eski dil sözlüğünü kaldır (varsa)
            var oldDict = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null &&
                                     d.Source.OriginalString.Contains("/Languages/Strings."));
            if (oldDict != null)
                Application.Current.Resources.MergedDictionaries.Remove(oldDict);

            // Yeni sözlüğü ekle
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
