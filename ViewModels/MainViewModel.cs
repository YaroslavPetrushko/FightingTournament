using FightingTournament.Models;
using FightingTournament.Services;
using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace FightingTournament.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentView = null!;

    public BaseViewModel CurrentView
    {
        get => _currentView;
        private set => Set(ref _currentView, value);
    }

    public ICommand OpenDatabaseCommand   { get; }
    public ICommand SaveDatabaseAsCommand { get; }
    public ICommand CleanSessionsCommand  { get; }
    public ICommand AboutCommand          { get; }
    public ICommand ExitCommand           { get; }
    public ICommand ChangeThemeCommand    { get; }
    public ICommand ChangeLanguageCommand { get; }
    public ICommand OpenProfileCommand    { get; }

    public MainViewModel()
    {
        OpenDatabaseCommand   = new RelayCommand(OpenDatabase);
        SaveDatabaseAsCommand = new RelayCommand(SaveDatabaseAs);
        CleanSessionsCommand  = new RelayCommand(CleanSessions);
        AboutCommand          = new RelayCommand(ShowAbout);
        ExitCommand           = new RelayCommand(ExitApplication);
        ChangeThemeCommand    = new RelayCommand(p => ChangeTheme(p as string));
        ChangeLanguageCommand = new RelayCommand(p => ChangeLanguage(p as string));
        OpenProfileCommand    = new RelayCommand(OpenProfile);

        // Load persisted dynamic theme on startup
        ThemeManager.Initialize();

        ProfileManager.ProfileChanged += () => OnPropertyChanged(nameof(ProfileButtonText));

        NavigateToSetup();
    }

    public string CurrentThemeName => ThemeManager.CurrentTheme;

    public bool IsDefaultThemeChecked => CurrentThemeName == "default";
    public bool IsVoltGreenThemeChecked => CurrentThemeName == "volt_green";
    public bool IsElectricBlueThemeChecked => CurrentThemeName == "electric_blue";
    public bool IsDeepDarkThemeChecked => CurrentThemeName == "deep_dark";
    public bool IsMinimalistThemeChecked => CurrentThemeName == "minimalist";
    public bool IsWhiteThemeChecked => CurrentThemeName == "white";

    public bool IsEnglishLanguageChecked => LocalizationManager.CurrentLanguage == "en";
    public bool IsUkrainianLanguageChecked => LocalizationManager.CurrentLanguage == "ua";

    private void ChangeTheme(string? themeName)
    {
        if (themeName == null) return;
        ThemeManager.ApplyTheme(themeName);
        
        // Notify UI to update checkmarks
        OnPropertyChanged(nameof(CurrentThemeName));
        OnPropertyChanged(nameof(IsDefaultThemeChecked));
        OnPropertyChanged(nameof(IsVoltGreenThemeChecked));
        OnPropertyChanged(nameof(IsElectricBlueThemeChecked));
        OnPropertyChanged(nameof(IsDeepDarkThemeChecked));
        OnPropertyChanged(nameof(IsMinimalistThemeChecked));
        OnPropertyChanged(nameof(IsWhiteThemeChecked));
    }

    private void ChangeLanguage(string? lang)
    {
        if (lang == null) return;
        LocalizationManager.ApplyLanguage(lang);
        
        // Notify UI to update checkmarks
        OnPropertyChanged(nameof(IsEnglishLanguageChecked));
        OnPropertyChanged(nameof(IsUkrainianLanguageChecked));
        OnPropertyChanged(nameof(ProfileButtonText));
    }

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

    private void OpenDatabase()
    {
        if (CurrentView is TournamentViewModel)
        {
            var result = MessageBox.Show(
                LocalizationManager.GetString("Loc_MainDbWarning"),
                LocalizationManager.GetString("Loc_MainDbWarningTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;
        }

        var openFileDialog = new OpenFileDialog
        {
            Filter = "SQLite Database (*.db)|*.db",
            Title = "Open SQLite Database File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                DatabaseConnector.Instance.ChangeDatabasePath(openFileDialog.FileName);
                NavigateToSetup();
                MessageBox.Show($"Successfully switched database to:\n{openFileDialog.FileName}", "Database Changed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open database:\n{ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SaveDatabaseAs()
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "SQLite Database (*.db)|*.db",
            Title = "Save Database Copy As",
            FileName = "FightingTournament_Backup.db"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                DatabaseConnector.Instance.SaveDatabaseCopyAs(saveFileDialog.FileName);
                // Active database stays active, so do NOT call NavigateToSetup() here.
                MessageBox.Show($"Database backup successfully created:\n{saveFileDialog.FileName}", "Database Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save database copy:\n{ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CleanSessions()
    {
        var result = MessageBox.Show(
            LocalizationManager.GetString("Loc_MainCleanConfirm"),
            LocalizationManager.GetString("Loc_MainCleanConfirmTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            int deletedCount = DatabaseRepository.PruneEmptySessions();
            NavigateToSetup();
            MessageBox.Show(
                string.Format(LocalizationManager.GetString("Loc_MainCleanSuccess"), deletedCount),
                LocalizationManager.GetString("Loc_UserPurgedTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{LocalizationManager.GetString("Loc_DeleteUserError")} {ex.Message}", LocalizationManager.GetString("Loc_DatabaseError"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowAbout()
    {
        try
        {
            var aboutWindow = new FightingTournament.Views.AboutWindow();
            aboutWindow.Owner = Application.Current.MainWindow;
            aboutWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not launch About dialog:\n{ex.Message}", "About Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExitApplication()
    {
        var result = MessageBox.Show(
            LocalizationManager.GetString("Loc_MainExitConfirm"),
            LocalizationManager.GetString("Loc_MainExitConfirmTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
        }
    }

    public string ProfileButtonText
    {
        get
        {
            string? name = ProfileManager.GetAssignedPlayerName();
            return name != null 
                ? $"👤  {name}" 
                : $"👤  {LocalizationManager.GetString("Loc_ProfileAssign")}";
        }
    }

    private void OpenProfile()
    {
        string? assigned = ProfileManager.GetAssignedPlayerName();
        if (assigned != null)
        {
            try
            {
                var profile = DatabaseRepository.GetUserProfile(assigned);
                bool isTournamentActive = CurrentView is TournamentViewModel;
                var window = new Views.UserProfileWindow(profile, isTournamentActive);
                window.Owner = Application.Current.MainWindow;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open user profile:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            try
            {
                var window = new Views.AssignProfileWindow();
                window.Owner = Application.Current.MainWindow;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open profile assignment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
