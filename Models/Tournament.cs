using System;
using System.Collections.Generic;
using System.Linq;

namespace FightingTournament.Models;

public enum TournamentMode
{
    Endless,
    Championship
}

public class Tournament
{
    public List<Player> Players { get; } = new();
    public List<Cycle>  Cycles  { get; } = new();

    public string SelectedGame { get; set; } = "Tekken 8";
    public string SessionName  { get; set; } = string.Empty;

    public int CurrentCycleIndex { get; set; } = 0;
    public TournamentMode Mode   { get; set; } = TournamentMode.Endless;

    public Cycle? CurrentCycle =>
        CurrentCycleIndex < Cycles.Count ? Cycles[CurrentCycleIndex] : null;

    // Resolves to true when cycles are empty (edge-case) or if championship round is fully completed.
    public bool IsFinished
    {
        get
        {
            if (Mode == TournamentMode.Championship)
            {
                if (Cycles.Count > 0)
                {
                    var lastCycle = Cycles.Last();
                    if (lastCycle.Matches.Count == 1 && lastCycle.IsCompleted && CurrentCycleIndex >= Cycles.Count)
                    {
                        return true;
                    }
                }
                return false;
            }
            return Cycles.Count == 0;
        }
    }
}
