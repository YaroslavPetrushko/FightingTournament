using FightingTournament.Models;
using FightingTournament.Services;
using System;
using System.Collections.ObjectModel;

namespace FightingTournament.ViewModels;

public class MatchRowViewModel : BaseViewModel
{
    private readonly Match _match;

    // Unique per instance so RadioButtons in different rows don't interfere
    public string MatchId     { get; } = Guid.NewGuid().ToString();

    public string Player1Name => _match.Player1.Name;
    public string Player2Name => _match.Player2.Name;

    // ── Characters ───────────────────────────────────────────────────

    private string? _char1;
    public string? Character1
    {
        get => _char1;
        set { Set(ref _char1, value); _match.Character1 = value; }
    }

    private string? _char2;
    public string? Character2
    {
        get => _char2;
        set { Set(ref _char2, value); _match.Character2 = value; }
    }

    // ── Winner (1 / 2 / null) ────────────────────────────────────────

    private int? _winnerId;
    public int? WinnerId
    {
        get => _winnerId;
        set
        {
            _winnerId       = value;
            _match.WinnerId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Player1Won));
            OnPropertyChanged(nameof(Player2Won));
            OnPropertyChanged(nameof(IsCompleted));
        }
    }

    // These two properties drive the RadioButton IsChecked bindings.
    // Mutual exclusion is handled both here and by RadioButton.GroupName = MatchId.
    public bool Player1Won
    {
        get => WinnerId == 1;
        set { if (value) WinnerId = 1; }
    }

    public bool Player2Won
    {
        get => WinnerId == 2;
        set { if (value) WinnerId = 2; }
    }

    public bool IsCompleted => WinnerId.HasValue;

    public int Rounds
    {
        get => _match.Rounds;
        set
        {
            if (_match.Rounds != value)
            {
                _match.Rounds = value;
                OnPropertyChanged();
            }
        }
    }

    public bool CanEditRounds { get; }
    public ObservableCollection<int> AvailableRounds { get; } = new() { 2, 3, 4, 5 };

    // ── Character presets ────────────────────────────────────────────

    public ObservableCollection<string> AvailableCharacters { get; }

    public MatchRowViewModel(Match match, string selectedGame, TournamentMode mode)
    {
        _match = match;
        _char1 = match.Character1;
        _char2 = match.Character2;
        _winnerId = match.WinnerId;
        CanEditRounds = mode == TournamentMode.Endless;

        var allChars = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Add static presets
        if (!string.IsNullOrWhiteSpace(selectedGame) && GameDatabase.Games.TryGetValue(selectedGame, out var list))
        {
            foreach (var c in list)
            {
                allChars.Add(c);
            }
        }

        // 2. Add dynamically learned characters
        try
        {
            var customChars = DatabaseRepository.GetCustomCharacters(selectedGame);
            foreach (var c in customChars)
            {
                allChars.Add(c);
            }
        }
        catch
        {
            // Ignore database errors during initialization
        }

        AvailableCharacters = new ObservableCollection<string>(allChars);
    }
}