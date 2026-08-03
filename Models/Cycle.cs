using System.Collections.Generic;
using System.Linq;

namespace FightingTournament.Models;

public class Cycle
{
    public int Number { get; }
    public List<Match> Matches { get; } = new();
    public EndlessPairingMode PairingMode { get; set; } = EndlessPairingMode.Mixed;

    public bool IsCompleted => Matches.All(m => m.IsCompleted);

    public Cycle(int number) => Number = number;
}
