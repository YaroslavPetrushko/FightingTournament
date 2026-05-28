using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace FightingTournament.Services;

public static class ThemeManager
{
    private const string ThemeSettingKey = "SelectedTheme";
    
    public static string CurrentTheme { get; private set; } = "default";

    private static readonly Dictionary<string, Dictionary<string, string>> Themes = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "default", new()
            {
                { "BrushBg", "#0D0D15" },
                { "BrushSurface", "#16161F" },
                { "BrushSurface2", "#1E1E2A" },
                { "BrushSurface3", "#26263A" },
                { "BrushAccent", "#E8003D" },
                { "BrushAccentDim", "#8C0025" },
                { "BrushAccentHover", "#FF1A54" },
                { "BrushText", "#E8E8F2" },
                { "BrushTextDim", "#7070A0" },
                { "BrushBorder", "#2A2A3E" },
                { "BrushBorderLight", "#3A3A52" },
                { "BrushWin", "#00CC6A" },
                { "BrushLoss", "#FF4444" },
                { "BrushCurrent", "#FF9800" },
                { "BrushElim", "#26161B" }
            }
        },
        {
            "volt_green", new()
            {
                { "BrushBg", "#0B0F0B" },
                { "BrushSurface", "#121A12" },
                { "BrushSurface2", "#1B261B" },
                { "BrushSurface3", "#243524" },
                { "BrushAccent", "#107C10" },
                { "BrushAccentDim", "#0A4D0A" },
                { "BrushAccentHover", "#139C13" },
                { "BrushText", "#E2EAE2" },
                { "BrushTextDim", "#6E806E" },
                { "BrushBorder", "#243324" },
                { "BrushBorderLight", "#364C36" },
                { "BrushWin", "#00CC6A" },
                { "BrushLoss", "#FF4444" },
                { "BrushCurrent", "#FF9800" },
                { "BrushElim", "#1E251E" }
            }
        },
        {
            "electric_blue", new()
            {
                { "BrushBg", "#090C15" },
                { "BrushSurface", "#101424" },
                { "BrushSurface2", "#181D33" },
                { "BrushSurface3", "#222947" },
                { "BrushAccent", "#00D2FF" },
                { "BrushAccentDim", "#007CA3" },
                { "BrushAccentHover", "#33DBFF" },
                { "BrushText", "#E0E6ED" },
                { "BrushTextDim", "#6B7C96" },
                { "BrushBorder", "#252F4F" },
                { "BrushBorderLight", "#374573" },
                { "BrushWin", "#00CC6A" },
                { "BrushLoss", "#FF4444" },
                { "BrushCurrent", "#FF9800" },
                { "BrushElim", "#151B2B" }
            }
        },
        {
            "deep_dark", new()
            {
                { "BrushBg", "#0A0813" },
                { "BrushSurface", "#120F20" },
                { "BrushSurface2", "#1A162D" },
                { "BrushSurface3", "#241E3D" },
                { "BrushAccent", "#8B5CF6" },
                { "BrushAccentDim", "#5B21B6" },
                { "BrushAccentHover", "#A78BFA" },
                { "BrushText", "#EDE9FE" },
                { "BrushTextDim", "#7C72A5" },
                { "BrushBorder", "#2B2348" },
                { "BrushBorderLight", "#3D3266" },
                { "BrushWin", "#10B981" },
                { "BrushLoss", "#EF4444" },
                { "BrushCurrent", "#F59E0B" },
                { "BrushElim", "#1B1629" }
            }
        },
        {
            "minimalist", new()
            {
                { "BrushBg", "#121212" },
                { "BrushSurface", "#1E1E1E" },
                { "BrushSurface2", "#262626" },
                { "BrushSurface3", "#303030" },
                { "BrushAccent", "#4A4A4A" },
                { "BrushAccentDim", "#2B2B2B" },
                { "BrushAccentHover", "#606060" },
                { "BrushText", "#F5F5F5" },
                { "BrushTextDim", "#888888" },
                { "BrushBorder", "#2C2C2C" },
                { "BrushBorderLight", "#3E3E3E" },
                { "BrushWin", "#888888" },
                { "BrushLoss", "#404040" },
                { "BrushCurrent", "#606060" },
                { "BrushElim", "#202020" }
            }
        },
        {
            "white", new()
            {
                { "BrushBg", "#F9FAFB" },
                { "BrushSurface", "#FFFFFF" },
                { "BrushSurface2", "#F3F4F6" },
                { "BrushSurface3", "#E5E7EB" },
                { "BrushAccent", "#4F46E5" },
                { "BrushAccentDim", "#C7D2FE" },
                { "BrushAccentHover", "#6366F1" },
                { "BrushText", "#111827" },
                { "BrushTextDim", "#6B7280" },
                { "BrushBorder", "#E5E7EB" },
                { "BrushBorderLight", "#D1D5DB" },
                { "BrushWin", "#10B981" },
                { "BrushLoss", "#EF4444" },
                { "BrushCurrent", "#F59E0B" },
                { "BrushElim", "#FEE2E2" }
            }
        }
    };

    public static void Initialize()
    {
        // Try to load persisted theme from SQLite UserSettings
        string savedTheme = DatabaseConnector.Instance.GetSetting(ThemeSettingKey, "default");
        ApplyTheme(savedTheme);
    }

    public static void ApplyTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName) || !Themes.ContainsKey(themeName))
        {
            themeName = "default";
        }

        CurrentTheme = themeName.ToLower();
        var palette = Themes[CurrentTheme];

        foreach (var pair in palette)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(pair.Value);
                var brush = new SolidColorBrush(color);
                brush.Freeze(); // Speed up WPF rendering and ensure thread safety
                
                // Overwrite the dynamic resource key at the Application level
                Application.Current.Resources[pair.Key] = brush;

                // Dynamically register the raw Color value (e.g. ColorBg instead of BrushBg)
                string colorKey = pair.Key.Replace("Brush", "Color");
                Application.Current.Resources[colorKey] = color;
            }
            catch (Exception)
            {
                // Fallback / skip invalid color format
            }
        }

        // Save selection to SQLite UserSettings
        DatabaseConnector.Instance.SaveSetting(ThemeSettingKey, CurrentTheme);
    }
}
