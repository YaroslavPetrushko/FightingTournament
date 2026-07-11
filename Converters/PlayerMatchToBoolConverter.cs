using System;
using System.Globalization;
using System.Windows.Data;

namespace FightingTournament.Converters;

public class PlayerMatchToBoolConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2)
            return false;

        string? name = values[0] as string;
        string? assigned = values[1] as string;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(assigned))
            return false;

        return string.Equals(name, assigned, StringComparison.OrdinalIgnoreCase);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
