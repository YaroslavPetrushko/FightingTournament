using System;
using System.Collections.Generic;
using System.Linq;
using FightingTournament.Models;

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

    // ── Cycle builders ────────────────────────────────────────────────

    /// <summary>Generates ALL unique pairs from currently-active players (round-robin).</summary>
    private static Cycle BuildCycle(Tournament t, int number)
    {
        return t.PairingMode switch
        {
            EndlessPairingMode.Mixed => BuildCycleMixed(t, number),
            EndlessPairingMode.Random => BuildCycleRandom(t, number),
            _ => BuildCycleSequential(t, number)
        };
    }

    private static Cycle BuildCycleSequential(Tournament t, int number)
    {
        var cycle = new Cycle(number);
        var active = t.Players.Where(p => !p.IsEliminated).ToList();

        for (int i = 0; i < active.Count; i++)
            for (int j = i + 1; j < active.Count; j++)
                cycle.Matches.Add(new Match(active[i], active[j]) { Rounds = t.DefaultRounds, SubRound = 1 });

        return cycle;
    }

    private static Cycle BuildCycleRandom(Tournament t, int number)
    {
        var cycle = BuildCycleSequential(t, number);
        var rng = new Random();
        var shuffled = cycle.Matches.OrderBy(_ => rng.Next()).ToList();
        cycle.Matches.Clear();
        cycle.Matches.AddRange(shuffled);
        return cycle;
    }

    /// <summary>
    /// Generates ALL unique pairs using the Berger Circle Scheduling algorithm,
    /// grouping matches into sub-rounds so players receive optimal rest and rotation.
    /// </summary>
    private static Cycle BuildCycleMixed(Tournament t, int number)
    {
        var cycle = new Cycle(number);
        var active = t.Players.Where(p => !p.IsEliminated).ToList();
        if (active.Count < 2) return cycle;

        var players = new List<Player>(active);

        // If player count is odd, pad with a dummy BYE player for even rotation math
        bool hasBye = players.Count % 2 != 0;
        Player? dummyBye = null;
        if (hasBye)
        {
            dummyBye = new Player { Name = "BYE" };
            players.Add(dummyBye);
        }

        int totalPlayers = players.Count;
        int subRoundsCount = totalPlayers - 1;
        int half = totalPlayers / 2;

        for (int roundIndex = 0; roundIndex < subRoundsCount; roundIndex++)
        {
            int subRoundNumber = roundIndex + 1;

            for (int i = 0; i < half; i++)
            {
                Player p1 = players[i];
                Player p2 = players[players.Count - 1 - i];

                if (p1 != dummyBye && p2 != dummyBye)
                {
                    var match = new Match(p1, p2)
                    {
                        Rounds = t.DefaultRounds,
                        SubRound = subRoundNumber
                    };
                    cycle.Matches.Add(match);
                }
            }

            // Rotate elements: keep index 0 fixed, move last element to index 1
            Player last = players[players.Count - 1];
            players.RemoveAt(players.Count - 1);
            players.Insert(1, last);
        }

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
            else
            {
                // Odd remaining winner gets an auto-resolved BYE match to advance
                var byePlayer = new Player { Name = "BYE" };
                t.Players.Add(byePlayer);
                var match = new Match(winners[i], byePlayer) { Rounds = t.DefaultRounds, WinnerId = 1 };
                cycle.Matches.Add(match);
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

            if (!m.Player1.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase) &&
                !m.Player2.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase))
            {
                m.Player1.RecordResult(p1Won, m.Character1);
                m.Player2.RecordResult(!p1Won, m.Character2);
            }

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

        NormalizeSubRounds(cycle);
    }

    /// <summary>
    /// Re-indexes SubRound numbers on remaining matches in a cycle so they form a contiguous 1..K sequence.
    /// </summary>
    public static void NormalizeSubRounds(Cycle cycle)
    {
        if (cycle is null || cycle.Matches.Count == 0) return;

        var distinctSubRounds = cycle.Matches
            .Select(m => m.SubRound)
            .Distinct()
            .OrderBy(r => r)
            .ToList();

        var subRoundMap = new Dictionary<int, int>();
        for (int i = 0; i < distinctSubRounds.Count; i++)
        {
            subRoundMap[distinctSubRounds[i]] = i + 1;
        }

        foreach (var m in cycle.Matches)
        {
            if (subRoundMap.TryGetValue(m.SubRound, out int newSubRound))
            {
                m.SubRound = newSubRound;
            }
        }
    }
}
