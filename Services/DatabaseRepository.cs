using FightingTournament.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace FightingTournament.Services;

public static class DatabaseRepository
{
    /// <summary>
    /// Deletes any existing tournament session with the same SessionName and
    /// saves the entire tournament state (players, picks, cycles, matches) in a single transaction.
    /// </summary>
    public static void SaveTournamentState(Tournament tournament)
    {
        if (string.IsNullOrWhiteSpace(tournament.SessionName))
            throw new ArgumentException("Session name cannot be empty when saving tournament state.", nameof(tournament));

        using var connection = DatabaseConnector.Instance.GetConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            // 1. Cleanly delete any existing session with the same name to prevent duplicates.
            // ON DELETE CASCADE takes care of child tables!
            using (var deleteCmd = connection.CreateCommand())
            {
                deleteCmd.Transaction = transaction;
                deleteCmd.CommandText = "DELETE FROM Tournaments WHERE SessionName = @SessionName";
                deleteCmd.Parameters.AddWithValue("@SessionName", tournament.SessionName);
                deleteCmd.ExecuteNonQuery();
            }

            // 2. Insert new Tournament record
            long tournamentId;
            using (var insertTournamentCmd = connection.CreateCommand())
            {
                insertTournamentCmd.Transaction = transaction;
                insertTournamentCmd.CommandText = @"
                    INSERT INTO Tournaments (SessionName, SelectedGame, CurrentCycleIndex, IsFinished)
                    VALUES (@SessionName, @SelectedGame, @CurrentCycleIndex, @IsFinished);
                    SELECT last_insert_rowid();";
                insertTournamentCmd.Parameters.AddWithValue("@SessionName", tournament.SessionName);
                insertTournamentCmd.Parameters.AddWithValue("@SelectedGame", tournament.SelectedGame);
                insertTournamentCmd.Parameters.AddWithValue("@CurrentCycleIndex", tournament.CurrentCycleIndex);
                insertTournamentCmd.Parameters.AddWithValue("@IsFinished", tournament.IsFinished ? 1 : 0);

                tournamentId = (long)insertTournamentCmd.ExecuteScalar()!;
            }

            // 3. Insert Players and their Character Picks
            var playerIds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var player in tournament.Players)
            {
                // Register in global Users directory (ignore duplicate inserts)
                using (var insertGlobalUserCmd = connection.CreateCommand())
                {
                    insertGlobalUserCmd.Transaction = transaction;
                    insertGlobalUserCmd.CommandText = "INSERT OR IGNORE INTO Users (Nickname) VALUES (@Nickname);";
                    insertGlobalUserCmd.Parameters.AddWithValue("@Nickname", player.Name);
                    insertGlobalUserCmd.ExecuteNonQuery();
                }

                long playerId;
                using (var insertPlayerCmd = connection.CreateCommand())
                {
                    insertPlayerCmd.Transaction = transaction;
                    insertPlayerCmd.CommandText = @"
                        INSERT INTO Players (TournamentId, Name, IsEliminated, TotalWins, TotalLosses, TotalMatches)
                        VALUES (@TournamentId, @Name, @IsEliminated, @TotalWins, @TotalLosses, @TotalMatches);
                        SELECT last_insert_rowid();";
                    insertPlayerCmd.Parameters.AddWithValue("@TournamentId", tournamentId);
                    insertPlayerCmd.Parameters.AddWithValue("@Name", player.Name);
                    insertPlayerCmd.Parameters.AddWithValue("@IsEliminated", player.IsEliminated ? 1 : 0);
                    insertPlayerCmd.Parameters.AddWithValue("@TotalWins", player.TotalWins);
                    insertPlayerCmd.Parameters.AddWithValue("@TotalLosses", player.TotalLosses);
                    insertPlayerCmd.Parameters.AddWithValue("@TotalMatches", player.TotalMatches);

                    playerId = (long)insertPlayerCmd.ExecuteScalar()!;
                    playerIds[player.Name] = playerId;
                }

                // Insert character picks for the player
                foreach (var pick in player.CharacterPicks)
                {
                    using var insertPickCmd = connection.CreateCommand();
                    insertPickCmd.Transaction = transaction;
                    insertPickCmd.CommandText = @"
                        INSERT INTO PlayerCharacterPicks (PlayerId, CharacterName, PickCount)
                        VALUES (@PlayerId, @CharacterName, @PickCount);";
                    insertPickCmd.Parameters.AddWithValue("@PlayerId", playerId);
                    insertPickCmd.Parameters.AddWithValue("@CharacterName", pick.Key);
                    insertPickCmd.Parameters.AddWithValue("@PickCount", pick.Value);
                    insertPickCmd.ExecuteNonQuery();
                }
            }

