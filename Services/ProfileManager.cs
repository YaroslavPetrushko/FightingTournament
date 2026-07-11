using System;

namespace FightingTournament.Services;

public static class ProfileManager
{
    private const string ProfileSettingKey = "AssignedPlayerName";

    public static event Action? ProfileChanged;

    private static string? _assignedPlayer;

    static ProfileManager()
    {
        Initialize();
    }

    public static void Initialize()
    {
        try
        {
            string saved = DatabaseConnector.Instance.GetSetting(ProfileSettingKey, "");
            _assignedPlayer = string.IsNullOrWhiteSpace(saved) ? null : saved;
        }
        catch
        {
            _assignedPlayer = null;
        }
    }

    public static string? GetAssignedPlayerName()
    {
        return _assignedPlayer;
    }

    public static void AssignPlayer(string? name)
    {
        _assignedPlayer = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        try
        {
            DatabaseConnector.Instance.SaveSetting(ProfileSettingKey, _assignedPlayer ?? "");
        }
        catch
        {
            // Fail-safe if DB connection is busy or not initialized
        }
        ProfileChanged?.Invoke();
    }

    public static bool IsAssigned(string name)
    {
        if (string.IsNullOrEmpty(_assignedPlayer) || string.IsNullOrEmpty(name))
            return false;

        return _assignedPlayer.Equals(name, StringComparison.OrdinalIgnoreCase);
    }
}
