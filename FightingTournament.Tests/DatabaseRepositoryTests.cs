using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FightingTournament.Models;
using FightingTournament.Services;
using Xunit;

namespace FightingTournament.Tests;

public class DatabaseRepositoryTests : IDisposable
{
    private readonly string _tempDbPath;

    public DatabaseRepositoryTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"TestDb_{Guid.NewGuid():N}.db");
        DatabaseConnector.Instance.ChangeDatabasePath(_tempDbPath);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempDbPath))
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                File.Delete(_tempDbPath);
            }
        }
        catch
        {
            // Ignore cleanup errors for temp file
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SaveAndLoadTournamentStateAsync_RoundTripsDataCorrectly()
    {
        // Arrange
        var playerNames = new List<string> { "Alice", "Bob" };
        var tournament = TournamentEngine.Create(playerNames, TournamentMode.Endless, defaultRounds: 3);
        tournament.SessionName = $"TestSession_{Guid.NewGuid():N}";

        // Complete match 1
        var match = tournament.Cycles[0].Matches[0];
        match.WinnerId = 1; // Alice wins
        match.Character1 = "Jin";
        match.Character2 = "Kazuya";

        // Commit cycle
        TournamentEngine.CommitCurrentCycle(tournament);

        // Act: Save asynchronously
        await DatabaseRepository.SaveTournamentStateAsync(tournament);

        // Act: Load asynchronously
        var loaded = await DatabaseRepository.LoadTournamentStateAsync(tournament.SessionName);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(tournament.SessionName, loaded.SessionName);
        Assert.Equal(TournamentMode.Endless, loaded.Mode);
        Assert.Equal(2, loaded.Players.Count);
        Assert.Equal(1, loaded.CurrentCycleIndex);

        var alice = loaded.Players.Find(p => p.Name == "Alice");
        Assert.NotNull(alice);
        Assert.Equal(1, alice.TotalWins);
        Assert.Equal(1, alice.TotalMatches);

        var sessions = await DatabaseRepository.GetSavedSessionsAsync();
        Assert.Contains(tournament.SessionName, sessions);

        var users = await DatabaseRepository.GetRegisteredUsersAsync();
        Assert.Contains("Alice", users);
        Assert.Contains("Bob", users);
    }

    [Fact]
    public async Task DeleteTournamentStateAsync_RemovesSessionFromDatabase()
    {
        // Arrange
        var playerNames = new List<string> { "Player1", "Player2" };
        var tournament = TournamentEngine.Create(playerNames, TournamentMode.Championship);
        tournament.SessionName = $"DeleteTest_{Guid.NewGuid():N}";
        await DatabaseRepository.SaveTournamentStateAsync(tournament);

        // Act
        await DatabaseRepository.DeleteTournamentStateAsync(tournament.SessionName);
        var loaded = await DatabaseRepository.LoadTournamentStateAsync(tournament.SessionName);
        var sessions = await DatabaseRepository.GetSavedSessionsAsync();

        // Assert
        Assert.Null(loaded);
        Assert.DoesNotContain(tournament.SessionName, sessions);
    }
}