            // 4. Insert Cycles and their Matches
            foreach (var cycle in tournament.Cycles)
            {
                long cycleId;
                using (var insertCycleCmd = connection.CreateCommand())
                {
                    insertCycleCmd.Transaction = transaction;
                    insertCycleCmd.CommandText = @"
                        INSERT INTO Cycles (TournamentId, Number)
                        VALUES (@TournamentId, @Number);
                        SELECT last_insert_rowid();";
                    insertCycleCmd.Parameters.AddWithValue("@TournamentId", tournamentId);
                    insertCycleCmd.Parameters.AddWithValue("@Number", cycle.Number);

                    cycleId = (long)insertCycleCmd.ExecuteScalar()!;
                }

                // Insert matches within this cycle
                foreach (var match in cycle.Matches)
                {
                    if (!playerIds.TryGetValue(match.Player1.Name, out long p1Id) ||
                        !playerIds.TryGetValue(match.Player2.Name, out long p2Id))
                    {
                        throw new InvalidOperationException($"Cannot save match: player names do not exist in players list.");
                    }

                    using var insertMatchCmd = connection.CreateCommand();
                    insertMatchCmd.Transaction = transaction;
                    insertMatchCmd.CommandText = @"
                        INSERT INTO Matches (CycleId, Player1Id, Player2Id, WinnerId, Character1, Character2)
                        VALUES (@CycleId, @Player1Id, @Player2Id, @WinnerId, @Character1, @Character2);";
                    insertMatchCmd.Parameters.AddWithValue("@CycleId", cycleId);
                    insertMatchCmd.Parameters.AddWithValue("@Player1Id", p1Id);
                    insertMatchCmd.Parameters.AddWithValue("@Player2Id", p2Id);
                    insertMatchCmd.Parameters.AddWithValue("@WinnerId", (object?)match.WinnerId ?? DBNull.Value);
                    insertMatchCmd.Parameters.AddWithValue("@Character1", (object?)match.Character1 ?? DBNull.Value);
                    insertMatchCmd.Parameters.AddWithValue("@Character2", (object?)match.Character2 ?? DBNull.Value);
                    insertMatchCmd.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Gets all unique registered player nicknames in alphabetical order.
    /// </summary>
    public static List<string> GetRegisteredUsers()
    {
        var list = new List<string>();
        using var connection = DatabaseConnector.Instance.GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Nickname FROM Users ORDER BY Nickname";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(reader.GetString(0));
        }

        return list;
    }

    /// <summary>
    /// Gets all unique SessionName values stored in the Tournaments table.
    /// </summary>
    public static List<string> GetSavedSessions()
    {
        var list = new List<string>();
        using var connection = DatabaseConnector.Instance.GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT SessionName FROM Tournaments ORDER BY SessionName";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(reader.GetString(0));
        }

        return list;
    }

