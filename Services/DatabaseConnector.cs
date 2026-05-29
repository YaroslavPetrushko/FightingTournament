using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace FightingTournament.Services;

public class DatabaseConnector
{
    private static readonly Lazy<DatabaseConnector> _instance =
        new(() => new DatabaseConnector());

    public static DatabaseConnector Instance => _instance.Value;

    private readonly object _lock = new();
    private string _dbPath;
    private string _connectionString;

    public string CurrentDbPath
    {
        get
        {
            lock (_lock)
            {
                return _dbPath;
            }
        }
    }

    private DatabaseConnector()
    {
        // Place FightingTournament.db in the application execution directory
        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FightingTournament.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true // Enable foreign keys constraint support!
        }.ToString();
    }

    public void ChangeDatabasePath(string newPath)
    {
        if (string.IsNullOrWhiteSpace(newPath)) return;

        lock (_lock)
        {
            _dbPath = newPath;
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                ForeignKeys = true
            }.ToString();
        }

        InitializeDatabase();
    }

    public void SaveDatabaseCopyAs(string targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath)) return;

        // Clear all Sqlite connection pools to release any lock on the current database file
        SqliteConnection.ClearAllPools();

        string currentPath = CurrentDbPath;
        if (File.Exists(currentPath))
        {
            File.Copy(currentPath, targetPath, true);
        }
    }

    /// <summary>
    /// Gets a new SQLite connection. The caller is responsible for opening and disposing it.
    /// </summary>
    public SqliteConnection GetConnection()
    {
        lock (_lock)
        {
            return new SqliteConnection(_connectionString);
        }
    }

    /// <summary>
    /// Initializes the database file and creates the schema tables if they do not exist.
    /// </summary>
    public void InitializeDatabase()
    {
        using var connection = GetConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            // 0. Users table (global player catalog)
            string createUsers = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Nickname TEXT PRIMARY KEY
                );";

            // 1. Tournaments table
            string createTournaments = @"
                CREATE TABLE IF NOT EXISTS Tournaments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionName TEXT NOT NULL,
                    SelectedGame TEXT NOT NULL,
                    CurrentCycleIndex INTEGER NOT NULL DEFAULT 0,
                    IsFinished INTEGER NOT NULL DEFAULT 0,
                    TournamentMode TEXT NOT NULL DEFAULT 'Endless',
                    DefaultRounds INTEGER NOT NULL DEFAULT 3
                );";

            // 2. Players table
            string createPlayers = @"
                CREATE TABLE IF NOT EXISTS Players (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TournamentId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    IsEliminated INTEGER NOT NULL DEFAULT 0,
                    TotalWins INTEGER NOT NULL DEFAULT 0,
                    TotalLosses INTEGER NOT NULL DEFAULT 0,
                    TotalMatches INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY(TournamentId) REFERENCES Tournaments(Id) ON DELETE CASCADE
                );";

            // 3. PlayerCharacterPicks table
            string createCharacterPicks = @"
                CREATE TABLE IF NOT EXISTS PlayerCharacterPicks (
                    PlayerId INTEGER NOT NULL,
                    CharacterName TEXT NOT NULL,
                    PickCount INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY(PlayerId, CharacterName),
                    FOREIGN KEY(PlayerId) REFERENCES Players(Id) ON DELETE CASCADE
                );";

            // 4. Cycles table
            string createCycles = @"
                CREATE TABLE IF NOT EXISTS Cycles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TournamentId INTEGER NOT NULL,
                    Number INTEGER NOT NULL,
                    FOREIGN KEY(TournamentId) REFERENCES Tournaments(Id) ON DELETE CASCADE
                );";

            // 5. Matches table
            string createMatches = @"
                CREATE TABLE IF NOT EXISTS Matches (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CycleId INTEGER NOT NULL,
                    Player1Id INTEGER NOT NULL,
                    Player2Id INTEGER NOT NULL,
                    WinnerId INTEGER, -- NULL, 1, or 2
                    Character1 TEXT,
                    Character2 TEXT,
                    Rounds INTEGER NOT NULL DEFAULT 3,
                    FOREIGN KEY(CycleId) REFERENCES Cycles(Id) ON DELETE CASCADE,
                    FOREIGN KEY(Player1Id) REFERENCES Players(Id) ON DELETE CASCADE,
                    FOREIGN KEY(Player2Id) REFERENCES Players(Id) ON DELETE CASCADE
                );";

            // 6. CustomGames table
            string createCustomGames = @"
                CREATE TABLE IF NOT EXISTS CustomGames (
                    Name TEXT PRIMARY KEY
                );";

            // 7. CustomCharacters table
            string createCustomCharacters = @"
                CREATE TABLE IF NOT EXISTS CustomCharacters (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GameName TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    UNIQUE(GameName, Name)
                );";

            // 8. UserPresets table
            string createUserPresets = @"
                CREATE TABLE IF NOT EXISTS UserPresets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PresetName TEXT UNIQUE NOT NULL,
                    DefaultGame TEXT,
                    DefaultMode TEXT,
                    DefaultRounds INTEGER NOT NULL DEFAULT 3,
                    PlayerCount INTEGER,
                    PlayerNames TEXT
                );";

            ExecuteNonQuery(createUsers, connection, transaction);
            ExecuteNonQuery(createTournaments, connection, transaction);
            ExecuteNonQuery(createPlayers, connection, transaction);
            ExecuteNonQuery(createCharacterPicks, connection, transaction);
            ExecuteNonQuery(createCycles, connection, transaction);
            ExecuteNonQuery(createMatches, connection, transaction);
            ExecuteNonQuery(createCustomGames, connection, transaction);
            ExecuteNonQuery(createCustomCharacters, connection, transaction);
            ExecuteNonQuery(createUserPresets, connection, transaction);

            // 9. UserSettings table
            string createUserSettings = @"
                CREATE TABLE IF NOT EXISTS UserSettings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );";
            ExecuteNonQuery(createUserSettings, connection, transaction);

            // Backward compatibility: Alter table to add TournamentMode and DefaultRounds if missing
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.Transaction = transaction;
                checkCmd.CommandText = "PRAGMA table_info(Tournaments);";
                using var checkReader = checkCmd.ExecuteReader();
                bool modeExists = false;
                bool defaultRoundsExists = false;
                while (checkReader.Read())
                {
                    string colName = checkReader.GetString(1);
                    if (colName.Equals("TournamentMode", StringComparison.OrdinalIgnoreCase))
                        modeExists = true;
                    if (colName.Equals("DefaultRounds", StringComparison.OrdinalIgnoreCase))
                        defaultRoundsExists = true;
                }
                if (!modeExists)
                {
                    using (var alterCmd = connection.CreateCommand())
                    {
                        alterCmd.Transaction = transaction;
                        alterCmd.CommandText = "ALTER TABLE Tournaments ADD COLUMN TournamentMode TEXT NOT NULL DEFAULT 'Endless';";
                        alterCmd.ExecuteNonQuery();
                    }
                }
                if (!defaultRoundsExists)
                {
                    using (var alterCmd = connection.CreateCommand())
                    {
                        alterCmd.Transaction = transaction;
                        alterCmd.CommandText = "ALTER TABLE Tournaments ADD COLUMN DefaultRounds INTEGER NOT NULL DEFAULT 3;";
                        alterCmd.ExecuteNonQuery();
                    }
                }
            }

            // Backward compatibility: Alter Matches to add Rounds if missing
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.Transaction = transaction;
                checkCmd.CommandText = "PRAGMA table_info(Matches);";
                using var checkReader = checkCmd.ExecuteReader();
                bool roundsExists = false;
                while (checkReader.Read())
                {
                    if (checkReader.GetString(1).Equals("Rounds", StringComparison.OrdinalIgnoreCase))
                    {
                        roundsExists = true;
                        break;
                    }
                }
                if (!roundsExists)
                {
                    using (var alterCmd = connection.CreateCommand())
                    {
                        alterCmd.Transaction = transaction;
                        alterCmd.CommandText = "ALTER TABLE Matches ADD COLUMN Rounds INTEGER NOT NULL DEFAULT 3;";
                        alterCmd.ExecuteNonQuery();
                    }
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

    public string GetSetting(string key, string defaultValue)
    {
        try
        {
            using var connection = GetConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Value FROM UserSettings WHERE Key = @Key;";
            cmd.Parameters.AddWithValue("@Key", key);
            var result = cmd.ExecuteScalar();
            return result != null ? result.ToString()! : defaultValue;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get setting '{key}': {ex.Message}");
            return defaultValue;
        }
    }

    public void SaveSetting(string key, string value)
    {
        try
        {
            using var connection = GetConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO UserSettings (Key, Value) 
                VALUES (@Key, @Value)
                ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
            cmd.Parameters.AddWithValue("@Key", key);
            cmd.Parameters.AddWithValue("@Value", value);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save setting '{key}' = '{value}': {ex.Message}");
        }
    }

    private void ExecuteNonQuery(string commandText, SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Transaction = transaction;
        command.ExecuteNonQuery();
    }
}
