using System.Collections.Generic;
using System.Linq;

namespace FightingTournament.Models;

public class Player
{
    public string Name { get; init; } = string.Empty;

    public int TotalWins    { get; private set; }
    public int TotalLosses  { get; private set; }
    public int TotalMatches { get; private set; }

    public double WinRate =>
        TotalMatches > 0 ? (double)TotalWins / TotalMatches * 100.0 : 0.0;

    /// <summary>Eliminated mid-tournament. Stats are preserved; future matches are removed.</summary>
    public bool IsEliminated { get; set; }

    private readonly Dictionary<string, int> _charPicks =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> CharacterPicks => _charPicks;

    public string MostPickedCharacter =>
        _charPicks.Count == 0
            ? "—"
            : _charPicks.OrderByDescending(kv => kv.Value).First().Key;

    /// <summary>Records one match result and optionally logs the character played.</summary>
    public void RecordResult(bool won, string? character)
    {
        TotalMatches++;
        if (won) TotalWins++;
        else     TotalLosses++;

        if (!string.IsNullOrWhiteSpace(character))
        {
            _charPicks.TryGetValue(character, out int n);
            _charPicks[character] = n + 1;
        }
    }

    public void Reset()
    {
        TotalWins = TotalLosses = TotalMatches = 0;
        IsEliminated = false;
        _charPicks.Clear();
    }

    public void LoadPersistedStats(int wins, int losses, int matches, Dictionary<string, int> picks)
    {
        TotalWins = wins;
        TotalLosses = losses;
        TotalMatches = matches;
        _charPicks.Clear();
        foreach (var kv in picks)
        {
            _charPicks[kv.Key] = kv.Value;
        }
    }
}
