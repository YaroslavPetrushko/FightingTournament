using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FightingTournament.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>Set to true to invert: false → Visible, true → Collapsed.</summary>
    public bool Invert { get; set; }

    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        bool b = value is bool bv && bv;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        value is Visibility v && v == Visibility.Visible;
}
