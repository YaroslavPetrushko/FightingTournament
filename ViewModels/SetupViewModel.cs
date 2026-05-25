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

    public ICommand IncrementCommand { get; }
    public ICommand DecrementCommand { get; }
    public ICommand StartCommand     { get; }

    public SetupViewModel()
    {
        IncrementCommand = new RelayCommand(() => PlayerCount++);
        DecrementCommand = new RelayCommand(() => PlayerCount--);
        StartCommand     = new RelayCommand(StartTournament);

        SyncPlayerEntries();
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

        var tournament = TournamentEngine.Create(names);
        TournamentStarted?.Invoke(tournament);
    }
}
