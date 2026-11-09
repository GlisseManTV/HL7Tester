using System.Globalization;
using HL7Tester.Core;
using Microsoft.Maui.Controls;

namespace HL7Tester.Converters;

/// <summary>
/// Converts an <see cref="EndpointTestState"/> enum value to a <see cref="Color"/>
/// used for the endpoint status dot and label in the Saved Connections list.
/// </summary>
public sealed class EndpointStatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EndpointTestState state)
        {
            bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

            return state switch
            {
                EndpointTestState.Reachable => Color.FromArgb("#4CAF50"),                // Green – always visible
                EndpointTestState.Unreachable => Color.FromArgb("#F44336"),              // Red – always visible
                EndpointTestState.Testing => isDark ? Color.FromArgb("#9E9E9E") : Color.FromArgb("#757575"), // Gray
                _ => isDark ? Color.FromArgb("#757575") : Color.FromArgb("#9E9E9E"),     // Unknown – gray
            };
        }

        return Color.FromArgb("#9E9E9E");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
