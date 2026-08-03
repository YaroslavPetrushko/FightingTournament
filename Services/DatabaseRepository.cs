using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FightingTournament.Models;
using Microsoft.Data.Sqlite;

namespace FightingTournament.Services;

public static class DatabaseRepository
{
    /// <summary>
    /// Deletes any existing tournament session with the same SessionName and
    /// saves the entire tournament state (players, picks, cycles, matches) in a single transaction asynchronously.
    /// </summary>
    public static async Task SaveTournamentStateAsync(Tournament tournament)
    {
        if (string.IsNullOrWhiteSpace(tournament.SessionName))
            throw new ArgumentException("Session name cannot be empty when saving tournament state.", nameof(tournament));

        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            // 1. Cleanly delete any existing session with the same name to prevent duplicates.
            using (var deleteCmd = connection.CreateCommand())
            {
                deleteCmd.Transaction = transaction;
                deleteCmd.CommandText = "DELETE FROM Tournaments WHERE SessionName = @SessionName";
                deleteCmd.Parameters.AddWithValue("@SessionName", tournament.SessionName);
                await deleteCmd.ExecuteNonQueryAsync();
            }

            // 2. Insert new Tournament record
            long tournamentId;
            using (var insertTournamentCmd = connection.CreateCommand())
            {
                insertTournamentCmd.Transaction = transaction;
                insertTournamentCmd.CommandText = @"
                    INSERT INTO Tournaments (SessionName, SelectedGame, CurrentCycleIndex, IsFinished, TournamentMode, DefaultRounds, PairingMode)
                    VALUES (@SessionName, @SelectedGame, @CurrentCycleIndex, @IsFinished, @TournamentMode, @DefaultRounds, @PairingMode);
                    SELECT last_insert_rowid();";
                insertTournamentCmd.Parameters.AddWithValue("@SessionName", tournament.SessionName);
                insertTournamentCmd.Parameters.AddWithValue("@SelectedGame", tournament.SelectedGame);
                insertTournamentCmd.Parameters.AddWithValue("@CurrentCycleIndex", tournament.CurrentCycleIndex);
                insertTournamentCmd.Parameters.AddWithValue("@IsFinished", tournament.IsFinished ? 1 : 0);
                insertTournamentCmd.Parameters.AddWithValue("@TournamentMode", tournament.Mode.ToString());
                insertTournamentCmd.Parameters.AddWithValue("@DefaultRounds", tournament.DefaultRounds);
                insertTournamentCmd.Parameters.AddWithValue("@PairingMode", tournament.PairingMode.ToString());

                var scalarResult = await insertTournamentCmd.ExecuteScalarAsync();
                tournamentId = (long)scalarResult!;
            }

            // 3. Insert Players and their Character Picks
            var playerIds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var player in tournament.Players)
            {
                // Register in global Users directory (ignore duplicate inserts, skip virtual BYE players)
                if (!player.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase))
                {
                    using (var insertGlobalUserCmd = connection.CreateCommand())
                    {
                        insertGlobalUserCmd.Transaction = transaction;
                        insertGlobalUserCmd.CommandText = "INSERT OR IGNORE INTO Users (Nickname) VALUES (@Nickname);";
                        insertGlobalUserCmd.Parameters.AddWithValue("@Nickname", player.Name);
                        await insertGlobalUserCmd.ExecuteNonQueryAsync();
                    }
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

                    var scalarResult = await insertPlayerCmd.ExecuteScalarAsync();
                    playerId = (long)scalarResult!;
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
                    await insertPickCmd.ExecuteNonQueryAsync();
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
                        INSERT INTO Cycles (TournamentId, Number, PairingMode)
                        VALUES (@TournamentId, @Number, @PairingMode);
                        SELECT last_insert_rowid();";
                    insertCycleCmd.Parameters.AddWithValue("@TournamentId", tournamentId);
                    insertCycleCmd.Parameters.AddWithValue("@Number", cycle.Number);
                    insertCycleCmd.Parameters.AddWithValue("@PairingMode", cycle.PairingMode.ToString());

                    var scalarResult = await insertCycleCmd.ExecuteScalarAsync();
                    cycleId = (long)scalarResult!;
                }

