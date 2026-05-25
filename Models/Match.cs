namespace FightingTournament.Models;

public class Match
{
    public Player Player1 { get; }
    public Player Player2 { get; }

    public string? Character1 { get; set; }
    public string? Character2 { get; set; }

    /// <summary>1 = Player1 wins, 2 = Player2 wins, null = not played yet.</summary>
    public int? WinnerId { get; set; }

    public bool IsCompleted => WinnerId.HasValue;

    public Match(Player p1, Player p2)
    {
        Player1 = p1;
        Player2 = p2;
    }
}
