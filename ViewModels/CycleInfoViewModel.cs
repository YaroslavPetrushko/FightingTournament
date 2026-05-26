using FightingTournament.Models;
using System.Linq;

namespace FightingTournament.ViewModels;

public class CycleInfoViewModel : BaseViewModel
{
    private readonly Cycle _cycle;

    public int    Number   => _cycle.Number;
    public string Matchups => string.Join("\n",
        _cycle.Matches.Select(m => $"{m.Player1.Name}  vs  {m.Player2.Name}"));

    public string MatchCount =>
        $"{_cycle.Matches.Count} match{(_cycle.Matches.Count == 1 ? "" : "es")}";

    private bool _isCurrent;
    public bool IsCurrent
    {
        get => _isCurrent;
        set => Set(ref _isCurrent, value);
    }

    private bool _isCompleted;
    public bool IsCompleted
    {
        get => _isCompleted;
        set => Set(ref _isCompleted, value);
    }

    /// Visual indicator shown in the list: ✓ / ► / ○
    public string StatusGlyph =>
        IsCompleted ? "✓" :
        IsCurrent   ? "►" : "○";

    public CycleInfoViewModel(Cycle cycle, bool isCurrent, bool isCompleted)
    {
        _cycle       = cycle;
        _isCurrent   = isCurrent;
        _isCompleted = isCompleted;
    }

    public void Refresh(bool isCurrent, bool isCompleted)
    {
        IsCurrent   = isCurrent;
        IsCompleted = isCompleted;
        OnPropertyChanged(nameof(StatusGlyph));
        OnPropertyChanged(nameof(Matchups));
        OnPropertyChanged(nameof(MatchCount));
    }
}
