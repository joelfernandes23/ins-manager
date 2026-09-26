using System.Windows;
using System.Windows.Media;

namespace InsManager.App.Services;

public sealed class ThemeService
{
    public void Apply(string theme)
    {
        var light = string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase);
        Set("WindowBackgroundBrush", light ? "#F1F4F5" : "#0B1014");
        Set("PanelBackgroundBrush", light ? "#FFFFFF" : "#121A20");
        Set("InsetBackgroundBrush", light ? "#E9EEF0" : "#0B1014");
        Set("PrimaryTextBrush", light ? "#172027" : "#E8EEF2");
        Set("SecondaryTextBrush", light ? "#52616A" : "#8FA0AA");
        Set("MutedTextBrush", light ? "#73818A" : "#67737A");
        Set("BorderBrush", light ? "#CBD4D9" : "#25313A");
        Set("StatusBackgroundBrush", light ? "#E2E8EB" : "#172127");
    }

    private static void Set(string key, string color) =>
        Application.Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
}
