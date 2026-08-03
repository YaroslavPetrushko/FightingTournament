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

    [Fact]
    public void ReorderUnplayedMatches_PreservesCompletedMatches_AndReordersRemaining()
    {
        // Arrange: 4 players endless tournament starting in Classic mode
        var players = new List<string> { "P1", "P2", "P3", "P4" };
        var t = TournamentEngine.Create(players, TournamentMode.Endless);
        t.PairingMode = EndlessPairingMode.Sequential;
        var cycle = t.Cycles[0];

        // Mark 1st match as completed
        var match1 = cycle.Matches[0];
        match1.WinnerId = 1;

        // Act: Switch pairing mode to Mixed and re-order
        TournamentEngine.ReorderUnplayedMatches(t, cycle, EndlessPairingMode.Mixed);

        // Assert
        Assert.Equal(6, cycle.Matches.Count);
        Assert.True(cycle.Matches[0].IsCompleted);
        Assert.Equal(match1.Player1, cycle.Matches[0].Player1);
        Assert.Equal(match1.Player2, cycle.Matches[0].Player2);

        // Remaining 5 matches should be unplayed
        for (int i = 1; i < cycle.Matches.Count; i++)
        {
            Assert.False(cycle.Matches[i].IsCompleted);
        }
    }

    [Fact]
    public void EndlessMode_AccumulatesRoundNumbersMonotonically_AcrossCycles()
    {
        // Arrange: 4 players endless tournament starting in Mixed mode (3 sub-rounds per cycle)
        var players = new List<string> { "P1", "P2", "P3", "P4" };
        var t = TournamentEngine.Create(players, TournamentMode.Endless);
        t.PairingMode = EndlessPairingMode.Mixed;

        // Complete Cycle 1 matches
        foreach (var m in t.Cycles[0].Matches)
        {
            m.WinnerId = 1;
        }

        // Commit Cycle 1 -> Creates Cycle 2
        TournamentEngine.CommitCurrentCycle(t);

        // Instantiate ViewModel with Cycle 2 active
        var vm = new FightingTournament.ViewModels.TournamentViewModel(t, () => { });
        vm.SelectedCycleIndex = 1; // Cycle 2

        // Assert: First match in Cycle 2 should display Round 4 (3 from Cycle 1 + 1 from Cycle 2)
        var firstMatch = vm.CurrentMatches.First(m => m.ShowSubRoundHeader);
        Assert.Equal(4, firstMatch.DisplaySubRoundNumber);
        Assert.Equal("— Round 4 —", firstMatch.SubRoundHeaderText);
    }

    [Fact]
    public void EditHistoricalCycle_RecalculatesStats_IncludingActiveCycleCompletedMatches()
    {
        // Arrange: 4 players endless tournament
        var players = new List<string> { "P1", "P2", "P3", "P4" };
        var t = TournamentEngine.Create(players, TournamentMode.Endless);

        // Complete Cycle 1 matches (P1 wins all)
        foreach (var m in t.Cycles[0].Matches)
        {
            if (m.Player1.Name == "P1") m.WinnerId = 1;
            else if (m.Player2.Name == "P1") m.WinnerId = 2;
            else m.WinnerId = 1;
        }

        // Create ViewModel & commit Cycle 1
        var vm = new FightingTournament.ViewModels.TournamentViewModel(t, () => { });
        vm.CommitCycleCommand.Execute(null);

        // In Cycle 2 (active), mark 1 match as completed (P1 wins again)
        var cycle2Match = t.Cycles[1].Matches[0];
        cycle2Match.WinnerId = 1;
        cycle2Match.Player1.RecordResult(true, null);

        int p1WinsBeforeEdit = t.Players.First(p => p.Name == "P1").TotalWins;

        // Act: Navigate back to Cycle 1, edit a match, and click Save (CommitCycleCommand)
        vm.SelectedCycleIndex = 0; // Cycle 1
        vm.CommitCycleCommand.Execute(null);

        // Assert: P1's total wins should include both Cycle 1 wins and the completed match from Cycle 2
        int p1WinsAfterEdit = t.Players.First(p => p.Name == "P1").TotalWins;
        Assert.Equal(p1WinsBeforeEdit, p1WinsAfterEdit);
    }

    [Fact]
    public void IsPairingSelectorEnabled_DisabledWhenInspectingHistoricalCycles()
    {
        // Arrange
        var players = new List<string> { "P1", "P2", "P3", "P4" };
        var t = TournamentEngine.Create(players, TournamentMode.Endless);
        foreach (var m in t.Cycles[0].Matches) m.WinnerId = 1;
        TournamentEngine.CommitCurrentCycle(t);

        var vm = new FightingTournament.ViewModels.TournamentViewModel(t, () => { });

        // Act & Assert on active cycle
        vm.SelectedCycleIndex = 1;
        Assert.True(vm.IsPairingSelectorEnabled);

        // Act & Assert on historical cycle
        vm.SelectedCycleIndex = 0;
        Assert.False(vm.IsPairingSelectorEnabled);

        // Attempting to change pairing mode during historical cycle inspection should be ignored
        vm.IsPairingClassic = true;
        Assert.Equal(EndlessPairingMode.Mixed, t.PairingMode);
    }

    [Fact]
    public void Cycle_TracksPerCyclePairingMode_AccuratelyInViewModel()
    {
        // Arrange: Start Cycle 1 in Mixed mode
        var players = new List<string> { "P1", "P2", "P3", "P4" };
        var t = TournamentEngine.Create(players, TournamentMode.Endless);
        t.PairingMode = EndlessPairingMode.Mixed;
        foreach (var m in t.Cycles[0].Matches) m.WinnerId = 1;
        TournamentEngine.CommitCurrentCycle(t); // Cycle 2 generated

        var vm = new FightingTournament.ViewModels.TournamentViewModel(t, () => { });
        vm.SelectedCycleIndex = 1; // Cycle 2 active

        // Act: Switch Cycle 2 to Random mode
        vm.IsPairingRandom = true;

        // Assert: Cycle 2 is Random mode
        Assert.True(vm.IsPairingRandom);
        Assert.Equal(EndlessPairingMode.Random, t.Cycles[1].PairingMode);

        // Act: Switch selection to historical Cycle 1
        vm.SelectedCycleIndex = 0;

        // Assert: Cycle 1 reflects Mixed mode
        Assert.True(vm.IsPairingMixed);
        Assert.False(vm.IsPairingRandom);
        Assert.Equal(EndlessPairingMode.Mixed, t.Cycles[0].PairingMode);
    }
}
