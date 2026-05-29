# ⚔ Fighting Tournament Tracker

A WPF desktop application for running and scoring local fighting game tournaments.  
Built with **.NET 8 + WPF**, using pure MVVM architecture and dynamic theme design system.

---

## 🚀 Key Features (v5.1)

| Core Module               | Key Features & Capabilities                                                                                                                                         | Status |
|---------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------:|
| **Engine & Formats**      | Switch between Endless **Round-Robin** lobbies (Circle Algorithm) and visual **Championship Single Elimination** Bracket Trees (with auto-handled `"BYE"` matches). |   ✅    |
| **Active Analytics**      | Live leaderboard sorting (W/L ratio, win rate %, top characters), dynamic **mid-tournament pruning / additions**, and retro-active **history correction**.          |   ✅    |
| **Team & Character Play** | Predefined rosters for major titles with **auto-learning autocompletes** and **multi-character pick inputs** (e.g., UMvC3 team support via delimiters `;` or `/`).  |   ✅    |
| **Custom Theme Engine**   | 6 swappable visual themes (Default Black/Red, Volt Green, Retro Electric Blue, Deep Dark Purple, Minimalist, White Light Mode) stored dynamically in user settings. |   ✅    |
| **Visual Media Exporter** | One-click **cropped PNG screenshot export** of bracket trees and standings tables without margins or empty canvas spaces.                                           |   ✅    |
| **Robust Persistence**    | SQLite database storage for global player registries, setups, active sessions, and settings, allowing **resuming and editing**.                                     |   ✅    |
| **UX Styling**            | Adaptive **scaling up to 2K**, soft entry transitions, transparent borderless window chrome with a customized corner glow, and stylized dark tooltips.              |   ✅    |

---

## 📁 Project Structure

```
FightingTournament/
├── Converters/
│   ├── BoolToVisibilityConverter.cs  — WPF boolean to visibility converter
│   ├── InverseBooleanConverter.cs     — WPF boolean negation converter
│   └── StringToVisibilityConverter.cs — WPF string presence to visibility converter
├── Models/
│   ├── Cycle.cs                  — Represent a tournament round
│   ├── GameDatabase.cs           — Roster definitions & default autocompletes
│   ├── Match.cs                  — Individual matchup with scoring & character logs
│   ├── Player.cs                 — Player record, statistics & global info
│   ├── Tournament.cs             — State machine tracking active sessions
│   └── UserPreset.cs             — Saved game-mode setup presets
├── Services/
│   ├── DatabaseConnector.cs      — SQLite database initializer & connection helper
│   ├── DatabaseRepository.cs     — Persistence CRUD operations (sessions, setups, settings)
│   ├── PngExporter.cs            — Visual WPF component to cropped PNG renderer
│   ├── ThemeManager.cs           — Swappable UI color palettes controller
│   └── TournamentEngine.cs       — Pairings scheduler & brackets layout planner
├── ViewModels/
│   ├── BaseViewModel.cs          — MVVM property changed base notifier
│   ├── CycleInfoViewModel.cs     — Timeline cycle navigation sidebar
│   ├── MainViewModel.cs          — Application root & page-router
│   ├── MatchRowViewModel.cs      — Per-match interactive scoring row
│   ├── PlayerNameEntry.cs        — Setup form player list node
│   ├── PlayerStatsViewModel.cs   — Leaderboard analytics entry
│   ├── RelayCommand.cs           — WPF action executor framework
│   ├── SetupViewModel.cs         — Setup view logic, presets & registries
│   └── TournamentViewModel.cs    — Main scoreboard, brackets & visual grid controller
├── Views/
│   ├── AboutWindow.xaml          — Details, system version & credit popup
│   ├── SetupView.xaml            — Configuration & tournament initializer screen
│   └── TournamentView.xaml       — Dashboard, bracket canvas & control panel screen
├── App.xaml                      — Custom dark/theme XAML resources, control styles & triggers
└── MainWindow.xaml               — Frame-less main application shell container
```

---

## 🔮 Future Extensions

- **Global Roster & Autocomplete Editor**: A specialized management dashboard to edit base rosters and learned character names without entering active tournament sessions.
- **Detailed Player Profiles & Analytics**: View performance graphs, head-to-head match histories, and favorite game trends across all sessions.


---

## 🚀 Build & Run

### Prerequisites
* Windows OS
* [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Run from Command Line
```bash
# Clone the repository and navigate to root
cd FightingTournament

# Build and run the project
dotnet run
```

Alternatively, open `FightingTournament.sln` inside Visual Studio 2022+ or JetBrains Rider for full IDE support.

---

## 🧮 Circle Algorithm (Round-Robin)

The round-robin pairings are generated using the **circle (polygon) rotation method**:
* `N` players → `N - 1` cycles (if `N` is even) or `N` cycles with one player receiving a `BYE` per round (if `N` is odd).
* Each cycle contains `⌊N / 2⌋` active matchups.
* One player remains fixed at position 0, while all other players rotate clockwise each round to guarantee unique pairings.
* `BYE` matches are automatically skipped and pruned from the visual schedule dashboard.

---

#### Developed using Antigravity AI (Google Gemini 3.5 Flash).
