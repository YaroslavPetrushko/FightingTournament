using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FightingTournament.Converters;

[ValueConversion(typeof(int), typeof(Thickness))]
public class CycleToMatchMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int number) // 1-indexed round/cycle number
        {
            int r = number - 1; // 0-indexed round
            double cardHeight = 60.0;
            double baseTotalHeight = 80.0;
            double totalHeight = baseTotalHeight * Math.Pow(2, r);
            double margin = (totalHeight - cardHeight) / 2.0;

            return new Thickness(0, margin, 0, margin);
        }
        return new Thickness(0, 8, 0, 8); // Fallback margin
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
