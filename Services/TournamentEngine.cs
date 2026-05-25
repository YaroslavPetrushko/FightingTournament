using FightingTournament.Models;
using System.Collections.Generic;

namespace FightingTournament.Services;

/// <summary>
/// Creates round-robin schedules (circle / polygon algorithm) and
/// commits cycle results into player statistics.
///
/// For N players: N-1 cycles, N/2 matches per cycle (odd N adds a BYE slot).
/// </summary>
public static class TournamentEngine
{
    public static Tournament Create(IReadOnlyList<string> playerNames)
    {
        var tournament = new Tournament();

        foreach (var name in playerNames)
            tournament.Players.Add(new Player { Name = name.Trim() });

        BuildSchedule(tournament);
        return tournament;
    }

    // ── Schedule ─────────────────────────────────────────────────────

    private static void BuildSchedule(Tournament t)
    {
        var slots = new List<Player>(t.Players);

        // Odd count → virtual BYE player; BYE matches are skipped
        Player? bye = null;
        if (slots.Count % 2 != 0)
        {
            bye = new Player { Name = "BYE" };
            slots.Add(bye);
        }

        int n           = slots.Count;   // always even
        int totalRounds = n - 1;

        for (int round = 0; round < totalRounds; round++)
        {
            var cycle = new Cycle(round + 1);

            for (int i = 0; i < n / 2; i++)
            {
                var p1 = slots[i];
                var p2 = slots[n - 1 - i];

                if (p1 == bye || p2 == bye) continue;   // skip BYE pairings

                cycle.Matches.Add(new Match(p1, p2));
            }

            t.Cycles.Add(cycle);

            // Rotate: keep slots[0] fixed, rotate the rest clockwise
            var last = slots[n - 1];
            slots.RemoveAt(n - 1);
            slots.Insert(1, last);
        }
    }

    // ── Commit ───────────────────────────────────────────────────────

    /// <summary>
    /// Validates that every match in the current cycle has a result,
    /// then writes W/L + character data to each Player model.
    /// Advances CurrentCycleIndex on success.
    /// </summary>
    /// <returns>True if committed successfully; false if cycle is incomplete.</returns>
    public static bool CommitCurrentCycle(Tournament tournament)
    {
        var cycle = tournament.CurrentCycle;
        if (cycle is null || !cycle.IsCompleted) return false;

        foreach (var match in cycle.Matches)
        {
            bool p1Won = match.WinnerId == 1;
            match.Player1.RecordResult(p1Won,  match.Character1);
            match.Player2.RecordResult(!p1Won, match.Character2);
        }

        tournament.CurrentCycleIndex++;
        return true;
    }
}