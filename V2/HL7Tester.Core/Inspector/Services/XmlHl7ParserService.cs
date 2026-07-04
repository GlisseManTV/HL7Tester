using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace HL7Tester.Core.Inspector.Services;

/// <summary>
/// Converts an XML-serialized HL7 message back to standard pipe-separated format.
/// The resulting string can then be parsed by the existing <see cref="HL7ParserService"/>.
/// </summary>
public class XmlHl7ParserService
{
    /// <summary>
    /// Converts an XML-serialized HL7 message to standard HL7 text (pipe-separated).
    /// Returns null if the input is not valid XML or doesn't look like HL7 XML.
    /// Automatically normalizes unescaped characters (&, <, >) before parsing.
    /// </summary>
    public string? Parse(string xmlMessage)
    {
        if (string.IsNullOrWhiteSpace(xmlMessage))
            return null;

        try
        {
            // Normalize the XML content to handle unescaped special characters
            var normalized = NormalizeXmlContent(xmlMessage);
            var doc = XDocument.Parse(normalized);
            var root = doc.Root;
            if (root == null)
                return null;

            // Check for known HL7 XML patterns
            var hl7Patterns = new[] { "MSH", "EVN", "PID", "PV1", "OBX", "ORC", "OBR", "NK1", "IN1", "DG1", "AL1" };
            bool hasKnownSegment = root.Elements().Any(e => hl7Patterns.Contains(e.Name.LocalName, System.StringComparer.OrdinalIgnoreCase));
            if (!hasKnownSegment)
                return null;

            // Reconstruct standard HL7 message
            var segments = new List<string>();

            foreach (var element in root.Elements())
            {
                var segmentId = element.Name.LocalName;
                if (string.IsNullOrEmpty(segmentId))
                    continue;

                if (!IsKnownSegmentId(segmentId))
                    continue;

                var standardLine = ConvertSegmentToStandard(element, segmentId);
                segments.Add(standardLine);
            }

            return string.Join("\r", segments);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to auto-detect whether the input is XML-serialized HL7.
    /// Returns true if it looks like an HL7 XML document.
    /// </summary>
    public static bool IsXmlHl7(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var trimmed = message.Trim();
        if (!trimmed.StartsWith("<"))
            return false;

        // Check for known HL7 XML patterns
        var hl7Patterns = new[]
        {
            @"<MSH", @"<EVN", @"<PID", @"<PV1", @"<OBX", @"<ORC", @"<OBR",
            @"<NK1", @"<IN1", @"<DG1", @"<AL1", @"<ROL", @"<SCT"
        };

        return hl7Patterns.Any(p => Regex.IsMatch(trimmed, p, RegexOptions.IgnoreCase));
    }

    // HL7 data type prefixes used for sub-components (e.g. PL.1, HD.2, CX.5)
    private static readonly HashSet<string> DataTypePrefixes = new(
        System.StringComparer.OrdinalIgnoreCase)
    {
        "AD", "ADJ", "ADP", "AO", "AP", "CA", "CO", "DA", "DD", "DE", "DL", "DO",
        "DP", "DR", "DU", "EA", "EC", "EL", "EN", "EO", "EP", "EQ", "ER", "ES",
        "ET", "EV", "EX", "FA", "FC", "FD", "FE", "FI", "FL", "FN", "FO", "FT",
        "HD", "IC", "ID", "IS", "INT", "NM", "PL", "PN", "PNL", "PNP", "PR", "PT",
        "RP", "SI", "SL", "SN", "SP", "ST", "SCT", "TD", "TI", "TN", "TP", "TS",
        "TX", "UD", "UF", "UN", "UR", "URP", "VCD", "VID", "VN", "VO", "VR", "VT",
        "VV", "VW", "VX", "VY", "VZ", "WE", "XAD", "XCN", "XPN"
    };

    private string ConvertSegmentToStandard(XElement element, string segmentId)
    {
        // MSH is special: field separator goes in MSH-1
        if (string.Equals(segmentId, "MSH", System.StringComparison.OrdinalIgnoreCase))
        {
            return ConvertMshSegment(element);
        }

        var fields = new List<string>();
        fields.Add(segmentId); // Field 0 = segment ID

        foreach (var child in element.Elements())
        {
            var fieldNotation = child.Name.LocalName;
            int fieldIndex = ExtractFieldIndex(fieldNotation);
            if (fieldIndex <= 0)
                continue;

            // Skip data type sub-components (e.g. PL.1, HD.2, CX.5)
            var dotPos = fieldNotation.IndexOf('.');
            if (dotPos > 0 && IsDataTypePrefix(fieldNotation.Substring(0, dotPos)))
                continue;

            string value = ConvertElementToStandardValue(child, fieldIndex);
            SetFieldValue(fields, fieldIndex, value);
        }

        return string.Join("|", fields);
    }

    private string ConvertMshSegment(XElement element)
    {
        // In standard pipe-separated HL7, MSH-1 is the separator character itself:
        // MSH|^~\&|SENDER...
        // Therefore MSH.1 must define the join delimiter, not become an extra field.
        var fieldSeparator = "|";
        var mshFields = new List<string> { "MSH" };

        foreach (var child in element.Elements())
        {
            var fieldNotation = child.Name.LocalName;
            int fieldIndex = ExtractFieldIndex(fieldNotation);
            if (fieldIndex <= 0)
                continue;

            // Skip data type sub-components (e.g. PL.1, HD.2, CX.5)
            var dotPos = fieldNotation.IndexOf('.');
            if (dotPos > 0 && IsDataTypePrefix(fieldNotation.Substring(0, dotPos)))
                continue;

            if (fieldIndex == 1)
            {
                var configuredSeparator = GetElementValue(child);
                if (!string.IsNullOrEmpty(configuredSeparator))
                    fieldSeparator = configuredSeparator[0].ToString();

                continue;
            }

            string value = ConvertElementToStandardValue(child, fieldIndex);
            SetFieldValue(mshFields, fieldIndex - 1, value);
        }

        return string.Join(fieldSeparator, mshFields);
    }

    private static void SetFieldValue(List<string> fields, int outputIndex, string value)
    {
        while (fields.Count <= outputIndex)
        {
            fields.Add(string.Empty);
        }

        fields[outputIndex] = value;
    }

    private string ConvertElementToStandardValue(XElement element, int fieldIndex)
    {
        // If the element has typed children (e.g. <CX.1>, <HD.2>), build a component-separated value
        var children = element.Elements().ToList();
        if (!children.Any())
        {
            // Plain text value
            return GetElementValue(element);
        }

        // Check if children are typed sub-elements (e.g. CX.1, HD.3)
        var typedChildren = children.Where(c => c.Name.LocalName.Contains('.')).ToList();
        if (!typedChildren.Any())
        {
            // Children without dot notation — return text content directly
            return GetElementValue(element);
        }

        // Find the maximum index among typed children to build a correctly-sized array
        var maxIndex = typedChildren
            .Select(c => int.Parse(c.Name.LocalName.Substring(c.Name.LocalName.IndexOf('.') + 1)))
            .Max();

        // Build array with correct size (index is 1-based, empty slots become empty strings)
        var parts = new string[maxIndex];
        foreach (var child in typedChildren)
        {
            var childName = child.Name.LocalName;
            var childDotIndex = childName.IndexOf('.');
            if (childDotIndex > 0 && int.TryParse(childName.Substring(childDotIndex + 1), out var childIndex))
            {
                parts[childIndex - 1] = ExtractTypedValue(child);
            }
        }

        return string.Join("^", parts);
    }

    private string ExtractTypedValue(XElement element)
    {
        var name = element.Name.LocalName;
        var dotIndex = name.IndexOf('.');
        if (dotIndex <= 0)
            return GetElementValue(element);

        // Extract the numeric index (e.g. PL.9 → 9, CX.1 → 1)
        if (!int.TryParse(name.Substring(dotIndex + 1), out var index))
            return GetElementValue(element);

        // Check for nested elements (recursive extraction)
        var children = element.Elements().ToList();
        if (!children.Any())
        {
            return GetElementValue(element);
        }

        var typedChildren = children.Where(c => c.Name.LocalName.Contains('.')).ToList();
        if (!typedChildren.Any())
        {
            return GetElementValue(element);
        }

        // Build array with correct size (index is 1-based)
        var parts = new string[index];
        foreach (var child in typedChildren)
        {
            var childName = child.Name.LocalName;
            var childDotIndex = childName.IndexOf('.');
            if (childDotIndex > 0 && int.TryParse(childName.Substring(childDotIndex + 1), out var childIndex))
            {
                parts[childIndex - 1] = ExtractTypedValue(child);
            }
        }

        return string.Join("^", parts);
    }

    private static int ExtractFieldIndex(string notation)
    {
        // Extract numeric index from "MSH.12", "PID.3", "CX.4", etc.
        var match = Regex.Match(notation, @"\.(\d+)$");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var index))
            return index;

        return 0;
    }

