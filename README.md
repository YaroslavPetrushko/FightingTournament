# ⚔ Fighting Tournament Tracker

A WPF desktop application for running and scoring local fighting-game tournaments.  
Built with **.NET 8 + WPF**, pure MVVM, zero external dependencies.

---

## Features (v1 — base logic)

| Feature                                       | Status |
|-----------------------------------------------|--------|
| Round-robin schedule (circle algorithm)       | ✅      |
| Dynamic player count (2–16)                   | ✅      |
| Per-match winner selection (WIN pills)        | ✅      |
| Per-match character picker (preset + custom)  | ✅      |
| Live standings (W / L / WR% / Most Picked)    | ✅      |
| Cycle-by-cycle commit with result validation  | ✅      |
| Tournament complete detection + winner banner | ✅      |
| New Tournament reset flow                     | ✅      |

## Planned (later milestones)

- [ ] SQLite session save / restore
- [ ] Early player elimination mid-cycles
- [ ] Screenshot / image export
- [ ] More character presets per game

---

## Project structure

```
FightingTournament/
├── Models/
│   ├── Player.cs          — stats accumulation
│   ├── Match.cs           — single bout
│   ├── Cycle.cs           — round of matches
│   └── Tournament.cs      — top-level container
├── Services/
│   └── TournamentEngine.cs — schedule builder + cycle commit
├── ViewModels/
│   ├── BaseViewModel.cs
│   ├── RelayCommand.cs
│   ├── MainViewModel.cs       — navigation root
│   ├── SetupViewModel.cs      — setup screen logic
│   ├── TournamentViewModel.cs — main game screen logic
│   ├── MatchRowViewModel.cs   — one match row (characters + winner)
│   ├── PlayerStatsViewModel.cs
│   ├── CycleInfoViewModel.cs  — left-panel schedule item
│   └── PlayerNameEntry.cs
├── Views/
│   ├── SetupView.xaml         — player count + names
│   └── TournamentView.xaml    — schedule | matches | standings
├── Converters/
│   ├── BoolToVisibilityConverter.cs
│   └── StringToVisibilityConverter.cs
├── App.xaml                   — global styles + DataTemplates
└── MainWindow.xaml            — thin shell ContentControl
```

---

## Build & run

```bash
# Requires .NET 8 SDK + Windows
cd FightingTournament
dotnet run
```

Or open `FightingTournament.sln` (once created) in Visual Studio 2022+.

---

## Schedule algorithm

Uses the **circle (polygon) method** for round-robin:

- `N` players → `N−1` cycles (N even) or `N` cycles with one BYE per round (N odd)  
- Each cycle has `⌊N/2⌋` real matches  
- One player is fixed (index 0); the rest rotate clockwise each round  
- BYE pairings are silently skipped from the UI


#### This project is developed with the assistance of Claude Sonnet 4.6 (Anthropic).
