namespace FightingTournament.ViewModels;

/// <summary>Thin wrapper so individual name TextBoxes can participate in binding.</summary>
public class PlayerNameEntry : BaseViewModel
{
    private string _name;

    public int    Index { get; }
    public string Name
    {
        get => _name;
        set => Set(ref _name, value);
    }

    public PlayerNameEntry(int index, string name)
    {
        Index = index;
        _name = name;
    }
}