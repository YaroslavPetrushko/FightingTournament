using System;

namespace FightingTournament.Models;

public class UserProfileInfo
{
    public string Nickname { get; set; } = string.Empty;
    public int TotalMatches { get; set; }
    public int TotalWins { get; set; }
    public double WinRate { get; set; }
    public string FavoriteCharacter { get; set; } = "None";
    public string FavoriteGame { get; set; } = "None";
    public string LastMatchDate { get; set; } = "None";
}