                // Insert matches within this cycle
                foreach (var match in cycle.Matches)
                {
                    if (!playerIds.TryGetValue(match.Player1.Name, out long p1Id) ||
                        !playerIds.TryGetValue(match.Player2.Name, out long p2Id))
                    {
                        throw new InvalidOperationException("Cannot save match: player names do not exist in players list.");
                    }

                    using var insertMatchCmd = connection.CreateCommand();
                    insertMatchCmd.Transaction = transaction;
                    insertMatchCmd.CommandText = @"
                        INSERT INTO Matches (CycleId, Player1Id, Player2Id, WinnerId, Character1, Character2, Rounds, SubRound)
                        VALUES (@CycleId, @Player1Id, @Player2Id, @WinnerId, @Character1, @Character2, @Rounds, @SubRound);";
                    insertMatchCmd.Parameters.AddWithValue("@CycleId", cycleId);
                    insertMatchCmd.Parameters.AddWithValue("@Player1Id", p1Id);
                    insertMatchCmd.Parameters.AddWithValue("@Player2Id", p2Id);
                    insertMatchCmd.Parameters.AddWithValue("@WinnerId", (object?)match.WinnerId ?? DBNull.Value);
                    insertMatchCmd.Parameters.AddWithValue("@Character1", (object?)match.Character1 ?? DBNull.Value);
                    insertMatchCmd.Parameters.AddWithValue("@Character2", (object?)match.Character2 ?? DBNull.Value);
                    insertMatchCmd.Parameters.AddWithValue("@Rounds", match.Rounds);
                    insertMatchCmd.Parameters.AddWithValue("@SubRound", match.SubRound);
                    await insertMatchCmd.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static void SaveTournamentState(Tournament tournament) =>
        SaveTournamentStateAsync(tournament).GetAwaiter().GetResult();

    /// <summary>
    /// Gets all unique registered player nicknames in alphabetical order asynchronously.
    /// </summary>
    public static async Task<List<string>> GetRegisteredUsersAsync()
    {
        var list = new List<string>();
        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Nickname FROM Users ORDER BY Nickname";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }

        return list;
    }

    public static List<string> GetRegisteredUsers() =>
        GetRegisteredUsersAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Deletes a registered user from the global Users table asynchronously.
    /// </summary>
    public static async Task DeleteRegisteredUserAsync(string nickname)
    {
        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Users WHERE Nickname = @Nickname";
        command.Parameters.AddWithValue("@Nickname", nickname);
        await command.ExecuteNonQueryAsync();
    }

    public static void DeleteRegisteredUser(string nickname) =>
        DeleteRegisteredUserAsync(nickname).GetAwaiter().GetResult();

    /// <summary>
    /// Gets all unique SessionName values stored in the Tournaments table asynchronously.
    /// </summary>
    public static async Task<List<string>> GetSavedSessionsAsync()
    {
        var list = new List<string>();
        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT SessionName FROM Tournaments ORDER BY SessionName";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }

        return list;
    }

    public static List<string> GetSavedSessions() =>
        GetSavedSessionsAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Deletes a tournament session from the database based on SessionName asynchronously.
    /// </summary>
    public static async Task DeleteTournamentStateAsync(string sessionName)
    {
        if (string.IsNullOrWhiteSpace(sessionName)) return;

        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Tournaments WHERE SessionName = @SessionName";
        command.Parameters.AddWithValue("@SessionName", sessionName);
        await command.ExecuteNonQueryAsync();
    }

    public static void DeleteTournamentState(string sessionName) =>
        DeleteTournamentStateAsync(sessionName).GetAwaiter().GetResult();

    /// <summary>
    /// Loads and hydrates a complete Tournament model from the database by its SessionName asynchronously.
    /// </summary>
    public static async Task<Tournament?> LoadTournamentStateAsync(string sessionName)
    {
        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        // 1. Fetch parent tournament record
        long tournamentId;
        string selectedGame;
        int currentCycleIndex;
        int defaultRounds = 3;
        TournamentMode tournamentMode = TournamentMode.Endless;
        EndlessPairingMode pairingMode = EndlessPairingMode.Mixed;
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, SelectedGame, CurrentCycleIndex, TournamentMode, DefaultRounds, PairingMode FROM Tournaments WHERE SessionName = @SessionName";
            cmd.Parameters.AddWithValue("@SessionName", sessionName);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            tournamentId = reader.GetInt64(0);
            selectedGame = reader.GetString(1);
            currentCycleIndex = reader.GetInt32(2);

            // Backward compatibility
            string modeStr = "Endless";
            if (reader.FieldCount > 3 && !reader.IsDBNull(3))
            {
                modeStr = reader.GetString(3);
            }
            if (Enum.TryParse<TournamentMode>(modeStr, out var parsedMode))
            {
                tournamentMode = parsedMode;
            }

            if (reader.FieldCount > 4 && !reader.IsDBNull(4))
            {
                defaultRounds = reader.GetInt32(4);
            }

            if (reader.FieldCount > 5 && !reader.IsDBNull(5))
            {
                string pairingStr = reader.GetString(5);
                if (Enum.TryParse<EndlessPairingMode>(pairingStr, out var parsedPairing))
                {
                    pairingMode = parsedPairing;
                }
            }
        }

        var tournament = new Tournament
        {
            SessionName = sessionName,
            SelectedGame = selectedGame,
            CurrentCycleIndex = currentCycleIndex,
            Mode = tournamentMode,
            PairingMode = pairingMode,
            DefaultRounds = defaultRounds
        };

        // 2. Fetch players and their character picks
        var playerMap = new Dictionary<long, Player>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, Name, IsEliminated, TotalWins, TotalLosses, TotalMatches FROM Players WHERE TournamentId = @TournamentId";
            cmd.Parameters.AddWithValue("@TournamentId", tournamentId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
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
                    using var picksReader = await picksCmd.ExecuteReaderAsync();
                    while (await picksReader.ReadAsync())
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
            cmd.CommandText = "SELECT Id, Number, PairingMode FROM Cycles WHERE TournamentId = @TournamentId ORDER BY Number";
            cmd.Parameters.AddWithValue("@TournamentId", tournamentId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                long cycleId = reader.GetInt64(0);
                int number = reader.GetInt32(1);
                EndlessPairingMode cyclePairing = tournament.PairingMode;
                if (reader.FieldCount > 2 && !reader.IsDBNull(2))
                {
                    string pairingStr = reader.GetString(2);
                    if (Enum.TryParse<EndlessPairingMode>(pairingStr, out var parsedPairing))
                    {
                        cyclePairing = parsedPairing;
                    }
                }

                var cycle = new Cycle(number) { PairingMode = cyclePairing };
                tournament.Cycles.Add(cycle);

                // Fetch matches in this cycle
                using (var matchCmd = connection.CreateCommand())
                {
                    matchCmd.CommandText = "SELECT Player1Id, Player2Id, WinnerId, Character1, Character2, Rounds, SubRound FROM Matches WHERE CycleId = @CycleId";
                    matchCmd.Parameters.AddWithValue("@CycleId", cycleId);
                    using var matchReader = await matchCmd.ExecuteReaderAsync();
                    while (await matchReader.ReadAsync())
                    {
                        long p1Id = matchReader.GetInt64(0);
                        long p2Id = matchReader.GetInt64(1);
                        int? winnerId = matchReader.IsDBNull(2) ? null : matchReader.GetInt32(2);
                        string? char1 = matchReader.IsDBNull(3) ? null : matchReader.GetString(3);
                        string? char2 = matchReader.IsDBNull(4) ? null : matchReader.GetString(4);
                        int rounds = 3;
                        if (matchReader.FieldCount > 5 && !matchReader.IsDBNull(5))
                        {
                            rounds = matchReader.GetInt32(5);
                        }
                        int subRound = 1;
                        if (matchReader.FieldCount > 6 && !matchReader.IsDBNull(6))
                        {
                            subRound = matchReader.GetInt32(6);
                        }

                        if (playerMap.TryGetValue(p1Id, out var p1) && playerMap.TryGetValue(p2Id, out var p2))
                        {
                            var match = new Match(p1, p2)
                            {
                                WinnerId = winnerId,
                                Character1 = char1,
                                Character2 = char2,
                                Rounds = rounds,
                                SubRound = subRound
                            };
                            cycle.Matches.Add(match);
                        }
                    }
                }
            }
        }

        return tournament;
    }

