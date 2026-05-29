using FightingTournament.Models;
using FightingTournament.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FightingTournament.ViewModels;

public class TournamentViewModel : BaseViewModel
{
    private readonly Tournament _tournament;
    private readonly Action     _onNewTournament;

    // ── Collections ──────────────────────────────────────────────────

    public ObservableCollection<CycleInfoViewModel>   CycleSchedule  { get; } = new();
    public ObservableCollection<MatchRowViewModel>    CurrentMatches { get; } = new();
    public ObservableCollection<PlayerStatsViewModel> Standings      { get; } = new();

    // ── Bracket View tracking ────────────────────────────────────────

    private bool _showBracket = false;
    public bool ShowBracket
    {
        get => _showBracket;
        set => Set(ref _showBracket, value);
    }

    public bool ShowBracketToggleVisible => _tournament.Mode == TournamentMode.Championship;

    public bool IsFinished => _tournament.IsFinished;

    public string WinnerName
    {
        get
        {
            if (!_tournament.IsFinished) return string.Empty;
            var winner = _tournament.Players.FirstOrDefault(p => !p.IsEliminated && !p.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase));
            return winner?.Name ?? string.Empty;
        }
    }

    // ── Selected Cycle tracking ──────────────────────────────────────

    private int _selectedCycleIndex = 0;
    public int SelectedCycleIndex
    {
        get => _selectedCycleIndex;
        set
        {
            if (Set(ref _selectedCycleIndex, value))
            {
                OnPropertyChanged(nameof(CycleHeader));
                OnPropertyChanged(nameof(SaveButtonText));
                OnPropertyChanged(nameof(IsFinished));
                OnPropertyChanged(nameof(WinnerName));
                OnPropertyChanged(nameof(CanAddPlayerMidTournament));
                RefreshScheduleSidebar();
                LoadCurrentCycleMatches();
            }
        }
    }

    public string SaveButtonText =>
        SelectedCycleIndex == _tournament.CurrentCycleIndex ? "Save & Next Cycle  ►" : "Save Changes  ✓";

    // ── Header ───────────────────────────────────────────────────────

    public string CycleHeader
    {
        get
        {
            if (SelectedCycleIndex != _tournament.CurrentCycleIndex)
            {
                return $"EDITING Cycle  {SelectedCycleIndex + 1}   •   [HISTORICAL DATA]";
            }
            return $"Cycle  {_tournament.CurrentCycleIndex + 1}   •   " +
                   $"{ActiveCount()} active players   •   " +
                   $"{_tournament.CurrentCycle?.Matches.Count ?? 0} matches";
        }
    }

    private int ActiveCount() =>
        _tournament.Players.Count(p => !p.IsEliminated);

    // ── Status ───────────────────────────────────────────────────────

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        private set => Set(ref _statusMessage, value);
    }

    // ── Mid-Tournament Add Player ──────────────────────────────────────

    private bool _isAddingPlayer = false;
    public bool IsAddingPlayer
    {
        get => _isAddingPlayer;
        set => Set(ref _isAddingPlayer, value);
    }

    private string _newPlayerName = string.Empty;
    public string NewPlayerName
    {
        get => _newPlayerName;
        set => Set(ref _newPlayerName, value);
    }

    public bool CanAddPlayerMidTournament => _tournament.Mode == TournamentMode.Endless && !_tournament.IsFinished;

    // ── Commands ─────────────────────────────────────────────────────

    public ICommand CommitCycleCommand   { get; }
    public ICommand NewTournamentCommand { get; }
    public ICommand SelectCycleCommand   { get; }
    public ICommand ShowAddPlayerCommand  { get; }
    public ICommand ShowUserProfileCommand { get; }
    public ICommand SaveNewPlayerCommand  { get; }
    public ICommand CancelAddPlayerCommand { get; }
    public ICommand ExportStandingsPngCommand { get; }
    public ICommand ExportBracketPngCommand   { get; }

    // ── Constructor ──────────────────────────────────────────────────

    public TournamentViewModel(Tournament tournament, Action onNewTournament)
    {
        _tournament         = tournament;
        _onNewTournament    = onNewTournament;
        _selectedCycleIndex = tournament.CurrentCycleIndex;

        CommitCycleCommand   = new RelayCommand(CommitCycle);
        NewTournamentCommand = new RelayCommand(onNewTournament);
        SelectCycleCommand   = new RelayCommand(p =>
        {
            if (p is CycleInfoViewModel vm) SelectCycle(vm);
        });
        ShowAddPlayerCommand = new RelayCommand(ShowAddPlayer);
        ShowUserProfileCommand = new RelayCommand(p =>
        {
            if (p is string nickname)
            {
                ShowUserProfile(nickname);
            }
        });
        SaveNewPlayerCommand = new RelayCommand(SaveNewPlayer);
        CancelAddPlayerCommand = new RelayCommand(CancelAddPlayer);
        ExportStandingsPngCommand = new RelayCommand(ExportStandingsPng);
        ExportBracketPngCommand   = new RelayCommand(ExportBracketPng);

        BuildStandings();
        RefreshScheduleSidebar();
        LoadCurrentCycleMatches();

        try
        {
            DatabaseRepository.SaveTournamentState(_tournament);
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠  Could not save tournament to database: {ex.Message}";
        }
    }

    // ── Build helpers ─────────────────────────────────────────────────

    private int GetEliminationCycle(Player player)
    {
        if (player.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase)) return 0;
        if (!player.IsEliminated) return int.MaxValue; // Still active!

        // Find the cycle index where they lost
        for (int i = _tournament.Cycles.Count - 1; i >= 0; i--)
        {
            var cycle = _tournament.Cycles[i];
            var lostMatch = cycle.Matches.FirstOrDefault(m => m.IsCompleted && m.WinnerId.HasValue && 
                ((m.WinnerId == 1 && m.Player2 == player) || (m.WinnerId == 2 && m.Player1 == player)));
            if (lostMatch != null)
            {
                return i + 1; // 1-based cycle index
            }
        }
        return 0; // Pre-eliminated or BYE
    }

    private List<PlayerStatsViewModel> SortStandingsList(List<PlayerStatsViewModel> list)
    {
        if (_tournament.Mode == TournamentMode.Championship)
        {
            return list
                .OrderBy(s => s.PlayerModel.IsEliminated ? 1 : 0) // Active first
                .ThenByDescending(s => GetEliminationCycle(s.PlayerModel)) // Eliminated later = higher rank
                .ThenByDescending(s => s.Wins)
                .ThenByDescending(s => s.Matches)
                .ThenBy(s => s.Name)
                .ToList();
        }
        else
        {
            return list
                .OrderByDescending(s => s.Wins)
                .ThenByDescending(s => s.Matches)
                .ThenBy(s => s.Name)
                .ToList();
        }
    }

    private void BuildStandings()
    {
        Standings.Clear();
        var list = new List<PlayerStatsViewModel>();
        foreach (var p in _tournament.Players)
        {
            if (p.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase)) continue;
            list.Add(new PlayerStatsViewModel(p, 1, OnEliminatePlayer));
        }

        var sorted = SortStandingsList(list);
        int rank = 1;
        foreach (var s in sorted)
        {
            s.Rank = rank++;
            Standings.Add(s);
        }
    }

    private void RefreshScheduleSidebar()
    {
        CycleSchedule.Clear();
        int currentActive = _tournament.CurrentCycleIndex;

        for (int i = 0; i < _tournament.Cycles.Count; i++)
        {
            CycleSchedule.Add(new CycleInfoViewModel(
                _tournament.Cycles[i],
                isCurrent:   i == SelectedCycleIndex,
                isCompleted: i < currentActive));
        }
    }

    private void LoadCurrentCycleMatches()
    {
        CurrentMatches.Clear();

        if (SelectedCycleIndex >= 0 && SelectedCycleIndex < _tournament.Cycles.Count)
        {
            var cycle = _tournament.Cycles[SelectedCycleIndex];
            foreach (var m in cycle.Matches)
            {
                // Skip matches where either player is a virtual BYE player
                if (m.Player1.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase) ||
                    m.Player2.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                CurrentMatches.Add(new MatchRowViewModel(m, _tournament.SelectedGame, _tournament.Mode));
            }
        }
    }

    // ── Commit ───────────────────────────────────────────────────────

    private void LearnCharactersFromCycle(Cycle cycle)
    {
        if (cycle == null) return;
        string game = _tournament.SelectedGame;
        var predefined = GameDatabase.Games.TryGetValue(game, out var list)
            ? new HashSet<string>(list, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        char[] separators = new char[] { ';', '/', '\\' };

        foreach (var m in cycle.Matches)
        {
            if (!string.IsNullOrWhiteSpace(m.Character1))
            {
                string[] parts = m.Character1.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    string c = part.Trim();
                    if (!string.IsNullOrWhiteSpace(c) && !predefined.Contains(c))
                    {
                        try { DatabaseRepository.SaveCustomCharacter(game, c); } catch { }
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(m.Character2))
            {
                string[] parts = m.Character2.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    string c = part.Trim();
                    if (!string.IsNullOrWhiteSpace(c) && !predefined.Contains(c))
                    {
                        try { DatabaseRepository.SaveCustomCharacter(game, c); } catch { }
                    }
                }
            }
        }
    }

    private void CommitCycle()
    {
        if (CurrentMatches.Any(m => !m.IsCompleted))
        {
            StatusMessage = "⚠  Fill in all match results before saving.";
            return;
        }

        // Check if we are editing a past cycle
        if (SelectedCycleIndex < _tournament.CurrentCycleIndex)
        {
            int editedNumber = SelectedCycleIndex + 1;

            // 1. Recalculate all players' statistics from scratch
            var eliminationStates = _tournament.Players.ToDictionary(p => p.Name, p => p.IsEliminated, StringComparer.OrdinalIgnoreCase);

            foreach (var p in _tournament.Players)
            {
                p.Reset();
                if (eliminationStates.TryGetValue(p.Name, out bool isElim))
                {
                    p.IsEliminated = isElim;
                }
            }

            // Re-record matches for all cycles before CurrentCycleIndex
            for (int i = 0; i < _tournament.CurrentCycleIndex; i++)
            {
                var cycle = _tournament.Cycles[i];
                foreach (var m in cycle.Matches)
                {
                    if (m.IsCompleted &&
                        !m.Player1.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase) &&
                        !m.Player2.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase))
                    {
                        bool p1Won = m.WinnerId == 1;
                        m.Player1.RecordResult(p1Won,  m.Character1);
                        m.Player2.RecordResult(!p1Won, m.Character2);
                    }
                }
            }

            // Learn custom characters from the edited cycle
            LearnCharactersFromCycle(_tournament.Cycles[SelectedCycleIndex]);

            // 2. Refresh Standings view models + re-sort
            foreach (var s in Standings) s.Refresh();

            var sorted = SortStandingsList(Standings.ToList());

            Standings.Clear();
            int rank = 1;
            foreach (var s in sorted) { s.Rank = rank++; Standings.Add(s); }

            try
            {
                DatabaseRepository.SaveTournamentState(_tournament);
                StatusMessage = $"✓  Cycle {editedNumber} changes saved to database and stats recalculated. Returning to active cycle...";
            }
            catch (Exception ex)
            {
                StatusMessage = $"⚠  Cycle {editedNumber} updated locally, but database save failed: {ex.Message}";
            }

            // Return to active cycle
            SelectedCycleIndex = _tournament.CurrentCycleIndex;
            return;
        }

        // Standard active cycle commit flow
        int savedNumber = _tournament.CurrentCycleIndex + 1;

        // Learn custom characters from the active cycle before committing it
        var activeCycle = _tournament.CurrentCycle;
        if (activeCycle != null)
        {
            LearnCharactersFromCycle(activeCycle);
        }

        if (!TournamentEngine.CommitCurrentCycle(_tournament))
        {
            StatusMessage = "⚠  Could not save cycle — check results.";
            return;
        }

        // Refresh standings + re-sort
        foreach (var s in Standings) s.Refresh();

        var sortedActive = SortStandingsList(Standings.ToList());

        Standings.Clear();
        int rankActive = 1;
        foreach (var s in sortedActive) { s.Rank = rankActive++; Standings.Add(s); }

        SelectedCycleIndex = _tournament.CurrentCycleIndex; // Keep synced

        try
        {
            DatabaseRepository.SaveTournamentState(_tournament);
            StatusMessage = $"✓  Cycle {savedNumber} saved to database. Starting Cycle {savedNumber + 1}…";
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠  Cycle {savedNumber} saved locally, but database save failed: {ex.Message}";
        }
    }

    private void SelectCycle(CycleInfoViewModel cycleVm)
    {
        if (cycleVm is null) return;

        int index = _tournament.Cycles.IndexOf(cycleVm.CycleModel);

        // Championship bracket cannot be safely re-edited — structure depends on prior winners
        if (_tournament.Mode == TournamentMode.Championship && index < _tournament.CurrentCycleIndex)
        {
            StatusMessage = "⚠  Historical edits are not supported in Championship mode.";
            return;
        }

        if (index >= 0 && index <= _tournament.CurrentCycleIndex)
        {
            SelectedCycleIndex = index;
            StatusMessage = $"✏️  Editing Cycle {index + 1}. Press '{SaveButtonText}' when finished.";
        }
    }

    // ── Eliminate ────────────────────────────────────────────────────

    private void OnEliminatePlayer(Player player)
    {
        if (ActiveCount() <= 2)
        {
            StatusMessage = "⚠  Cannot eliminate: at least 2 active players required.";
            return;
        }

        var result = MessageBox.Show(
            $"Remove \"{player.Name}\" from the tournament?\n\n" +
            "• Completed match results are kept.\n" +
            "• Unplayed matches in the current cycle will be removed.",
            "Eliminate Player",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        TournamentEngine.EliminatePlayer(_tournament, player);

        // Refresh match list (unplayed matches removed)
        LoadCurrentCycleMatches();

        // Mark row as eliminated in standings
        var statsVm = Standings.FirstOrDefault(s => s.PlayerModel == player);
        if (statsVm is not null) statsVm.IsEliminated = true;

        try
        {
            DatabaseRepository.SaveTournamentState(_tournament);
            StatusMessage = $"✗  {player.Name} eliminated and database updated. {ActiveCount()} players remaining.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗  {player.Name} eliminated locally, but database update failed: {ex.Message}";
        }
        OnPropertyChanged(nameof(CycleHeader));
        OnPropertyChanged(nameof(IsFinished));
        OnPropertyChanged(nameof(WinnerName));
        OnPropertyChanged(nameof(CanAddPlayerMidTournament));
    }

    // ── Add Player Mid-Tournament Handlers ───────────────────────────

    private void ShowAddPlayer()
    {
        NewPlayerName = string.Empty;
        IsAddingPlayer = true;
    }

    private void CancelAddPlayer()
    {
        NewPlayerName = string.Empty;
        IsAddingPlayer = false;
    }

    private void SaveNewPlayer()
    {
        string name = NewPlayerName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "⚠  Player name cannot be empty.";
            return;
        }

        if (name.Equals("BYE", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "⚠  'BYE' is a reserved keyword.";
            return;
        }

        // Check if player with the same name already exists
        if (_tournament.Players.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = $"⚠  Player \"{name}\" is already registered.";
            return;
        }

        // Add player to the tournament
        var newPlayer = new Player { Name = name };
        _tournament.Players.Add(newPlayer);

        // Rebuild standings and refresh notifications
        BuildStandings();
        OnPropertyChanged(nameof(CycleHeader));
        OnPropertyChanged(nameof(IsFinished));
        OnPropertyChanged(nameof(WinnerName));

        try
        {
            DatabaseRepository.SaveTournamentState(_tournament);
            StatusMessage = $"✓  Added new player: \"{name}\". They will enter the schedule in the next cycle.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠  Added \"{name}\" locally, but database save failed: {ex.Message}";
        }

        IsAddingPlayer = false;
        NewPlayerName = string.Empty;
    }

    private void ExportStandingsPng(object? parameter)
    {
        if (parameter is FrameworkElement element)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png",
                Title = "Export Standings Leaderboard as PNG",
                FileName = $"Standings_{_tournament.SessionName.Replace(" ", "_").Replace("-", "_").Replace(".", "_")}.png"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    PngExporter.Export(element, saveFileDialog.FileName);
                    MessageBox.Show($"Standings successfully exported to:\n{saveFileDialog.FileName}", "Export Succeeded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not export standings:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Could not locate the standings component to export.", "Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ExportBracketPng(object? parameter)
    {
        if (parameter is FrameworkElement element)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png",
                Title = "Export Tournament Bracket as PNG",
                FileName = $"Bracket_{_tournament.SessionName.Replace(" ", "_").Replace("-", "_").Replace(".", "_")}.png"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    PngExporter.Export(element, saveFileDialog.FileName);
                    MessageBox.Show($"Bracket successfully exported to:\n{saveFileDialog.FileName}", "Export Succeeded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not export bracket:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Could not locate the bracket component to export.", "Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ShowUserProfile(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname) || nickname.Equals("BYE", StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            var profile = DatabaseRepository.GetUserProfile(nickname);
            var window = new Views.UserProfileWindow(profile, isTournamentActive: true);
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load user profile:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