    /// <summary>
    /// Normalizes XML content by escaping unescaped ampersands in text values.
    /// This handles cases where clients send raw characters instead of proper XML entities.
    /// Preserves already-correctly-escaped entities and XML structure.
    /// </summary>
    private static string NormalizeXmlContent(string xml)
    {
        // Escape raw ampersands while preserving valid XML entity references.
        // Example: <MSH.2>^~\&</MSH.2> becomes <MSH.2>^~\&amp;</MSH.2>,
        // while <MSH.2>^~\&amp;</MSH.2> is left unchanged.
        return Regex.Replace(
            xml,
            @"&(?!amp;|lt;|gt;|quot;|apos;|#\d+;|#x[\da-fA-F]+;)",
            "&amp;");
    }

    /// <summary>
    /// Decodes XML entity references back to their character equivalents.
    /// Used after extracting values from parsed XML elements.
    /// </summary>
    private static string DecodeXmlEntities(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // XElement.Value already decodes XML entities, but keep this method tolerant
        // for any text that may still contain entity references.
        var decoded = value
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&apos;", "'");

        // Decode numeric entities: &#123; (decimal) and &#x1F; (hexadecimal)
        decoded = Regex.Replace(decoded, @"&#(\d+);", m =>
        {
            try
            {
                int codePoint = int.Parse(m.Groups[1].Value);
                return char.ConvertFromUtf32(codePoint);
            }
            catch
            {
                return m.Value; // Keep original if invalid
            }
        });

        decoded = Regex.Replace(decoded, @"&#x([\da-fA-F]+);", m =>
        {
            try
            {
                int codePoint = int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                return char.ConvertFromUtf32(codePoint);
            }
            catch
            {
                return m.Value; // Keep original if invalid
            }
        });

        return decoded.Trim();
    }

    private static string GetElementValue(XElement element)
    {
        return DecodeXmlEntities(element.Value);
    }

    private static bool IsDataTypePrefix(string prefix)
    {
        return DataTypePrefixes.Contains(prefix);
    }

    private static bool IsKnownSegmentId(string id)
    {
        if (HL7KnowledgeBase.KnownSegmentIds.Contains(id))
            return true;

        // Also match custom Z-segments
        return Regex.IsMatch(id, @"^Z[A-Z0-9]{2}$", RegexOptions.IgnoreCase);
    }
}