using FightingTournament.Models;
using System.Collections.Generic;
using Xunit;

namespace FightingTournament.Tests;

public class PlayerTests
{
    [Fact]
    public void RecordResult_UpdatesStatsAndWinRate_OnWin()
    {
        // Arrange
        var player = new Player { Name = "Ryu" };

        // Act
        player.RecordResult(won: true, character: "Ryu");

        // Assert
        Assert.Equal(1, player.TotalMatches);
        Assert.Equal(1, player.TotalWins);
        Assert.Equal(0, player.TotalLosses);
        Assert.Equal(100.0, player.WinRate);
    }

    [Fact]
    public void RecordResult_UpdatesStatsAndWinRate_OnLoss()
    {
        // Arrange
        var player = new Player { Name = "Ken" };

        // Act
        player.RecordResult(won: false, character: "Ken");

        // Assert
        Assert.Equal(1, player.TotalMatches);
        Assert.Equal(0, player.TotalWins);
        Assert.Equal(1, player.TotalLosses);
        Assert.Equal(0.0, player.WinRate);
    }

    [Fact]
    public void RecordResult_WinRateIsZero_WhenNoMatchesPlayed()
    {
        // Arrange
        var player = new Player { Name = "Guile" };

        // Assert
        Assert.Equal(0, player.TotalMatches);
        Assert.Equal(0.0, player.WinRate);
    }

    [Fact]
    public void RecordResult_ParsesTeamCharactersAndWhitespaceCorrectly()
    {
        // Arrange
        var player = new Player { Name = "Chun-Li" };

        // Act
        player.RecordResult(won: true, character: " Chun-Li ; Ken / Ryu \\ Akuma ");

        // Assert
        Assert.Equal(4, player.CharacterPicks.Count);
        Assert.True(player.CharacterPicks.ContainsKey("Chun-Li"));
        Assert.True(player.CharacterPicks.ContainsKey("Ken"));
        Assert.True(player.CharacterPicks.ContainsKey("Ryu"));
        Assert.True(player.CharacterPicks.ContainsKey("Akuma"));
        Assert.Equal(1, player.CharacterPicks["Chun-Li"]);
        Assert.Equal(1, player.CharacterPicks["Ken"]);
        Assert.Equal(1, player.CharacterPicks["Ryu"]);
        Assert.Equal(1, player.CharacterPicks["Akuma"]);
    }

    [Fact]
    public void MostPickedCharacter_ResolvesHighestValue()
    {
        // Arrange
        var player = new Player { Name = "Zangief" };

        // Act
        player.RecordResult(won: true, character: "Zangief");
        player.RecordResult(won: true, character: "Zangief");
        player.RecordResult(won: false, character: "Ryu");

        // Assert
        Assert.Equal("Zangief", player.MostPickedCharacter);
    }

    [Fact]
    public void MostPickedCharacter_ReturnsDash_WhenNoPicks()
    {
        // Arrange
        var player = new Player { Name = "Dhalsim" };

        // Assert
        Assert.Equal("—", player.MostPickedCharacter);
    }

    [Fact]
    public void Reset_ClearsAllStatsAndPicks()
    {
        // Arrange
        var player = new Player { Name = "M. Bison" };
        player.RecordResult(won: true, character: "Vega");
        player.IsEliminated = true;

        // Act
        player.Reset();

        // Assert
        Assert.Equal(0, player.TotalWins);
        Assert.Equal(0, player.TotalLosses);
        Assert.Equal(0, player.TotalMatches);
        Assert.Equal(0.0, player.WinRate);
        Assert.False(player.IsEliminated);
        Assert.Empty(player.CharacterPicks);
        Assert.Equal("—", player.MostPickedCharacter);
    }

    [Fact]
    public void LoadPersistedStats_CorrectlyLoadsData()
    {
        // Arrange
        var player = new Player { Name = "Sagat" };
        var picks = new Dictionary<string, int>
        {
            { "Sagat", 5 },
            { "Adon", 2 }
        };

        // Act
        player.LoadPersistedStats(wins: 5, losses: 2, matches: 7, picks: picks);

        // Assert
        Assert.Equal(7, player.TotalMatches);
        Assert.Equal(5, player.TotalWins);
        Assert.Equal(2, player.TotalLosses);
        Assert.Equal(5.0 / 7.0 * 100.0, player.WinRate);
        Assert.Equal(2, player.CharacterPicks.Count);
        Assert.Equal(5, player.CharacterPicks["Sagat"]);
        Assert.Equal(2, player.CharacterPicks["Adon"]);
        Assert.Equal("Sagat", player.MostPickedCharacter);
    }
}
