using FightingTournament.Models;
using System;
using System.Collections.ObjectModel;

namespace FightingTournament.ViewModels;

public class MatchRowViewModel : BaseViewModel
{
    private readonly Match _match;

    // Unique per instance so RadioButtons in different rows don't interfere
    public string MatchId     { get; } = Guid.NewGuid().ToString();

    public string Player1Name => _match.Player1.Name;
    public string Player2Name => _match.Player2.Name;

    // ── Characters ───────────────────────────────────────────────────

    private string? _char1;
    public string? Character1
    {
        get => _char1;
        set { Set(ref _char1, value); _match.Character1 = value; }
    }

    private string? _char2;
    public string? Character2
    {
        get => _char2;
        set { Set(ref _char2, value); _match.Character2 = value; }
    }

    // ── Winner (1 / 2 / null) ────────────────────────────────────────

    private int? _winnerId;
    public int? WinnerId
    {
        get => _winnerId;
        set
        {
            _winnerId       = value;
            _match.WinnerId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Player1Won));
            OnPropertyChanged(nameof(Player2Won));
            OnPropertyChanged(nameof(IsCompleted));
        }
    }

    // These two properties drive the RadioButton IsChecked bindings.
    // Mutual exclusion is handled both here and by RadioButton.GroupName = MatchId.
    public bool Player1Won
    {
        get => WinnerId == 1;
        set { if (value) WinnerId = 1; }
    }

    public bool Player2Won
    {
        get => WinnerId == 2;
        set { if (value) WinnerId = 2; }
    }

    public bool IsCompleted => WinnerId.HasValue;

    // ── Character presets ────────────────────────────────────────────

    public static readonly ObservableCollection<string> AvailableCharacters = new(new[]
    {
        // Street Fighter
        //"Ryu", "Ken", "Chun-Li", "Guile", "Zangief", "Blanka", "Dhalsim", "Cammy", "Akuma",
        // Mortal Kombat
        //"Scorpion", "Sub-Zero", "Raiden", "Liu Kang", "Kitana", "Sonya", "Johnny Cage", "Shang Tsung",
        // Tekken (8)
        "Kazuya Mishima", "Heihachi Mishima", "Jin Kazama", "Jun Kazama", "Reina", "Paul Phoenix", "Marshall Law", "King", 
        "Nina Williams", "Anna Williams", "Hwoarang", "Xiaoyu", "Panda", "Kuma", "Zafina", "Devil Jin", "Victor Chevalier",
        "Lars Alexadersson", "Jack", "Leroy", "Asuka Kazama", "Lili", "Claudio", "Feng Wei", "Bryan Fury", "Raven", "Azucena",
        "Yoshimitsu", "Steve Fox", "Leo", "Sergei Dragunov", "Shaheen", "Lee Chaolan", "Alisa Bosconovitch",
        "Eddy Gordo", "Lidia Sobieska", "Clive Rosfield", "Fahkumram", "Armor King", "Miary Zo", "Kunimitsu",
        // Guilty Gear (Strive)
        "Sol Badguy", "Ky Kiske", "Millia Rage", "Faust", "May", "I-No", "Axl Low", "Chipp", "Potemkin", "Zato-1", "Ramlethal",
        "Leo",  "Nagoriyuki", "Giovanna", "Anji", "Goldlewis", "Jack-O", "Happy Chaos", "Baiken", "Testament", "Bridget", 
        "Sin Kiske", "Queen Dizzy","Bedman","Asuka R#", "Johnny", "Elphelt", "A.B.A.", "Slayer", "Venom", "Unika", "Lucy", "Jam",
        // King of Fighters
        // "Kyo Kusanagi", "Iori Yagami", "Terry Bogard", "Leona", "K'",
        // BlazBlue
        // "Ragna", "Jin Kisaragi", "Noel Vermillion", "Rachel Alucard",
        // Dragon Ball FighterZ
        // "Goku", "Vegeta", "Gohan", "Frieza", "Cell",
    });

    public MatchRowViewModel(Match match) => _match = match;
}