using FightingTournament.Models;

namespace FightingTournament.ViewModels;

public class PlayerStatsViewModel : BaseViewModel
{
    private readonly Player _player;

    private int _rank;
    public int Rank
    {
        get => _rank;
        set => Set(ref _rank, value);
    }

    public string Name       => _player.Name;
    public int    Wins       => _player.TotalWins;
    public int    Losses     => _player.TotalLosses;
    public int    Matches    => _player.TotalMatches;
    public string WinRate    => $"{_player.WinRate:F1}%";
    public string MostPicked => _player.MostPickedCharacter;

    public PlayerStatsViewModel(Player player, int rank = 0)
    {
        _player = player;
        _rank   = rank;
    }

    /// <summary>Called by TournamentViewModel after each cycle commit.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Wins));
        OnPropertyChanged(nameof(Losses));
        OnPropertyChanged(nameof(Matches));
        OnPropertyChanged(nameof(WinRate));
        OnPropertyChanged(nameof(MostPicked));
    }
}
