using System.Globalization;

namespace HL7Tester.Resources.Converters;

/// <summary>
/// Maps raw protocol values ("MLLP" / "FHIR") to the user-facing labels shown in the
/// protocol dropdown. Item values stay "MLLP"/"FHIR" so the ViewModel binding surface
/// is identical to NetworkSettings.Protocol.
/// </summary>
public sealed class ProtocolDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as string) switch
        {
            "FHIR" => "FHIR (REST)",
            _ => "MLLP (HL7 v2)",
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Not used: ItemsSource holds the raw values, the converter only decorates the display.
        throw new NotSupportedException();
    }
}