    public static Tournament? LoadTournamentState(string sessionName) =>
        LoadTournamentStateAsync(sessionName).GetAwaiter().GetResult();

    // ── Custom Games ──────────────────────────────────────────────────

    public static async Task<List<string>> GetCustomGamesAsync()
    {
        var list = new List<string>();
        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name FROM CustomGames ORDER BY Name";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }

        return list;
    }

    public static List<string> GetCustomGames() =>
        GetCustomGamesAsync().GetAwaiter().GetResult();

    public static async Task SaveCustomGameAsync(string gameName)
    {
        if (string.IsNullOrWhiteSpace(gameName)) return;

        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO CustomGames (Name) VALUES (@Name)";
        command.Parameters.AddWithValue("@Name", gameName.Trim());
        await command.ExecuteNonQueryAsync();
    }

    public static void SaveCustomGame(string gameName) =>
        SaveCustomGameAsync(gameName).GetAwaiter().GetResult();

    // ── Custom Characters ─────────────────────────────────────────────

    public static async Task<List<string>> GetCustomCharactersAsync(string gameName)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(gameName)) return list;

        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name FROM CustomCharacters WHERE GameName = @GameName ORDER BY Name";
        command.Parameters.AddWithValue("@GameName", gameName.Trim());

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }

        return list;
    }

    public static List<string> GetCustomCharacters(string gameName) =>
        GetCustomCharactersAsync(gameName).GetAwaiter().GetResult();

    public static async Task SaveCustomCharacterAsync(string gameName, string charName)
    {
        if (string.IsNullOrWhiteSpace(gameName) || string.IsNullOrWhiteSpace(charName)) return;

        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO CustomCharacters (GameName, Name) VALUES (@GameName, @Name)";
        command.Parameters.AddWithValue("@GameName", gameName.Trim());
        command.Parameters.AddWithValue("@Name", charName.Trim());
        await command.ExecuteNonQueryAsync();
    }

    public static void SaveCustomCharacter(string gameName, string charName) =>
        SaveCustomCharacterAsync(gameName, charName).GetAwaiter().GetResult();

    // ── User Presets ──────────────────────────────────────────────────

    public static async Task<List<UserPreset>> GetUserPresetsAsync()
    {
        var list = new List<UserPreset>();
        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, PresetName, DefaultGame, DefaultMode, DefaultRounds, PlayerCount, PlayerNames FROM UserPresets ORDER BY PresetName";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new UserPreset
            {
                Id = reader.GetInt32(0),
                PresetName = reader.GetString(1),
                DefaultGame = reader.IsDBNull(2) ? null : reader.GetString(2),
                DefaultMode = reader.IsDBNull(3) ? null : reader.GetString(3),
                DefaultRounds = reader.IsDBNull(4) ? 3 : reader.GetInt32(4),
                PlayerCount = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                PlayerNames = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }

        return list;
    }

    public static List<UserPreset> GetUserPresets() =>
        GetUserPresetsAsync().GetAwaiter().GetResult();

    public static async Task SaveUserPresetAsync(UserPreset preset)
    {
        if (string.IsNullOrWhiteSpace(preset.PresetName)) return;

        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO UserPresets (PresetName, DefaultGame, DefaultMode, DefaultRounds, PlayerCount, PlayerNames)
            VALUES (@PresetName, @DefaultGame, @DefaultMode, @DefaultRounds, @PlayerCount, @PlayerNames)";
        command.Parameters.AddWithValue("@PresetName", preset.PresetName.Trim());
        command.Parameters.AddWithValue("@DefaultGame", (object?)preset.DefaultGame ?? DBNull.Value);
        command.Parameters.AddWithValue("@DefaultMode", (object?)preset.DefaultMode ?? DBNull.Value);
        command.Parameters.AddWithValue("@DefaultRounds", preset.DefaultRounds);
        command.Parameters.AddWithValue("@PlayerCount", preset.PlayerCount);
        command.Parameters.AddWithValue("@PlayerNames", (object?)preset.PlayerNames ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    public static void SaveUserPreset(UserPreset preset) =>
        SaveUserPresetAsync(preset).GetAwaiter().GetResult();

    public static async Task DeleteUserPresetAsync(string presetName)
    {
        if (string.IsNullOrWhiteSpace(presetName)) return;

        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM UserPresets WHERE PresetName = @PresetName";
        command.Parameters.AddWithValue("@PresetName", presetName.Trim());
        await command.ExecuteNonQueryAsync();
    }

    public static void DeleteUserPreset(string presetName) =>
        DeleteUserPresetAsync(presetName).GetAwaiter().GetResult();

    public static async Task<int> PruneEmptySessionsAsync()
    {
        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                DELETE FROM Tournaments 
                WHERE Id NOT IN (
                    SELECT DISTINCT c.TournamentId 
                    FROM Cycles c
                    JOIN Matches m ON m.CycleId = c.Id
                    WHERE m.WinnerId IS NOT NULL
                );";
            int count = await command.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return count;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static int PruneEmptySessions() =>
        PruneEmptySessionsAsync().GetAwaiter().GetResult();

    public static async Task<UserProfileInfo> GetUserProfileAsync(string nickname)
    {
        var info = new UserProfileInfo { Nickname = nickname };

        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();

        // 1. Fetch total matches and wins
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT SUM(TotalWins), SUM(TotalMatches) 
                FROM Players 
                WHERE Name = @Nickname;";
            cmd.Parameters.AddWithValue("@Nickname", nickname);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync() && !reader.IsDBNull(1))
            {
                info.TotalWins = reader.GetInt32(0);
                info.TotalMatches = reader.GetInt32(1);
                if (info.TotalMatches > 0)
                {
                    info.WinRate = (info.TotalWins * 100.0) / info.TotalMatches;
                }
            }
        }

        // 2. Fetch favorite character
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT CharacterName, SUM(PickCount) as TotalPicks 
                FROM PlayerCharacterPicks 
                INNER JOIN Players ON PlayerCharacterPicks.PlayerId = Players.Id 
                WHERE Players.Name = @Nickname 
                GROUP BY CharacterName 
                ORDER BY TotalPicks DESC 
                LIMIT 1;";
            cmd.Parameters.AddWithValue("@Nickname", nickname);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null)
            {
                info.FavoriteCharacter = result.ToString()!;
            }
        }

        // 3. Fetch favorite game
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT Tournaments.SelectedGame, COUNT(Players.Id) as PlayCount 
                FROM Players 
                INNER JOIN Tournaments ON Players.TournamentId = Tournaments.Id 
                WHERE Players.Name = @Nickname 
                GROUP BY Tournaments.SelectedGame 
                ORDER BY PlayCount DESC 
                LIMIT 1;";
            cmd.Parameters.AddWithValue("@Nickname", nickname);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null)
            {
                info.FavoriteGame = result.ToString()!;
            }
        }

        // 4. Fetch last match record (date) from last tournament session
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT Tournaments.SessionName 
                FROM Players 
                INNER JOIN Tournaments ON Players.TournamentId = Tournaments.Id 
                WHERE Players.Name = @Nickname 
                ORDER BY Tournaments.Id DESC 
                LIMIT 1;";
            cmd.Parameters.AddWithValue("@Nickname", nickname);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null)
            {
                string sessionName = result.ToString()!;
                var parts = sessionName.Split(new[] { " - " }, StringSplitOptions.None);
                if (parts.Length > 0 && parts[0].Length == 10 && parts[0].Contains('-'))
                {
                    info.LastMatchDate = parts[0];
                }
                else
                {
                    info.LastMatchDate = sessionName;
                }
            }
        }

        return info;
    }

    public static UserProfileInfo GetUserProfile(string nickname) =>
        GetUserProfileAsync(nickname).GetAwaiter().GetResult();

    public static async Task PurgeUserCompletelyAsync(string nickname)
    {
        using var connection = DatabaseConnector.Instance.GetConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            // 1. Delete from Users table
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Users WHERE Nickname = @Nickname;";
                cmd.Parameters.AddWithValue("@Nickname", nickname);
                cmd.Transaction = transaction;
                await cmd.ExecuteNonQueryAsync();
            }

            // 2. Delete from Players table
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Players WHERE Name = @Nickname;";
                cmd.Parameters.AddWithValue("@Nickname", nickname);
                cmd.Transaction = transaction;
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static void PurgeUserCompletely(string nickname) =>
        PurgeUserCompletelyAsync(nickname).GetAwaiter().GetResult();
}
