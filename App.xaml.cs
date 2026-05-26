using FightingTournament.Services;
using System;
using System.Windows;

namespace FightingTournament;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            DatabaseConnector.Instance.InitializeDatabase();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize the database:\n\n{ex.Message}",
                "Database Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            Shutdown();
        }
    }
}
