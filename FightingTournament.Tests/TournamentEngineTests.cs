using System;
using System.Collections.Generic;
using System.Linq;
using FightingTournament.Models;
using FightingTournament.Services;
using Xunit;

namespace FightingTournament.Tests;

public class TournamentEngineTests
{
    [Fact]
    public void Create_InChampionshipMode_PadsToNextPowerOfTwo_AndAutoResolvesByes()
    {
        // Arrange
        var playerNames = new List<string> { "Ryu", "Ken", "Guile" };

        // Act
        var t = TournamentEngine.Create(playerNames, TournamentMode.Championship);

        // Assert
        Assert.Equal(TournamentMode.Championship, t.Mode);
        // Next power of 2 for 3 is 4, so 1 BYE player is added
        Assert.Equal(4, t.Players.Count);
        Assert.Equal(1, t.Players.Count(p => p.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase)));

        // Verify Round 1 matches
        Assert.Single(t.Cycles);
        var round1 = t.Cycles[0];
        Assert.Equal(2, round1.Matches.Count);

        // Match 1: Ryu vs BYE (should be auto-resolved with Ryu as winner)
        var match1 = round1.Matches[0];
        Assert.Equal("Ryu", match1.Player1.Name);
        Assert.Equal("BYE", match1.Player2.Name);
        Assert.Equal(1, match1.WinnerId); // Player 1 wins
        Assert.True(match1.IsCompleted);

