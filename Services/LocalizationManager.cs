using System;
using System.IO;
using System.Windows;

namespace FightingTournament.Services;

public static class LocalizationManager
{
    private const string LanguageSettingKey = "SelectedLanguage";
    
    public static string CurrentLanguage { get; private set; } = "en";

    public static void Initialize()
    {
        // Load setting from DB, default to English
        string savedLanguage = DatabaseConnector.Instance.GetSetting(LanguageSettingKey, "en");
        ApplyLanguage(savedLanguage);
    }

    public static void ApplyLanguage(string lang)
    {
        lang = lang.ToLower() == "ua" ? "ua" : "en";
        CurrentLanguage = lang;

        // Swapping resource dictionaries
        var resources = Application.Current.Resources;
        ResourceDictionary? existing = null;

        // Try to find if there is an existing localization dictionary loaded
        foreach (var md in resources.MergedDictionaries)
        {
            if (md.Source != null && md.Source.OriginalString.Contains("Locales/Strings."))
            {
                existing = md;
                break;
            }
        }

        string uriString = $"/Locales/Strings.{CurrentLanguage}.xaml";
        var newDict = new ResourceDictionary
        {
            Source = new Uri(uriString, UriKind.RelativeOrAbsolute)
        };

        if (existing != null)
        {
            int index = resources.MergedDictionaries.IndexOf(existing);
            resources.MergedDictionaries[index] = newDict;
        }
        else
        {
            resources.MergedDictionaries.Add(newDict);
        }

        // Save selected language to SQLite database settings
        DatabaseConnector.Instance.SaveSetting(LanguageSettingKey, CurrentLanguage);
    }

    public static string GetString(string key)
    {
        if (Application.Current.TryFindResource(key) is string value)
        {
            return value;
        }
        return $"[{key}]";
    }
}
