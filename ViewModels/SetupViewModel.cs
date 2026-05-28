using FightingTournament.Models;
using FightingTournament.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace FightingTournament.ViewModels;

public class SetupViewModel : BaseViewModel
{
    // Raised when the user clicks START and validation passes.
    public event Action<Tournament>? TournamentStarted;

    // ── Game selection ───────────────────────────────────────────────

    public ObservableCollection<string> AvailableGames { get; } = new();

    private string _selectedGame = "Tekken 8";
    public string SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (Set(ref _selectedGame, value))
            {
                UpdateDefaultSessionName();
            }
        }
    }

    private string _sessionName = string.Empty;
    public string SessionName
    {
        get => _sessionName;
        set => Set(ref _sessionName, value);
    }

    // ── Mode selection ───────────────────────────────────────────────

    private TournamentMode _selectedMode = TournamentMode.Endless;
    public TournamentMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (Set(ref _selectedMode, value))
            {
                OnPropertyChanged(nameof(IsEndlessModeSelected));
                OnPropertyChanged(nameof(IsChampionshipModeSelected));
                UpdateDefaultSessionName();
            }
        }
    }

    public bool IsEndlessModeSelected
    {
        get => SelectedMode == TournamentMode.Endless;
        set { if (value) SelectedMode = TournamentMode.Endless; }
    }

    public bool IsChampionshipModeSelected
    {
        get => SelectedMode == TournamentMode.Championship;
        set { if (value) SelectedMode = TournamentMode.Championship; }
    }

    // ── Default Rounds ───────────────────────────────────────────────

    public ObservableCollection<int> AvailableRoundsList { get; } = new() { 2, 3, 4, 5 };

    private int _selectedRounds = 3;
    public int SelectedRounds
    {
        get => _selectedRounds;
        set => Set(ref _selectedRounds, value);
    }

    // ── User Presets ──────────────────────────────────────────────────

    public ObservableCollection<UserPreset> UserPresets { get; } = new();

    private UserPreset? _selectedUserPreset;
    public UserPreset? SelectedUserPreset
    {
        get => _selectedUserPreset;
        set
        {
            if (Set(ref _selectedUserPreset, value))
            {
                if (value != null)
                {
                    LoadPreset(value);
                }
            }
        }
    }

    private string _newPresetName = string.Empty;
    public string NewPresetName
    {
        get => _newPresetName;
        set => Set(ref _newPresetName, value);
    }

    // ── Saved sessions ───────────────────────────────────────────────

    public ObservableCollection<string> SavedSessions { get; } = new();

    private string? _selectedSavedSession;
    public string? SelectedSavedSession
    {
        get => _selectedSavedSession;
        set => Set(ref _selectedSavedSession, value);
    }

    // ── Registered players ───────────────────────────────────────────

    public ObservableCollection<string> RegisteredUsers { get; } = new();

    // ── Player count ─────────────────────────────────────────────────

    private int _playerCount = 4;
    public int PlayerCount
    {
        get => _playerCount;
        set
        {
            int clamped = Math.Clamp(value, 2, 16);
            if (!Set(ref _playerCount, clamped)) return;
            SyncPlayerEntries();
        }
    }

    public ObservableCollection<PlayerNameEntry> PlayerEntries { get; } = new();

    // ── Validation message ───────────────────────────────────────────

    private string _validationMessage = string.Empty;
    public string ValidationMessage
    {
        get => _validationMessage;
        private set => Set(ref _validationMessage, value);
    }

    // ── Commands ─────────────────────────────────────────────────────

    public ICommand IncrementCommand     { get; }
    public ICommand DecrementCommand     { get; }
    public ICommand StartCommand         { get; }
    public ICommand ResumeCommand        { get; }
    public ICommand DeleteSessionCommand { get; }
    public ICommand AddPlayerCommand     { get; }
    public ICommand SavePresetCommand    { get; }
    public ICommand DeletePresetCommand  { get; }

    public SetupViewModel()
    {
        IncrementCommand     = new RelayCommand(() => PlayerCount++);
        DecrementCommand     = new RelayCommand(() => PlayerCount--);
        StartCommand         = new RelayCommand(StartTournament);
        ResumeCommand        = new RelayCommand(ResumeTournament);
        DeleteSessionCommand = new RelayCommand(DeleteSession);
        AddPlayerCommand     = new RelayCommand(p =>
        {
            if (p is string nickname)
            {
                AddPlayer(nickname);
            }
        });
        SavePresetCommand    = new RelayCommand(SavePreset);
        DeletePresetCommand  = new RelayCommand(DeletePreset);

        SyncPlayerEntries();
        RefreshSavedSessions();
        RefreshRegisteredUsers();
        RefreshAvailableGames();
        RefreshUserPresets();
        UpdateDefaultSessionName();
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private void RefreshAvailableGames()
    {
        AvailableGames.Clear();
        var games = new SortedSet<string>(GameDatabase.Games.Keys, StringComparer.OrdinalIgnoreCase);
        try
        {
            var customGames = DatabaseRepository.GetCustomGames();
            foreach (var g in customGames)
            {
                games.Add(g);
            }
        }
        catch {}

        foreach (var g in games)
        {
            AvailableGames.Add(g);
        }
    }

    public void RefreshUserPresets()
    {
        UserPresets.Clear();
        try
        {
            var presets = DatabaseRepository.GetUserPresets();
            foreach (var p in presets)
            {
                UserPresets.Add(p);
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"⚠  Could not query presets: {ex.Message}";
        }
    }

    private void SavePreset()
    {
        if (string.IsNullOrWhiteSpace(NewPresetName))
        {
            ValidationMessage = "⚠  Please enter a name for the new preset.";
            return;
        }

        var preset = new UserPreset
        {
            PresetName = NewPresetName.Trim(),
            DefaultGame = SelectedGame,
            DefaultMode = SelectedMode.ToString(),
            DefaultRounds = SelectedRounds,
            PlayerCount = PlayerCount,
            PlayerNames = string.Join(",", PlayerEntries.Select(e => e.Name.Trim()))
        };

        try
        {
            DatabaseRepository.SaveUserPreset(preset);
            NewPresetName = string.Empty;
            RefreshUserPresets();
            ValidationMessage = $"✓  Preset '{preset.PresetName}' saved.";
        }
        catch (Exception ex)
        {
            ValidationMessage = $"⚠  Could not save preset: {ex.Message}";
        }
    }

    private void DeletePreset()
    {
        if (SelectedUserPreset == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete the preset \"{SelectedUserPreset.PresetName}\"?",
            "Delete Preset",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            DatabaseRepository.DeleteUserPreset(SelectedUserPreset.PresetName);
            RefreshUserPresets();
            SelectedUserPreset = null;
            ValidationMessage = "✓  Preset deleted successfully.";
        }
        catch (Exception ex)
        {
            ValidationMessage = $"⚠  Could not delete preset: {ex.Message}";
        }
    }

    private void LoadPreset(UserPreset preset)
    {
        if (preset == null) return;

        // Set SelectedGame
        if (!string.IsNullOrWhiteSpace(preset.DefaultGame))
        {
            if (!AvailableGames.Contains(preset.DefaultGame))
            {
                AvailableGames.Add(preset.DefaultGame);
            }
            SelectedGame = preset.DefaultGame;
        }

        // Set SelectedMode
        if (!string.IsNullOrWhiteSpace(preset.DefaultMode) && Enum.TryParse<TournamentMode>(preset.DefaultMode, out var mode))
        {
            SelectedMode = mode;
        }

        // Set DefaultRounds
        SelectedRounds = preset.DefaultRounds;

        // Set PlayerCount and PlayerEntries
        if (preset.PlayerCount >= 2 && preset.PlayerCount <= 16)
        {
            PlayerCount = preset.PlayerCount;
        }

        if (!string.IsNullOrWhiteSpace(preset.PlayerNames))
        {
            var names = preset.PlayerNames.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < PlayerEntries.Count; i++)
            {
                if (i < names.Length)
                {
                    PlayerEntries[i].Name = names[i].Trim();
                }
            }
        }
    }

    private void UpdateDefaultSessionName()
    {
        string modeStr = SelectedMode == TournamentMode.Championship ? "Championship" : "Endless";
        SessionName = $"{DateTime.Now:dd.MM.yyyy} - {modeStr} - {SelectedGame}";
    }

    /// Keeps PlayerEntries in sync with PlayerCount without losing typed names.
    private void SyncPlayerEntries()
    {
        while (PlayerEntries.Count < PlayerCount)
            PlayerEntries.Add(new PlayerNameEntry(
                PlayerEntries.Count + 1,
                $"Player {PlayerEntries.Count + 1}"));

        while (PlayerEntries.Count > PlayerCount)
            PlayerEntries.RemoveAt(PlayerEntries.Count - 1);

        // Re-number displayed indices
        for (int i = 0; i < PlayerEntries.Count; i++)
            PlayerEntries[i].Index = i + 1;
    }

    private void StartTournament()
    {
        ValidationMessage = string.Empty;

        var names = PlayerEntries.Select(e => e.Name.Trim()).ToList();

        if (names.Any(string.IsNullOrWhiteSpace))
        {
            ValidationMessage = "⚠  All player names must be filled in.";
            return;
        }

        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count)
        {
            ValidationMessage = "⚠  Player names must be unique.";
            return;
        }

        string session = SessionName.Trim();
        if (string.IsNullOrWhiteSpace(session))
        {
            ValidationMessage = "⚠  Session name cannot be empty.";
            return;
        }

        // Save typed game to CustomGames if it is new
        if (!string.IsNullOrWhiteSpace(SelectedGame))
        {
            try
            {
                DatabaseRepository.SaveCustomGame(SelectedGame);
            }
            catch {}
        }

        var tournament = TournamentEngine.Create(names, SelectedMode, SelectedRounds);
        tournament.SelectedGame = SelectedGame;
        tournament.SessionName = session;
        TournamentStarted?.Invoke(tournament);
    }

    public void RefreshSavedSessions()
    {
        SavedSessions.Clear();
        try
        {
            var list = DatabaseRepository.GetSavedSessions();
            foreach (var s in list)
            {
                SavedSessions.Add(s);
            }
            if (SavedSessions.Count > 0)
            {
                SelectedSavedSession = SavedSessions[0];
            }
            else
            {
                SelectedSavedSession = null;
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"⚠  Could not query saved sessions: {ex.Message}";
        }
    }

    public void RefreshRegisteredUsers()
    {
        RegisteredUsers.Clear();
        try
        {
            var list = DatabaseRepository.GetRegisteredUsers();
            foreach (var u in list)
            {
                RegisteredUsers.Add(u);
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"⚠  Could not query registered players: {ex.Message}";
        }
    }

    private bool IsDefaultOrEmpty(string name)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return true;

        if (name.StartsWith("Player ", StringComparison.OrdinalIgnoreCase))
        {
            string suffix = name.Substring(7);
            if (int.TryParse(suffix, out _))
                return true;
        }
        return false;
    }

    private void AddPlayer(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname)) return;

        bool alreadyExists = PlayerEntries.Any(e => string.Equals(e.Name.Trim(), nickname.Trim(), StringComparison.OrdinalIgnoreCase));
        if (alreadyExists) return;

        var firstDefaultEntry = PlayerEntries.FirstOrDefault(e => IsDefaultOrEmpty(e.Name));
        if (firstDefaultEntry != null)
        {
            firstDefaultEntry.Name = nickname.Trim();
        }
        else
        {
            if (PlayerCount < 16)
            {
                PlayerCount++;
                var newEntry = PlayerEntries.LastOrDefault();
                if (newEntry != null)
                {
                    newEntry.Name = nickname.Trim();
                }
            }
        }
    }

    private void ResumeTournament()
    {
        ValidationMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(SelectedSavedSession))
        {
            ValidationMessage = "⚠  Please select a session to resume.";
            return;
        }

        try
        {
            var tournament = DatabaseRepository.LoadTournamentState(SelectedSavedSession);
            if (tournament is null)
            {
                ValidationMessage = "⚠  Could not load the selected tournament session.";
                return;
            }

            TournamentStarted?.Invoke(tournament);
        }
        catch (Exception ex)
        {
            ValidationMessage = $"⚠  Error loading session: {ex.Message}";
        }
    }

    private void DeleteSession()
    {
        ValidationMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(SelectedSavedSession)) return;

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete the saved session \"{SelectedSavedSession}\"?\n\nThis cannot be undone.",
            "Delete Saved Session",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            DatabaseRepository.DeleteTournamentState(SelectedSavedSession);
            RefreshSavedSessions();
        }
        catch (Exception ex)
        {
            ValidationMessage = $"⚠  Error deleting session: {ex.Message}";
        }
    }
}
