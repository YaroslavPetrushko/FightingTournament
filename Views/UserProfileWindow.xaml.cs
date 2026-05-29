using FightingTournament.Models;
using FightingTournament.Services;
using System;
using System.Windows;
using System.Windows.Input;

namespace FightingTournament.Views;

public partial class UserProfileWindow : Window
{
    private readonly UserProfileInfo _profile;
    private readonly Action? _onDeleted;

    public UserProfileWindow(UserProfileInfo profile, bool isTournamentActive, Action? onDeleted = null)
    {
        InitializeComponent();

        _profile = profile;
        _onDeleted = onDeleted;

        // Populate text boxes
        PlayerTitleText.Text = $"PROFILE: {profile.Nickname.ToUpper()}";
        TxtTotalMatches.Text = profile.TotalMatches.ToString();
        TxtTotalWins.Text = profile.TotalWins.ToString();
        TxtWinRate.Text = $"{profile.WinRate:F1}%";
        TxtFavoriteCharacter.Text = profile.FavoriteCharacter;
        TxtFavoriteGame.Text = profile.FavoriteGame;
        TxtLastMatchDate.Text = profile.LastMatchDate;

        // Block deletion if tournament is active
        if (isTournamentActive)
        {
            BtnDelete.IsEnabled = false;
            BtnDelete.ToolTip = "Cannot delete user during active tournament.";
        }
    }

    private void TitleArea_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            $"Are you sure you want to completely delete user \"{_profile.Nickname}\" and all their historical match records from the database?\n\nThis action cannot be undone and will affect standings of tournaments they participated in.",
            "Delete User Permanently",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                DatabaseRepository.PurgeUserCompletely(_profile.Nickname);
                _onDeleted?.Invoke();
                MessageBox.Show($"User \"{_profile.Nickname}\" and all their records have been successfully purged.", "User Purged", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete user:\n{ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
