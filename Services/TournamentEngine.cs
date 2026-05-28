using FightingTournament.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FightingTournament.Services;

/// <summary>
/// Dynamic tournament engine generating round-robin pairings for Endless Run lobbies,
/// and standard SE seeding brackets for Championship mode.
/// </summary>
public static class TournamentEngine
{
    // ── Create ───────────────────────────────────────────────────────

    public static Tournament Create(IReadOnlyList<string> playerNames, TournamentMode mode = TournamentMode.Endless, int defaultRounds = 3)
    {
        var t = new Tournament { Mode = mode, DefaultRounds = defaultRounds };

        if (mode == TournamentMode.Championship)
        {
            // 1. Calculate next power of 2
            int n = playerNames.Count;
            int k = 2;
            while (k < n) k *= 2;

            // 2. Add real players
            foreach (var name in playerNames)
                t.Players.Add(new Player { Name = name.Trim() });

            // 3. Add virtual BYE players
            int byesCount = k - n;
            for (int i = 0; i < byesCount; i++)
            {
                t.Players.Add(new Player { Name = "BYE" });
            }

            // 4. Build standard seeding bracket for Round 1
            var round1 = new Cycle(1);
            for (int i = 0; i < k / 2; i++)
            {
                var p1 = t.Players[i];
                var p2 = t.Players[k - 1 - i];
                var match = new Match(p1, p2) { Rounds = defaultRounds };
                round1.Matches.Add(match);

                // Auto-resolve matches containing BYE players
                if (p2.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase))
                {
                    match.WinnerId = 1;
                }
                else if (p1.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase))
                {
                    match.WinnerId = 2;
                }
            }
            t.Cycles.Add(round1);
        }
        else
        {
            foreach (var name in playerNames)
                t.Players.Add(new Player { Name = name.Trim() });

            t.Cycles.Add(BuildCycle(t, 1));
        }

        return t;
    }

    // ── Cycle builders ────────────────────────────────────────────────

    /// <summary>Generates ALL unique pairs from currently-active players (round-robin).</summary>
    private static Cycle BuildCycle(Tournament t, int number)
    {
        var cycle   = new Cycle(number);
        var active  = t.Players.Where(p => !p.IsEliminated).ToList();

        for (int i = 0; i < active.Count; i++)
            for (int j = i + 1; j < active.Count; j++)
                cycle.Matches.Add(new Match(active[i], active[j]) { Rounds = t.DefaultRounds });

        return cycle;
    }

    /// <summary>Pairs previous cycle winners sequentially for the next Single Elimination round.</summary>
    private static Cycle BuildChampionshipCycle(Tournament t, int number)
    {
        var cycle = new Cycle(number);
        var previousCycle = t.Cycles[t.CurrentCycleIndex - 1];

        // Collect winners of the previous round
        var winners = new List<Player>();
        foreach (var m in previousCycle.Matches)
        {
            var winner = m.WinnerId == 1 ? m.Player1 : m.Player2;
            winners.Add(winner);
        }

        // Pair them sequentially
        for (int i = 0; i < winners.Count; i += 2)
        {
            if (i + 1 < winners.Count)
            {
                var match = new Match(winners[i], winners[i + 1]) { Rounds = t.DefaultRounds };
                cycle.Matches.Add(match);

                // Auto-resolve matches containing BYE players
                if (match.Player2.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase))
                {
                    match.WinnerId = 1;
                }
                else if (match.Player1.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase))
                {
                    match.WinnerId = 2;
                }
            }
        }

        return cycle;
    }

    // ── Commit ───────────────────────────────────────────────────────

    public static bool CommitCurrentCycle(Tournament tournament)
    {
        var cycle = tournament.CurrentCycle;
        if (cycle is null || !cycle.IsCompleted) return false;

        foreach (var m in cycle.Matches)
        {
            bool p1Won = m.WinnerId == 1;
            m.Player1.RecordResult(p1Won,  m.Character1);
            m.Player2.RecordResult(!p1Won, m.Character2);

            // In Championship mode, eliminate the loser immediately
            if (tournament.Mode == TournamentMode.Championship)
            {
                var loser = p1Won ? m.Player2 : m.Player1;
                loser.IsEliminated = true;
            }
        }

        tournament.CurrentCycleIndex++;

        // Lazily create next cycle
        if (tournament.Mode == TournamentMode.Championship)
        {
            int activeCount = tournament.Players.Count(p => !p.IsEliminated);
            if (activeCount >= 2)
            {
                int nextNum = tournament.Cycles.Count + 1;
                tournament.Cycles.Add(BuildChampionshipCycle(tournament, nextNum));
            }
        }
        else
        {
            int nextNum = tournament.Cycles.Count + 1;
            tournament.Cycles.Add(BuildCycle(tournament, nextNum));
        }

        return true;
    }

    // ── Eliminate ────────────────────────────────────────────────────

    /// <summary>Marks a player as eliminated, pruning their unplayed matches from active round.</summary>
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
