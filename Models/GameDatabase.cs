using System.Collections.Generic;

namespace FightingTournament.Models;

public static class GameDatabase
{
    public static readonly Dictionary<string, List<string>> Games = new()
    {
        {
            "Tekken 8", new List<string>
            {
                "Alisa Bosconovitch", "Asuka Kazama", "Azucena", "Bryan Fury", "Claudio", "Devil Jin", "Dragunov", "Eddy Gordo",
                "Feng Wei", "Heihachi Mishima", "Hwoarang", "Jack-8", "Jin Kazama", "Jun Kazama", "Kazuya Mishima", "King", "Kunimitsu",
                "Kuma", "Lars Alexandersson", "Lee Chaolan", "Leo", "Leroy Smith", "Lidia Sobieska", "Lili", "Marshall Law",
                "Nina Williams", "Panda", "Paul Phoenix", "Raven", "Reina", "Shaheen", "Steve Fox", "Victor Chevalier", "Xiaoyu",
                "Yoshimitsu", "Zafina"
            }
        },
        {
            "Guilty Gear -Strive-", new List<string>
            {
                "A.B.A", "Anji Mito", "Asuka R#", "Axl Low", "Baiken", "Bedman?", "Bridget", "Chipp Zanuff", "Elphelt Valentine",
                "Faust", "Giovanna", "Goldlewis Dickinson", "Happy Chaos", "I-No", "Jack-O'", "Johnny", "Jam Kuradoberi", "Ky Kiske", "Leo Whitefang",
                "Lucy", "May", "Millia Rage", "Nagoriyuki", "Potemkin", "Queen Dizzy", "Ramlethal Valentine", "Sin Kiske", "Slayer",
                "Sol Badguy", "Testament", "Venom", "Unika", "Zato-1"
            }
        },
        {
            "Street Fighter 6", new List<string>
            {
                "A.K.I.", "Akuma", "Blanka", "Cammy", "Chun-Li", "Dee Jay", "Dhalsim", "E. Honda", "Ed", "Elena", "Guile", "Jamie", "JP",
                "Juri", "Ken", "Kimberly", "Lily", "Luke", "Mai Shiranui", "Manon", "Marisa", "M. Bison", "Rashid", "Ryu", "Terry Bogard", "Zangief"
            }
        },
        {
            "Mortal Kombat 1", new List<string>
            {
                "Ashrah", "Baraka", "Cyrax", "Ermac", "General Shao", "Geras", "Ghostface", "Havik", "Homelander", "Johnny Cage", "Kenshi", "Kitana",
                "Kung Lao", "Li Mei", "Liu Kang", "Mileena", "Nitara", "Noob Saibot", "Omni-Man", "Peacemaker", "Quan Chi", "Raiden", "Rain",
                "Reiko", "Reptile", "Scorpion", "Sektor", "Shang Tsung", "Sindel", "Smoke", "Sub-Zero", "Takeda", "Tanya"
            }
        },
        {
            "Ultimate Marvel Vs. Capcom 3", new List<string>
            {
                "Akuma", "Albert Wesker", "Amaterasu", "Arthur", "Captain America", "Chris Redfield", "Crimson Viper", "Dante",
                "Deadpool", "Doctor Doom", "Doctor Strange", "Dormammu", "Felicia", "Firebrand", "Frank West", "Ghost Rider",
                "Hawkeye", "Hulk", "Hsien-Ko", "Iron Fist", "Iron Man", "Jill Valentine", "Magneto", "Mike Haggar",
                "M.O.D.O.K.", "Morrigan Aensland", "Nathan Spencer", "Nemesis T-Type", "Nova", "Phoenix", "Phoenix Wright",
                "Rocket Raccoon", "Ryu", "Sentinel", "She-Hulk", "Shuma-Gorath", "Spider-Man", "Storm", "Super-Skrull",
                "Taskmaster", "Thor", "Trish", "Tron Bonne", "Vergil", "Viewtiful Joe", "Wolverine", "X-23", "Zero"
            }
        },
        {
            "Dragon Ball FighterZ", new List<string>
            {
                "Android 16", "Android 17", "Android 18", "Android 21", "Android 21 (Lab Coat)", "Bardock", "Beerus", "Broly",
                "Broly (DBS)", "Cell", "Cooler", "Frieza", "Ginyu", "Gogeta (SS4)", "Gogeta (SSGSS)", "Gohan (Adult)",
                "Gohan (Teen)", "Goku (Base)", "Goku (GT)", "Goku (SSGSS)", "Goku (Super Saiyan)", "Goku (Ultra Instinct)",
                "Goku Black", "Gotenks", "Hit", "Janemba", "Jiren", "Kefla", "Kid Buu", "Krillin", "Majin Buu",
                "Master Roshi", "Nappa", "Piccolo", "Super Baby 2", "Tien", "Trunks", "Vegito (SSGSS)", "Videl", "Yamcha", "Zamasu (Fused)"
            }
        },
        {
            "Super Smash Bros. Ultimate", new List<string>
            {
                "Mario", "Donkey Kong", "Link", "Samus", "Dark Samus", "Yoshi", "Kirby", "Fox", "Pikachu", "Luigi", "Ness",
                "Captain Falcon", "Jigglypuff", "Peach", "Daisy", "Bowser", "Ice Climbers", "Sheik", "Zelda", "Dr. Mario",
                "Pichu", "Falco", "Marth", "Lucina", "Young Link", "Ganondorf", "Mewtwo", "Roy", "Chrom", "Mr. Game & Watch",
                "Meta Knight", "Pit", "Dark Pit", "Zero Suit Samus", "Wario", "Snake", "Ike", "Pokemon Trainer", "Diddy Kong",
                "Lucas", "Sonic", "King Dedede", "Olimar", "Lucario", "R.O.B.", "Toon Link", "Wolf", "Villager", "Mega Man",
                "Wii Fit Trainer", "Rosalina & Luma", "Little Mac", "Greninja", "Mii Brawler", "Mii Swordfighter", "Mii Gunner",
                "Palutena", "Pac-Man", "Robin", "Shulk", "Bowser Jr.", "Duck Hunt", "Ryu", "Ken", "Cloud", "Corrin", "Bayonetta",
                "Inkling", "Ridley", "Simon", "Richter", "King K. Rool", "Isabelle", "Incineroar", "Piranha Plant", "Joker",
                "Hero", "Banjo & Kazooie", "Terry", "Byleth", "Min Min", "Steve", "Sephiroth", "Pyra", "Mythra", "Kazuya", "Sora"
            }
        },
        {
            "Custom (Any/Blank)", new List<string>()
        }
    };
}
