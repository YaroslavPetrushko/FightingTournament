using System.Windows.Controls;

namespace FightingTournament.Views;

public partial class SetupView : UserControl
{
    public SetupView()
    {
        InitializeComponent();
        Unloaded += (s, e) => (DataContext as System.IDisposable)?.Dispose();
    }
}
