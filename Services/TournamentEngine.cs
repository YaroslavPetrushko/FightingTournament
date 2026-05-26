using FightingTournament.Models;
using System.Collections.Generic;
using System.Linq;

namespace FightingTournament.Services;

/// <summary>
/// Each cycle = ALL unique pairs among active players (full round-robin).
/// For N=4: 6 matches per cycle (C(4,2)).
/// Cycles are created lazily — after each commit a new one is appended,
/// so the tournament never "auto-finishes"; the user decides when to stop.
/// </summary>
public static class TournamentEngine
{
    // ── Create ───────────────────────────────────────────────────────

    public static Tournament Create(IReadOnlyList<string> playerNames)
    {
        var t = new Tournament();
        foreach (var name in playerNames)
            t.Players.Add(new Player { Name = name.Trim() });

        t.Cycles.Add(BuildCycle(t, 1));
        return t;
    }

    // ── Cycle builder ────────────────────────────────────────────────

    /// <summary>Generates ALL unique pairs from currently-active players.</summary>
    private static Cycle BuildCycle(Tournament t, int number)
    {
        var cycle   = new Cycle(number);
        var active  = t.Players.Where(p => !p.IsEliminated).ToList();

        for (int i = 0; i < active.Count; i++)
            for (int j = i + 1; j < active.Count; j++)
                cycle.Matches.Add(new Match(active[i], active[j]));

        return cycle;
    }

    // ── Commit ───────────────────────────────────────────────────────

    /// <summary>
    /// Validates, records W/L stats for the current cycle, advances the index,
    /// and eagerly appends the next empty cycle.
    /// Returns false if any match is still incomplete.
    /// </summary>
    /// <returns>True if committed successfully; false if cycle is incomplete.</returns>
    public static bool CommitCurrentCycle(Tournament tournament)
    {
        var cycle = tournament.CurrentCycle;
        if (cycle is null || !cycle.IsCompleted) return false;

        foreach (var m in cycle.Matches)
        {
            bool p1Won = m.WinnerId == 1;
            m.Player1.RecordResult(p1Won,  m.Character1);
            m.Player2.RecordResult(!p1Won, m.Character2);
        }

        tournament.CurrentCycleIndex++;

        // Lazily create next cycle
        int nextNum = tournament.Cycles.Count + 1;
        tournament.Cycles.Add(BuildCycle(tournament, nextNum));

        return true;
    }

    // ── Eliminate ────────────────────────────────────────────────────

    /// <summary>
    /// Marks a player as eliminated.
    /// Removes their UNPLAYED matches from the current cycle.
    /// Completed matches (results already entered) are kept intact — their
    /// stats contribution remains valid.
    /// No new matches are injected: all pairings among the remaining players
    /// that don't involve the eliminated player are already present.
    /// </summary>
    public static void EliminatePlayer(Tournament tournament, Player player)
    {
        player.IsEliminated = true;

        var cycle = tournament.CurrentCycle;
        if (cycle is null) return;

        var toRemove = cycle.Matches
            .Where(m => !m.IsCompleted &&
                        (m.Player1 == player || m.Player2 == player))
            .ToList();

        foreach (var m in toRemove)
            cycle.Matches.Remove(m);
    }
}
