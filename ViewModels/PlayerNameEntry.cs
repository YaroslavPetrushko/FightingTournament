namespace FightingTournament.ViewModels;

/// <summary>Thin wrapper so individual name TextBoxes can participate in binding.</summary>
public class PlayerNameEntry : BaseViewModel
{
    private int    _index;
    private string _name;

    public int Index
    {
        get => _index;
        set => Set(ref _index, value);
    }

    public string Name
    {
        get => _name;
        set => Set(ref _name, value);
    }

    public PlayerNameEntry(int index, string name)
    {
        _index = index;
        _name  = name;
    }
}
