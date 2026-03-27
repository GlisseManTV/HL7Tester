using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HL7Tester.Converters;

/// <summary>
/// Converts a boolean value to an arrow indicator (▼ when true, ► when false).
/// </summary>
public class BoolToArrowConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "▼ Send log" : "► Send log";
        }

        return "► Send log";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return stringValue.StartsWith("▼");
        }

        return false;
    }
}