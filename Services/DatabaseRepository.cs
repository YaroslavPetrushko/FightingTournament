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
}