    /// <summary>
    /// Deletes a tournament session from the database based on SessionName.
    /// Cascades automatically delete players, character picks, cycles, and matches.
    /// </summary>
    public static void DeleteTournamentState(string sessionName)
    {
        if (string.IsNullOrWhiteSpace(sessionName)) return;

        using var connection = DatabaseConnector.Instance.GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Tournaments WHERE SessionName = @SessionName";
        command.Parameters.AddWithValue("@SessionName", sessionName);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Loads and hydrates a complete Tournament model from the database by its SessionName.
    /// Returns null if the session name does not exist.
    /// </summary>
    public static Tournament? LoadTournamentState(string sessionName)
    {
        using var connection = DatabaseConnector.Instance.GetConnection();
        connection.Open();

        // 1. Fetch parent tournament record
        long tournamentId;
        string selectedGame;
        int currentCycleIndex;
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, SelectedGame, CurrentCycleIndex FROM Tournaments WHERE SessionName = @SessionName";
            cmd.Parameters.AddWithValue("@SessionName", sessionName);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            tournamentId = reader.GetInt64(0);
            selectedGame = reader.GetString(1);
            currentCycleIndex = reader.GetInt32(2);
        }

        var tournament = new Tournament
        {
            SessionName = sessionName,
            SelectedGame = selectedGame,
            CurrentCycleIndex = currentCycleIndex
        };

        // 2. Fetch players and their character picks
        var playerMap = new Dictionary<long, Player>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, Name, IsEliminated, TotalWins, TotalLosses, TotalMatches FROM Players WHERE TournamentId = @TournamentId";
            cmd.Parameters.AddWithValue("@TournamentId", tournamentId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                long playerId = reader.GetInt64(0);
                string name = reader.GetString(1);
                bool isEliminated = reader.GetInt32(2) == 1;
                int wins = reader.GetInt32(3);
                int losses = reader.GetInt32(4);
                int matches = reader.GetInt32(5);

                var player = new Player
                {
                    Name = name,
                    IsEliminated = isEliminated
                };

                playerMap[playerId] = player;
                tournament.Players.Add(player);

                // Fetch character picks for this player
                var picks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                using (var picksCmd = connection.CreateCommand())
                {
                    picksCmd.CommandText = "SELECT CharacterName, PickCount FROM PlayerCharacterPicks WHERE PlayerId = @PlayerId";
                    picksCmd.Parameters.AddWithValue("@PlayerId", playerId);
                    using var picksReader = picksCmd.ExecuteReader();
                    while (picksReader.Read())
                    {
                        picks[picksReader.GetString(0)] = picksReader.GetInt32(1);
                    }
                }

                player.LoadPersistedStats(wins, losses, matches, picks);
            }
        }

        // 3. Fetch Cycles and their Matches
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, Number FROM Cycles WHERE TournamentId = @TournamentId ORDER BY Number";
            cmd.Parameters.AddWithValue("@TournamentId", tournamentId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                long cycleId = reader.GetInt64(0);
                int number = reader.GetInt32(1);

                var cycle = new Cycle(number);
                tournament.Cycles.Add(cycle);

                // Fetch matches in this cycle
                using (var matchCmd = connection.CreateCommand())
                {
                    matchCmd.CommandText = "SELECT Player1Id, Player2Id, WinnerId, Character1, Character2 FROM Matches WHERE CycleId = @CycleId";
                    matchCmd.Parameters.AddWithValue("@CycleId", cycleId);
                    using var matchReader = matchCmd.ExecuteReader();
                    while (matchReader.Read())
                    {
                        long p1Id = matchReader.GetInt64(0);
                        long p2Id = matchReader.GetInt64(1);
                        int? winnerId = matchReader.IsDBNull(2) ? null : matchReader.GetInt32(2);
                        string? char1 = matchReader.IsDBNull(3) ? null : matchReader.GetString(3);
                        string? char2 = matchReader.IsDBNull(4) ? null : matchReader.GetString(4);

                        if (playerMap.TryGetValue(p1Id, out var p1) && playerMap.TryGetValue(p2Id, out var p2))
                        {
                            var match = new Match(p1, p2)
                            {
                                WinnerId = winnerId,
                                Character1 = char1,
                                Character2 = char2
                            };
                            cycle.Matches.Add(match);
                        }
                    }
                }
            }
        }

        return tournament;
    }
}
