using System.Collections.Generic;

namespace FightingTournament.Models;

public class Tournament
{
    public List<Player> Players { get; } = new();
    public List<Cycle>  Cycles  { get; } = new();

    public int CurrentCycleIndex { get; set; } = 0;

    public Cycle? CurrentCycle =>
        CurrentCycleIndex < Cycles.Count ? Cycles[CurrentCycleIndex] : null;

    public bool IsFinished => CurrentCycleIndex >= Cycles.Count;
}