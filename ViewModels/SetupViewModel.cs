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

    public ObservableCollection<string> AvailableGames { get; } = new(GameDatabase.Games.Keys);

    private string _selectedGame = "Tekken 8";
    public string SelectedGame
    {
        get => _selectedGame;
        set => Set(ref _selectedGame, value);
    }

    private string _sessionName = DateTime.Now.ToString("dd.MM.yyyy");
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

        SyncPlayerEntries();
        RefreshSavedSessions();
        RefreshRegisteredUsers();
    }

    // ── Helpers ──────────────────────────────────────────────────────

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

        var tournament = TournamentEngine.Create(names, SelectedMode);
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
