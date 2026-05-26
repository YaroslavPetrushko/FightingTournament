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

    // ── Commands ─────────────────────────────────────────────────────

    public ICommand CommitCycleCommand   { get; }
    public ICommand NewTournamentCommand { get; }
    public ICommand SelectCycleCommand   { get; }

    // ── Constructor ──────────────────────────────────────────────────

    public TournamentViewModel(Tournament tournament, Action onNewTournament)
    {
        _tournament         = tournament;
        _onNewTournament    = onNewTournament;
        _selectedCycleIndex = tournament.CurrentCycleIndex;

        CommitCycleCommand   = new RelayCommand(CommitCycle);
        NewTournamentCommand = new RelayCommand(onNewTournament);
        SelectCycleCommand   = new RelayCommand(p => SelectCycle((CycleInfoViewModel)p!));

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
                CurrentMatches.Add(new MatchRowViewModel(m, _tournament.SelectedGame));
            }
        }
    }

    // ── Commit ───────────────────────────────────────────────────────

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
                    if (m.IsCompleted)
                    {
                        bool p1Won = m.WinnerId == 1;
                        m.Player1.RecordResult(p1Won,  m.Character1);
                        m.Player2.RecordResult(!p1Won, m.Character2);
                    }
                }
            }

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
        RefreshScheduleSidebar();
        LoadCurrentCycleMatches();

        try
        {
            DatabaseRepository.SaveTournamentState(_tournament);
            StatusMessage = $"✓  Cycle {savedNumber} saved to database. Starting Cycle {savedNumber + 1}…";
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠  Cycle {savedNumber} saved locally, but database save failed: {ex.Message}";
        }
        OnPropertyChanged(nameof(CycleHeader));
    }

    private void SelectCycle(CycleInfoViewModel cycleVm)
    {
        if (cycleVm is null) return;

        int index = _tournament.Cycles.IndexOf(cycleVm.CycleModel);
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
    }
}
