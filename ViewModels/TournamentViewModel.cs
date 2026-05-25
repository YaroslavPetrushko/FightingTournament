using FightingTournament.Models;
using FightingTournament.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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

    // ── Header state ─────────────────────────────────────────────────

    public string CycleHeader =>
        _tournament.IsFinished
            ? $"Tournament Complete  •  {_tournament.Cycles.Count} cycles played"
            : $"Cycle  {_tournament.CurrentCycleIndex + 1}  /  {_tournament.Cycles.Count}";

    private bool _isFinished;
    public bool IsFinished
    {
        get => _isFinished;
        private set => Set(ref _isFinished, value);
    }

    // ── Status message ───────────────────────────────────────────────

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        private set => Set(ref _statusMessage, value);
    }

    // ── Commands ─────────────────────────────────────────────────────

    public ICommand CommitCycleCommand    { get; }
    public ICommand NewTournamentCommand  { get; }

    // ── Constructor ──────────────────────────────────────────────────

    public TournamentViewModel(Tournament tournament, Action onNewTournament)
    {
        _tournament      = tournament;
        _onNewTournament = onNewTournament;

        CommitCycleCommand   = new RelayCommand(CommitCycle);
        NewTournamentCommand = new RelayCommand(onNewTournament);

        BuildStandings();
        BuildCycleSchedule();
        LoadCurrentCycleMatches();
    }

    // ── Initialization ───────────────────────────────────────────────

    private void BuildStandings()
    {
        Standings.Clear();
        int rank = 1;
        foreach (var p in _tournament.Players)
            Standings.Add(new PlayerStatsViewModel(p, rank++));
    }

    private void BuildCycleSchedule()
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

        foreach (var match in cycle.Matches)
            CurrentMatches.Add(new MatchRowViewModel(match));
    }

    // ── Commit ───────────────────────────────────────────────────────

    private void CommitCycle()
    {
        if (_tournament.IsFinished) return;

        if (CurrentMatches.Any(m => !m.IsCompleted))
        {
            StatusMessage = "⚠  Fill in all match results before saving.";
            return;
        }

        int completedNumber = _tournament.CurrentCycleIndex + 1;

        bool ok = TournamentEngine.CommitCurrentCycle(_tournament);
        if (!ok)
        {
            StatusMessage = "⚠  Could not save cycle — check results.";
            return;
        }

        // Refresh standings + re-sort by wins desc
        foreach (var s in Standings) s.Refresh();

        var sorted = Standings.OrderByDescending(s => s.Wins)
                               .ThenByDescending(s => s.Matches)
                               .ThenBy(s => s.Name)
                               .ToList();
        Standings.Clear();
        int rank = 1;
        foreach (var s in sorted) { s.Rank = rank++; Standings.Add(s); }

        // Refresh schedule markers
        int newCurrent = _tournament.CurrentCycleIndex;
        for (int i = 0; i < CycleSchedule.Count; i++)
            CycleSchedule[i].Refresh(isCurrent: i == newCurrent, isCompleted: i < newCurrent);

        if (_tournament.IsFinished)
        {
            IsFinished    = true;
            StatusMessage = $"🏆  Tournament complete!  Congratulations to {Standings.First().Name}!";
            CurrentMatches.Clear();
        }
        else
        {
            LoadCurrentCycleMatches();
            StatusMessage = $"✓  Cycle {completedNumber} saved.  Starting Cycle {newCurrent + 1}…";
        }

        OnPropertyChanged(nameof(CycleHeader));
        OnPropertyChanged(nameof(IsFinished));
    }
}
