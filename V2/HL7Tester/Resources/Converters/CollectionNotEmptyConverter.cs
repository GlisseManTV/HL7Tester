using System;
using System.Collections;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HL7Tester.Converters;

/// <summary>
/// Converts a non-empty collection to true, null/empty to false.
/// </summary>
public class CollectionNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable collection)
        {
            var enumerator = collection.GetEnumerator();
            var hasItems = enumerator.MoveNext();
            return hasItems;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}