using FightingTournament.Models;
using System;
using System.Windows.Input;

namespace FightingTournament.ViewModels;

public class PlayerStatsViewModel : BaseViewModel
{
    // Expose model reference so TournamentViewModel can match by reference
    public Player PlayerModel { get; }

    private int _rank;
    public int Rank
    {
        get => _rank;
        set => Set(ref _rank, value);
    }

    private bool _isEliminated;
    public bool IsEliminated
    {
        get => _isEliminated;
        set => Set(ref _isEliminated, value);
    }

    public string Name       => PlayerModel.Name;
    public int    Wins       => PlayerModel.TotalWins;
    public int    Losses     => PlayerModel.TotalLosses;
    public int    Matches    => PlayerModel.TotalMatches;
    public string WinRate    => $"{PlayerModel.WinRate:F1}%";
    public string MostPicked => PlayerModel.MostPickedCharacter;

    public ICommand EliminateCommand { get; }

    public PlayerStatsViewModel(Player player, int rank, Action<Player> onEliminate)
    {
        PlayerModel      = player;
        _rank            = rank;
        _isEliminated    = player.IsEliminated;
        EliminateCommand = new RelayCommand(
            () => onEliminate(PlayerModel),
            () => !PlayerModel.IsEliminated);
    }

    /// <summary>Called by TournamentViewModel after each cycle commit.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Wins));
        OnPropertyChanged(nameof(Losses));
        OnPropertyChanged(nameof(Matches));
        OnPropertyChanged(nameof(WinRate));
        OnPropertyChanged(nameof(MostPicked));
        IsEliminated = PlayerModel.IsEliminated;
    }
}
