# ⚔ Fighting Tournament Tracker

A WPF desktop application for running and scoring local fighting game tournaments.  
Built with **.NET 8 + WPF**, using pure MVVM architecture and high-fidelity custom design systems.

---

## 🚀 Features (v5.0 — current state)

| Feature                                  | Description                                                                                                     | Status |
|------------------------------------------|-----------------------------------------------------------------------------------------------------------------|:------:|
| **Tournament Modes Selection**           | Switch between Endless Round-Robin lobbies or classic Championship Single Elimination Bracket modes.            |   ✅    |
| **Championship Bracket Engine**          | Standard SE power-of-2 visual tree seeding with auto-completed virtual `"BYE"` match-ups.                       |   ✅    |
| **Visual Scrollable Bracket Tree**       | Horizontal dashboard column-by-column flow highlighting winners with trophies, dimming losers.                  |   ✅    |
| **Knockout Standing Analytics**          | Leaderboard ranks active survivors first, then ranks eliminated players based on the round they lost in.        |   ✅    |
| **Winner Celebration Victory Screen**    | Centered banner celebrating the crowned champion with icons.                                                    |   ✅    |
| **Smart Session Auto-Naming**            | Dynamically formats session names as `"Date - Mode - Game"` to prevent daily multi-game conflicts.              |   ✅    |
| **UI Spacing & Fit Optimization**        | Single-row stylized brand headers; optimized margins ensuring the start button fits on all screen sizes.        |   ✅    |
| **Micro-Animations (Page Transitions)**  | Soft page entry slide and fade animations when changing views for a pleasant user experience.                   |   ✅    |
| **Round-robin Schedule**                 | Generates pairing matching using the circle algorithm.                                                          |   ✅    |
| **Dynamic Player Setup**                 | Custom player counts (2–16) with unique validation checks.                                                      |   ✅    |
| **Select Game Presets**                  | Select a game at setup to populate game-specific character autocompletes.                                       |   ✅    |
| **Rich Character Databases**             | Predefined rosters for Tekken 8, Guilty Gear -Strive-, SF6, MK1, Smash Ultimate, UMvC 3 and DBFZ.               |   ✅    |
| **Custom Dark Theme**                    | Custom dark mode styling for all inputs, buttons, sliders, text boxes, and scrollbars.                          |   ✅    |
| **High-Legibility ToolTips**             | Custom dark-themed popover tooltips with crisp white text.                                                      |   ✅    |
| **Interactive ComboBox Templates**       | Completely custom-templated dropdown inputs using toggle overlays for flawless UX.                              |   ✅    |
| **Per-match Scoring & Winner Selection** | Tap "WIN" pills to instantly allocate scores and update rankings.                                               |   ✅    |
| **Live Standings & Re-sorting**          | Live leaderboard tracking W/L records, win rate percentage, and most picked character.                          |   ✅    |
| **Mid-cycle Player Elimination**         | Eject players mid-tournament while preserving completed matches and pruning unplayed matchups.                  |   ✅    |
| **Mid-Tournament Player Additions**      | Sleek Endless-mode button panel docked inside standings sidebar to add new players in upcoming cycles.          |   ✅    |
| **SQLite Session Persistence**           | Automatically persists matches, character picks, standings, and cycles to a local SQLite database file.         |   ✅    |
| **Session Resume & Deletion**            | Seamless dashboard to select a saved session, resume it, or delete it with a safety confirmation dialog.        |   ✅    |
| **Global Player Directory**              | Maintains unique player nicknames globally across tournaments to track overall player participation.            |   ✅    |
| **Quick-Add Registered Players**         | Click-to-add sidebar in setup: autofills generic slots or expands the roster automatically up to 16 players.    |   ✅    |
| **Edit Completed Cycles**                | Select completed cycles in history panel to reload results for live correction; standings updates instantly.    |   ✅    |
| **Custom Setup Presets**                 | Save current tournament setup parameters (mode, game, rounds, player names) for quick loading and deleting.     |   ✅    |
| **Editable Game Selector**               | Game dropdown is fully editable, automatically registering new typed game names in the permanent registry.      |   ✅    |
| **Character Auto-Learning**              | Auto-learns newly entered character names on round commits, instantly updating autocomplete suggestions.        |   ✅    |
| **Match-Level Round Settings**           | Configure individual rounds count (2, 3, 4, 5) per match pair; Championship mode supports static default setup. |   ✅    |
| **Visual Media PNG Exporter**            | Direct one-click PNG screenshots of Standings and Bracket trees, tightly cropped to eliminate empty space.      |   ✅    |
| **Rounded Window Chrome & Glow**         | Transparent layout with a custom border (`CornerRadius="8"`) and a soft, dimmed active outline.                 |   ✅    |
| **Top Outline Omission & Corner Inset**  | Border thickness excludes top edge to frame gradient accents; 1.5px inset mask prevents grid overlap clipping.  |   ✅    |
| **Adaptive Dashboard Scaling**           | Proportional layout grids that dynamically expand names, standings columns, and character dropdowns up to 2K.   |   ✅    |
| **Global Player Roster Deletion**        | Right-click registered players sidebar in setup to cleanly prompt and delete players from the global registry.  |   ✅    |

---

## 🗺️ Planned Milestones

### ⚙️ Milestone 4: QOL & Custom Preset Editor [COMPLETED]
- **Saved User Presets:** Save setup configurations (game, mode, player count, nicknames, default rounds) in `UserPresets` table.
- **Editable Game ComboBox:** Dynamically register custom game names in database upon starting tournaments.
- **Hybrid Autocomplete & Learning:** Auto-learn newly typed character names on commits and merge with predefined rosters dynamically.
- **Customizable Rounds:** Adjust individual match rounds count (2\3\4\5) on current active cycles; disabled inside Championship mode.
- **Roster Ramps:** Full 50-character list for Ultimate Marvel Vs. Capcom 3, 41-character DBFZ, and recent SF6/MK1 DLC additions.
- **Mid-Tournament Additions:** Sleek Endless Mode card button enabling player additions inside upcoming cycles.

### 📊 Milestone 5: Advanced Analytics & Polish [COMPLETED]
- **Performance Charting:** Character pick-rates, win rate rankings, and progress tracking on setup/tournament layers.
- **Media Exporter:** High-performance, tightly shrink-wrapped PNG visual exports of standings and single-elimination tournament brackets.
- **Rounded Windows Chrome & Active Outline**: Custom transparent window styling with rounded borders, a dimmed premium crimson active outline, and top edge omission.
- **Content Inset Masking**: 1.5px inset padding mask that prevents child elements from overlapping the rounded corner curves.
- **Adaptive Grid Scaling**: Proportional grid columns across Left (schedule DockPanel), Right (star-sized player/character), and Center (expanded scoring dropdowns and player names) panels to support HD, Full HD, and 2K screens.
- **Global Player Directory Maintenance**: Left-click to quick-add, right-click to prompt confirmation and delete registered players globally.

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

#### Developed using Antigravity AI (Google Gemini 3.5 Flash).
