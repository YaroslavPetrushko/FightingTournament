using System;
using System.Windows;
using System.Windows.Input;
using FightingTournament.Models;
using FightingTournament.Services;

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
        PlayerTitleText.Text = $"{LocalizationManager.GetString("Loc_ProfilePrefix")} {profile.Nickname.ToUpper(System.Globalization.CultureInfo.CurrentCulture)}";
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
            BtnDelete.ToolTip = LocalizationManager.GetString("Loc_CannotDeleteTooltip");
        }

        UpdateAssignmentState();
    }

    private void UpdateAssignmentState()
    {
        bool isAssigned = ProfileManager.IsAssigned(_profile.Nickname);
        if (isAssigned)
        {
            PlayerTitleText.Text = $"★ {LocalizationManager.GetString("Loc_ProfilePrefix")} {_profile.Nickname.ToUpper(System.Globalization.CultureInfo.CurrentCulture)}";
            PlayerSubtitleText.Text = LocalizationManager.GetString("Loc_MenuMyProfile").ToUpper(System.Globalization.CultureInfo.CurrentCulture);
            BtnToggleAssignment.Content = LocalizationManager.GetString("Loc_UnassignMyProfile");
        }
        else
        {
            PlayerTitleText.Text = $"{LocalizationManager.GetString("Loc_ProfilePrefix")} {_profile.Nickname.ToUpper(System.Globalization.CultureInfo.CurrentCulture)}";
            PlayerSubtitleText.Text = LocalizationManager.GetString("Loc_ProfileSubtitle");
            BtnToggleAssignment.Content = LocalizationManager.GetString("Loc_AssignAsMyProfile");
        }
    }

    private void BtnToggleAssignment_Click(object sender, RoutedEventArgs e)
    {
        bool isAssigned = ProfileManager.IsAssigned(_profile.Nickname);
        if (isAssigned)
        {
            ProfileManager.AssignPlayer(null);
        }
        else
        {
            ProfileManager.AssignPlayer(_profile.Nickname);
        }
        UpdateAssignmentState();
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
            string.Format(LocalizationManager.GetString("Loc_DeleteUserConfirm"), _profile.Nickname),
            LocalizationManager.GetString("Loc_DeleteUserTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                DatabaseRepository.PurgeUserCompletely(_profile.Nickname);
                _onDeleted?.Invoke();
                MessageBox.Show(
                    string.Format(LocalizationManager.GetString("Loc_UserPurgedMsg"), _profile.Nickname),
                    LocalizationManager.GetString("Loc_UserPurgedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.GetString("Loc_DeleteUserError")} {ex.Message}",
                    LocalizationManager.GetString("Loc_DatabaseError"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
