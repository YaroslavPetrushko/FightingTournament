namespace FightingTournament.Models;

public class UserPreset
{
    public int Id { get; set; }
    public string PresetName { get; set; } = string.Empty;
    public string? DefaultGame { get; set; }
    public string? DefaultMode { get; set; }
    public int DefaultRounds { get; set; } = 3;
    public int PlayerCount { get; set; }
    public string? PlayerNames { get; set; } // Comma-separated player nicknames

    public override string ToString() => PresetName;
}
