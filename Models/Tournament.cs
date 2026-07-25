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
    public List<Cycle> Cycles { get; } = new();

    public string SelectedGame { get; set; } = "Tekken 8";
    public string SessionName { get; set; } = string.Empty;

    public int CurrentCycleIndex { get; set; } = 0;
    public TournamentMode Mode { get; set; } = TournamentMode.Endless;
    public int DefaultRounds { get; set; } = 3;

    public Cycle? CurrentCycle =>
        CurrentCycleIndex < Cycles.Count ? Cycles[CurrentCycleIndex] : null;

    public bool IsFinished
    {
        get
        {
            if (Mode == TournamentMode.Championship)
            {
                int realActiveCount = Players.Count(p => !p.IsEliminated
                    && !p.Name.Equals("BYE", StringComparison.OrdinalIgnoreCase));

                return realActiveCount <= 1
                    && Cycles.Count > 0
                    && CurrentCycleIndex >= Cycles.Count;
            }
            return Cycles.Count == 0;
        }
    }
}
