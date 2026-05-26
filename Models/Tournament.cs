using System.Collections.Generic;

namespace FightingTournament.Models;

public class Tournament
{
    public List<Player> Players { get; } = new();
    public List<Cycle>  Cycles  { get; } = new();

    public int CurrentCycleIndex { get; set; } = 0;

    public Cycle? CurrentCycle =>
        CurrentCycleIndex < Cycles.Count ? Cycles[CurrentCycleIndex] : null;

    // Tournament never auto-finishes — user stops manually.
    // IsFinished is kept for "0 active players" edge case only.
    public bool IsFinished => Cycles.Count == 0;
}
