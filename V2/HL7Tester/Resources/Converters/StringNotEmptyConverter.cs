using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HL7Tester.Converters;

/// <summary>
/// Converts a non-empty string to true, empty/null to false.
/// </summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return !string.IsNullOrWhiteSpace(str);
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}