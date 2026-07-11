using System;
using System.Windows;
using System.Windows.Input;
using FightingTournament.Services;

namespace FightingTournament.Views;

public partial class AssignProfileWindow : Window
{
    public AssignProfileWindow()
    {
        InitializeComponent();

        LoadPlayers();
    }

    private void LoadPlayers()
    {
        try
        {
            var registeredUsers = DatabaseRepository.GetRegisteredUsers();
            ComboPlayers.ItemsSource = registeredUsers;

            string? assigned = ProfileManager.GetAssignedPlayerName();
            if (assigned != null && registeredUsers.Contains(assigned))
            {
                ComboPlayers.SelectedItem = assigned;
                BtnUnassign.IsEnabled = true;
            }
            else
            {
                BtnUnassign.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to load players: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void TitleArea_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnUnassign_Click(object sender, RoutedEventArgs e)
    {
        ProfileManager.AssignPlayer(null);
        DialogResult = true;
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        string? selected = ComboPlayers.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(selected))
        {
            MessageBox.Show(
                "Please select a player name to assign as your profile.",
                "Selection Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        ProfileManager.AssignPlayer(selected);
        DialogResult = true;
        Close();
    }
}
