using System.Globalization;
using HL7Tester.ViewModels;
using Microsoft.Maui.Controls;

namespace HL7Tester.Converters;

/// <summary>
/// Converts a <see cref="Hl7TreeNode.NodeLevel"/> enum value to a <see cref="Color"/>
/// used to colour the notation label in the HL7 inspector tree.
/// </summary>
public class NodeLevelToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Hl7TreeNode.NodeLevel level)
        {
            bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

            return level switch
            {
                Hl7TreeNode.NodeLevel.Segment =>
                    Color.FromArgb("#6366F1"),                                              // Indigo – always visible

                Hl7TreeNode.NodeLevel.Field =>
                    isDark ? Colors.White : Color.FromArgb("#111827"),                      // Near-black / white

                Hl7TreeNode.NodeLevel.Component =>
                    isDark ? Color.FromArgb("#D1D5DB") : Color.FromArgb("#374151"),         // Gray-300 / Gray-700

                Hl7TreeNode.NodeLevel.SubComponent =>
                    isDark ? Color.FromArgb("#9CA3AF") : Color.FromArgb("#6B7280"),         // Gray-400 / Gray-500

                _ => Color.FromArgb("#374151")
            };
        }

        return Color.FromArgb("#374151");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
