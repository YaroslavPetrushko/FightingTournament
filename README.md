# ⚔ Fighting Tournament Tracker

A WPF desktop application for running and scoring local fighting game tournaments.  
Built with **.NET 8 + WPF**, using pure MVVM architecture and high-fidelity custom design systems.

---

## 🚀 Features (v3.0 — current state)

| Feature                                  | Description                                                                                                   | Status |
|------------------------------------------|---------------------------------------------------------------------------------------------------------------|:------:|
| **Tournament Modes Selection**           | Switch between Endless Round-Robin lobbies or classic Championship Single Elimination Bracket modes.          |   ✅    |
| **Championship Bracket Engine**          | Standard SE power-of-2 visual tree seeding with auto-completed virtual `"BYE"` match-ups.                     |   ✅    |
| **Visual Scrollable Bracket Tree**       | Horizontal dashboard column-by-column flow highlighting winners with trophies, dimming losers.                |   ✅    |
| **Knockout Standing Analytics**          | Leaderboard ranks active survivors first, then ranks eliminated players based on the round they lost in.      |   ✅    |
| **Winner Celebration Victory Screen**    | Centered banner celebrating the crowned champion with icons.                                                  |   ✅    |
| **Smart Session Auto-Naming**            | Dynamically formats session names as `"Date - Mode - Game"` to prevent daily multi-game conflicts.            |   ✅    |
| **UI Spacing & Fit Optimization**        | Single-row stylized brand headers; optimized margins ensuring the start button fits on all screen sizes.      |   ✅    |
| **Micro-Animations (Page Transitions)**  | Soft page entry slide and fade animations when changing views for a pleasant user experience.                 |   ✅    |
| **Round-robin Schedule**                 | Generates pairing matching using the circle algorithm.                                                        |   ✅    |
| **Dynamic Player Setup**                 | Custom player counts (2–16) with unique validation checks.                                                    |   ✅    |
| **Select Game Presets**                  | Select a game at setup to populate game-specific character autocompletes.                                     |   ✅    |
| **Rich Character Databases**             | Predefined rosters for Tekken 8, Guilty Gear -Strive-, Street Fighter 6, Mortal Kombat 1, and Smash Ultimate. |   ✅    |
| **Custom Dark Theme**                    | Custom dark mode styling for all inputs, buttons, sliders, text boxes, and scrollbars.                        |   ✅    |
| **High-Legibility ToolTips**             | Custom dark-themed popover tooltips with crisp white text.                                                    |   ✅    |
| **Interactive ComboBox Templates**       | Completely custom-templated dropdown inputs using toggle overlays for flawless UX.                            |   ✅    |
| **Per-match Scoring & Winner Selection** | Tap "WIN" pills to instantly allocate scores and update rankings.                                             |   ✅    |
| **Live Standings & Re-sorting**          | Live leaderboard tracking W/L records, win rate percentage, and most picked character.                        |   ✅    |
| **Mid-cycle Player Elimination**         | Eject players mid-tournament while preserving completed matches and pruning unplayed matchups.                |   ✅    |
| **SQLite Session Persistence**           | Automatically persists matches, character picks, standings, and cycles to a local SQLite database file.       |   ✅    |
| **Session Resume & Deletion**            | Seamless dashboard to select a saved session, resume it, or delete it with a safety confirmation dialog.      |   ✅    |
| **Global Player Directory**              | Maintains unique player nicknames globally across tournaments to track overall player participation.          |   ✅    |
| **Quick-Add Registered Players**         | Click-to-add sidebar in setup: autofills generic slots or expands the roster automatically up to 16 players.  |   ✅    |
| **Edit Completed Cycles**                | Select completed cycles in history panel to reload results for live correction; standings updates instantly.  |   ✅    |

---

## 🗺️ Planned Milestones

### 🎨 Milestone 3: Tournament Modes & UI/UX Aesthetics [COMPLETED]
- **Segmented Toggle Buttons:** Custom styled left/right rounded radio button segmented groups for mode choices and views. [COMPLETED]
- **Winner Victory Screen:** Centered banner celebrating the crowned champion with gold crowns and trophy icons.
- **UI Spacing & Fit Optimization:** Single-row stylized brand headers; optimized margins ensuring the start button fits on all screen sizes.
- **Micro-Animations (Page Transitions):** Soft page entry slide and fade animations when changing views.
- **leaderboard Background Tint:** Subtle dark-red background tint (`BrushElim`) for eliminated players in the standings.
- **Frameless Custom Window:** Replace the standard Windows title bar with a custom title bar matching the `#0D0D15` theme.

### ⚙️ Milestone 4: QOL & Custom Preset Editor
- **Game & Character Creator:** A full in-app management interface where users can add custom games and write down their own character rosters.
- **Save User Presets:** Save local configurations (favorite players, default games, tournament sizes) for instant tournament creation.
- **Intelligent Autocomplete:** Hybrid suggestions that automatically combine the selected game's database with custom player-entered characters.

### 📊 Milestone 5: Advanced Analytics
- **Performance Charting:** Dynamic round-by-round statistics and charts displaying character pick-rates and tournament progress.
- **Media Exporter:** Direct one-click screenshot/image and PDF exports of the final standings, winner podium, and matches logs.

---

## 📁 Project Structure

```
FightingTournament/
├── Models/
│   ├── Player.cs           — Stats accumulation
│   ├── Match.cs            — Single bout representation
│   ├── Cycle.cs            — Round of matches
│   ├── Tournament.cs       — Top-level tournament state
│   └── GameDatabase.cs     — PRESET game rosters (Tekken 8, SF6, GGST, MK1, Smash)
├── Services/
│   ├── DatabaseConnector.cs  — Singleton connector to SQLite database [NEW]
│   ├── DatabaseRepository.cs — Session state loading, saving, and player indexing [NEW]
│   └── TournamentEngine.cs   — Schedule builder + cycle commit logic
├── ViewModels/
│   ├── BaseViewModel.cs
│   ├── RelayCommand.cs
│   ├── MainViewModel.cs        — Navigation root
│   ├── SetupViewModel.cs       — Setup screen logic & game choice selection
│   ├── TournamentViewModel.cs  — Main tournament screen logic
│   ├── MatchRowViewModel.cs    — One match row (dynamic character lists + winner)
│   ├── PlayerStatsViewModel.cs — Leaderboard rows
│   ├── CycleInfoViewModel.cs   — Left-panel schedule item
│   └── PlayerNameEntry.cs      — Setup list entry
├── Views/
│   ├── SetupView.xaml          — Player count & select game input screen
│   └── TournamentView.xaml     — Schedule | matches | standings main dashboard
├── Converters/
│   ├── BoolToVisibilityConverter.cs
│   └── StringToVisibilityConverter.cs
├── App.xaml                    — Custom dark UI styles, custom templates & DataTemplates
└── MainWindow.xaml             — Main window container with shell ContentControl
```

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

#### Developed in collaboration with Antigravity AI (Google DeepMind).
