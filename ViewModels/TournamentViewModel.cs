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

    // ── Header ───────────────────────────────────────────────────────

    public string CycleHeader =>
        $"Cycle  {_tournament.CurrentCycleIndex + 1}   •   " +
        $"{ActiveCount()} active players   •   " +
        $"{_tournament.CurrentCycle?.Matches.Count ?? 0} matches";

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

    // ── Constructor ──────────────────────────────────────────────────

    public TournamentViewModel(Tournament tournament, Action onNewTournament)
    {
        _tournament      = tournament;
        _onNewTournament = onNewTournament;

        CommitCycleCommand   = new RelayCommand(CommitCycle);
        NewTournamentCommand = new RelayCommand(onNewTournament);

        BuildStandings();
        RefreshScheduleSidebar();
        LoadCurrentCycleMatches();
    }

    // ── Build helpers ─────────────────────────────────────────────────

    private void BuildStandings()
    {
        Standings.Clear();
        int rank = 1;
        foreach (var p in _tournament.Players)
            Standings.Add(new PlayerStatsViewModel(p, rank++, OnEliminatePlayer));
    }

    private void RefreshScheduleSidebar()
    {
        CycleSchedule.Clear();
        int current = _tournament.CurrentCycleIndex;

        for (int i = 0; i < _tournament.Cycles.Count; i++)
        {
            CycleSchedule.Add(new CycleInfoViewModel(
                _tournament.Cycles[i],
                isCurrent:   i == current,
                isCompleted: i < current));
        }
    }

    private void LoadCurrentCycleMatches()
    {
        CurrentMatches.Clear();

        var cycle = _tournament.CurrentCycle;
        if (cycle is null) return;
        foreach (var m in cycle.Matches)
            CurrentMatches.Add(new MatchRowViewModel(m, _tournament.SelectedGame));
    }

    // ── Commit ───────────────────────────────────────────────────────

    private void CommitCycle()
    {
        if (CurrentMatches.Any(m => !m.IsCompleted))
        {
            StatusMessage = "⚠  Fill in all match results before saving.";
            return;
        }

        int savedNumber = _tournament.CurrentCycleIndex + 1;

        if (!TournamentEngine.CommitCurrentCycle(_tournament))
        {
            StatusMessage = "⚠  Could not save cycle — check results.";
            return;
        }

        // Refresh standings + re-sort
        foreach (var s in Standings) s.Refresh();

        var sorted = Standings
            .OrderByDescending(s => s.Wins)
            .ThenByDescending(s => s.Matches)
            .ThenBy(s => s.Name)
            .ToList();

        Standings.Clear();
        int rank = 1;
        foreach (var s in sorted) { s.Rank = rank++; Standings.Add(s); }

        RefreshScheduleSidebar();
        LoadCurrentCycleMatches();

        StatusMessage = $"✓  Cycle {savedNumber} saved.  Starting Cycle {savedNumber + 1}…";
        OnPropertyChanged(nameof(CycleHeader));
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

        StatusMessage = $"✗  {player.Name} eliminated. " +
                        $"{ActiveCount()} players remaining.";
        OnPropertyChanged(nameof(CycleHeader));
    }
}
