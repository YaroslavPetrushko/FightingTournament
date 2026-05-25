using FightingTournament.Models;

namespace FightingTournament.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentView = null!;

    public BaseViewModel CurrentView
    {
        get => _currentView;
        private set => Set(ref _currentView, value);
    }

    public MainViewModel() => NavigateToSetup();

    private void NavigateToSetup()
    {
        var setup = new SetupViewModel();
        setup.TournamentStarted += NavigateToTournament;
        CurrentView = setup;
    }

    private void NavigateToTournament(Tournament tournament)
    {
        var t = new TournamentViewModel(tournament, NavigateToSetup);
        CurrentView = t;
    }
}
