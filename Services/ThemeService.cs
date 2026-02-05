using System;
using System.Linq;
using System.Windows;

namespace OpenRdpGuard.Services
{
    public interface IThemeService
    {
        void ApplyTheme(AppTheme theme);
    }

    public class ThemeService : IThemeService
    {
        private const string LightUri = "Themes/Light.xaml";
        private const string DarkUri = "Themes/Dark.xaml";

        public void ApplyTheme(AppTheme theme)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var existing = dictionaries.FirstOrDefault(d => d.Source != null &&
                                                            (d.Source.OriginalString.EndsWith("Themes/Light.xaml", StringComparison.OrdinalIgnoreCase) ||
                                                             d.Source.OriginalString.EndsWith("Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase)));

            var targetUri = theme == AppTheme.Dark ? DarkUri : LightUri;
            var newDict = new ResourceDictionary { Source = new Uri(targetUri, UriKind.Relative) };

            if (existing != null)
            {
                var index = dictionaries.IndexOf(existing);
                dictionaries.RemoveAt(index);
                dictionaries.Insert(index, newDict);
            }
            else
            {
                dictionaries.Add(newDict);
            }
        }
    }
}
