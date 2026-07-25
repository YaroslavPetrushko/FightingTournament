using System.Windows.Controls;

namespace FightingTournament.Views;

public partial class TournamentView : UserControl
{
    public TournamentView()
    {
        InitializeComponent();
        Unloaded += (s, e) => (DataContext as System.IDisposable)?.Dispose();
    }
}