        // Match 2: Ken vs Guile (should NOT be auto-resolved)
        var match2 = round1.Matches[1];
        Assert.Equal("Ken", match1.Player2.Name.Equals("BYE") ? match2.Player1.Name : match1.Player2.Name);
        Assert.Null(match2.WinnerId);
        Assert.False(match2.IsCompleted);
    }

    [Fact]
    public void Create_InEndlessMode_GeneratesRoundRobinPairings()
    {
        // Arrange
        var playerNames = new List<string> { "Ryu", "Ken", "Guile", "Chun-Li" };

        // Act
        var t = TournamentEngine.Create(playerNames, TournamentMode.Endless);

        // Assert
        Assert.Equal(TournamentMode.Endless, t.Mode);
        Assert.Equal(4, t.Players.Count);
        Assert.Single(t.Cycles);

        var round1 = t.Cycles[0];
        // 4 players round-robin should have 4 * 3 / 2 = 6 matches
        Assert.Equal(6, round1.Matches.Count);

        // Verify all matches are unique pairings
        var pairings = round1.Matches.Select(m => $"{m.Player1.Name} vs {m.Player2.Name}").ToList();
        var uniquePairings = pairings.Distinct().ToList();
        Assert.Equal(pairings.Count, uniquePairings.Count);
    }

    [Fact]
    public void CommitCurrentCycle_AdvancesCycleAndEliminatesLosers_InChampionshipMode()
    {
        // Arrange
        var playerNames = new List<string> { "Ryu", "Ken", "Guile", "Chun-Li" };
        var t = TournamentEngine.Create(playerNames, TournamentMode.Championship);
        var round1 = t.Cycles[0];

        // Resolve matches
        // Match 1: Ryu vs Chun-Li -> Ryu wins
        round1.Matches[0].WinnerId = 1;
        // Match 2: Ken vs Guile -> Ken wins
        round1.Matches[1].WinnerId = 1;

        // Act
        bool result = TournamentEngine.CommitCurrentCycle(t);

        // Assert
        Assert.True(result);
        Assert.Equal(1, t.CurrentCycleIndex);

        // Chun-Li and Guile should be eliminated
        Assert.True(t.Players.First(p => p.Name == "Chun-Li").IsEliminated);
        Assert.True(t.Players.First(p => p.Name == "Guile").IsEliminated);
        Assert.False(t.Players.First(p => p.Name == "Ryu").IsEliminated);
        Assert.False(t.Players.First(p => p.Name == "Ken").IsEliminated);

        // A new cycle should be generated with 1 match: Ryu vs Ken
        Assert.Equal(2, t.Cycles.Count);
        var round2 = t.Cycles[1];
        Assert.Single(round2.Matches);
        var match = round2.Matches[0];
        Assert.Equal("Ryu", match.Player1.Name);
        Assert.Equal("Ken", match.Player2.Name);
    }

    [Fact]
    public void CommitCurrentCycle_RecordsStatsCorrectly_ForNonByePlayers()
    {
        // Arrange
        var playerNames = new List<string> { "Ryu", "Ken" };
        var t = TournamentEngine.Create(playerNames, TournamentMode.Endless);
        var round1 = t.Cycles[0];

        var match = round1.Matches[0];
        match.WinnerId = 1; // Player 1 (Ryu) wins
        match.Character1 = "Ryu";
        match.Character2 = "Ken";

        // Act
        bool result = TournamentEngine.CommitCurrentCycle(t);

        // Assert
        Assert.True(result);
        var ryu = t.Players.First(p => p.Name == "Ryu");
        var ken = t.Players.First(p => p.Name == "Ken");

        Assert.Equal(1, ryu.TotalMatches);
        Assert.Equal(1, ryu.TotalWins);
        Assert.Equal(0, ryu.TotalLosses);
        Assert.Equal("Ryu", ryu.MostPickedCharacter);

        Assert.Equal(1, ken.TotalMatches);
        Assert.Equal(0, ken.TotalWins);
        Assert.Equal(1, ken.TotalLosses);
        Assert.Equal("Ken", ken.MostPickedCharacter);
    }

    [Fact]
    public void EliminatePlayer_PrunesUnplayedMatches()
    {
        // Arrange
        var playerNames = new List<string> { "Ryu", "Ken", "Guile" };
        var t = TournamentEngine.Create(playerNames, TournamentMode.Endless);
        var round1 = t.Cycles[0];

        // 3 players round-robin:
        // Match 1: Ryu vs Ken
        // Match 2: Ryu vs Guile
        // Match 3: Ken vs Guile
        Assert.Equal(3, round1.Matches.Count);

        var ryu = t.Players.First(p => p.Name == "Ryu");
        var ken = t.Players.First(p => p.Name == "Ken");
        var guile = t.Players.First(p => p.Name == "Guile");

        // Complete Ryu vs Ken -> Ryu wins
        var ryuVsKen = round1.Matches.First(m => (m.Player1 == ryu && m.Player2 == ken) || (m.Player1 == ken && m.Player2 == ryu));
        ryuVsKen.WinnerId = ryuVsKen.Player1 == ryu ? 1 : 2;

        // Act
        TournamentEngine.EliminatePlayer(t, ken);

        // Assert
        Assert.True(ken.IsEliminated);
        // The completed match (Ryu vs Ken) should remain
        Assert.Contains(ryuVsKen, round1.Matches);
        // The uncompleted match involving Ken (Ken vs Guile) should be pruned
        var kenVsGuile = round1.Matches.FirstOrDefault(m => (m.Player1 == ken && m.Player2 == guile) || (m.Player1 == guile && m.Player2 == ken));
        Assert.Null(kenVsGuile);
        // The uncompleted match not involving Ken (Ryu vs Guile) should remain
        var ryuVsGuile = round1.Matches.FirstOrDefault(m => (m.Player1 == ryu && m.Player2 == guile) || (m.Player1 == guile && m.Player2 == ryu));
        Assert.NotNull(ryuVsGuile);
    }

    [Fact]
    public void BuildChampionshipCycle_WithOddWinnersCount_PadsWithByeAndAdvancesOrphanedWinner()
    {
        // Arrange: 3 players in Championship mode
        var t = TournamentEngine.Create(new List<string> { "Ryu", "Ken", "Guile" }, TournamentMode.Championship);
        var r1 = t.Cycles[0];

        // Match 0 is Ryu vs BYE (auto-resolved with Ryu = WinnerId 1)
        // Match 1 is Ken vs Guile -> mark Ken as winner
        var match2 = r1.Matches[1];
        match2.WinnerId = match2.Player1.Name == "Ken" ? 1 : 2;

        // Act
        bool committed = TournamentEngine.CommitCurrentCycle(t);

        // Assert
        Assert.True(committed);
        Assert.Equal(2, t.Cycles.Count);

        var r2 = t.Cycles[1];
        Assert.Single(r2.Matches);

        var finalsMatch = r2.Matches[0];
        var finalPlayers = new List<string> { finalsMatch.Player1.Name, finalsMatch.Player2.Name };
        Assert.Contains("Ryu", finalPlayers);
        Assert.Contains("Ken", finalPlayers);
    }

    [Fact]
    public void ChampionshipMode_IsFinished_EvaluatesFalseBeforeCycleCommitted()
    {
        // Arrange
        var t = TournamentEngine.Create(new List<string> { "SoloPlayer" }, TournamentMode.Championship);

        // Assert: Before committing current cycle, IsFinished should be false
        Assert.False(t.IsFinished);

        // Act: Commit the round containing the BYE match
        TournamentEngine.CommitCurrentCycle(t);

        // Assert: After committing cycle 0, tournament should now be finished
        Assert.True(t.IsFinished);
    }

    [Fact]
    public void BuildCycle_InEndlessMode_WithMixedPairing_GeneratesAllUniquePairingsAndSubRounds_EvenPlayerCount()
    {
        // Arrange: 4 players -> 6 unique pairings, 3 sub-rounds (2 matches per sub-round)
        var players = new List<string> { "P1", "P2", "P3", "P4" };

        // Act
        var t = TournamentEngine.Create(players, TournamentMode.Endless);
        t.PairingMode = EndlessPairingMode.Mixed;
        var cycle = t.Cycles[0];

        // Assert
        Assert.Equal(6, cycle.Matches.Count);

        // Verify sub-round distribution
        var subRounds = cycle.Matches.Select(m => m.SubRound).Distinct().OrderBy(r => r).ToList();
        Assert.Equal(new List<int> { 1, 2, 3 }, subRounds);

        // Verify all 6 unique pairs exist
        var pairs = new HashSet<string>();
        foreach (var m in cycle.Matches)
        {
            var names = new List<string> { m.Player1.Name, m.Player2.Name };
            names.Sort();
            pairs.Add($"{names[0]}-{names[1]}");
        }
        Assert.Equal(6, pairs.Count);
        Assert.Contains("P1-P2", pairs);
        Assert.Contains("P1-P3", pairs);
        Assert.Contains("P1-P4", pairs);
        Assert.Contains("P2-P3", pairs);
        Assert.Contains("P2-P4", pairs);
        Assert.Contains("P3-P4", pairs);
    }

    [Fact]
    public void BuildCycle_InEndlessMode_WithMixedPairing_GeneratesAllUniquePairingsAndSubRounds_OddPlayerCount()
    {
        // Arrange: 5 players -> 10 unique pairings, 5 sub-rounds (2 matches per sub-round)
        var players = new List<string> { "P1", "P2", "P3", "P4", "P5" };

        // Act
        var t = TournamentEngine.Create(players, TournamentMode.Endless);
        t.PairingMode = EndlessPairingMode.Mixed;
        var cycle = t.Cycles[0];

        // Assert
        Assert.Equal(10, cycle.Matches.Count);

        // Verify sub-round distribution
        var subRounds = cycle.Matches.Select(m => m.SubRound).Distinct().OrderBy(r => r).ToList();
        Assert.Equal(new List<int> { 1, 2, 3, 4, 5 }, subRounds);

        // Verify no player plays more than 1 match per sub-round
        foreach (int subRound in subRounds)
        {
            var roundMatches = cycle.Matches.Where(m => m.SubRound == subRound).ToList();
            var playersInRound = roundMatches.SelectMany(m => new[] { m.Player1.Name, m.Player2.Name }).ToList();
            Assert.Equal(playersInRound.Count, playersInRound.Distinct().Count());
        }

        // Verify all 10 unique pairs exist
        var pairs = new HashSet<string>();
        foreach (var m in cycle.Matches)
        {
            var names = new List<string> { m.Player1.Name, m.Player2.Name };
            names.Sort();
            pairs.Add($"{names[0]}-{names[1]}");
        }
        Assert.Equal(10, pairs.Count);
    }

    [Fact]
    public void EliminatePlayer_NormalizesSubRoundNumbers_WhenMatchesArePruned()
    {
        // Arrange: 3 players -> sub-rounds 1, 2, 3
        var players = new List<string> { "P1", "P2", "P3" };
        var t = TournamentEngine.Create(players, TournamentMode.Endless);
        t.PairingMode = EndlessPairingMode.Mixed;
        var cycle = t.Cycles[0];

        // Player P3 is eliminated
        var p3 = t.Players.First(p => p.Name == "P3");

        // Act
        TournamentEngine.EliminatePlayer(t, p3);

        // Assert: Only 1 match (P1 vs P2) remains, and its SubRound must be normalized to 1 (not 3)
        Assert.Single(cycle.Matches);
        Assert.Equal(1, cycle.Matches[0].SubRound);
    }
}
