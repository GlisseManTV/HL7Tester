using System.Text.RegularExpressions;
using HL7Tester.Core.Inspector.Models;

namespace HL7Tester.Core.Inspector.Services;

/// <summary>
/// Parses raw HL7 v2 messages into a structured <see cref="ParsedHL7Message"/> hierarchy.
/// Supports Segment → Field → Repetition → Component → Sub-Component, with
/// human-readable descriptions and data types from <see cref="HL7KnowledgeBase"/>.
/// 
/// Handles:
///   - MLLP framing removal (VT/FS/CR)
///   - Multi-segment messages (CR or LF delimited)
///   - MSH special-case (MSH-1 = "|" implicit, field indices offset by 1)
///   - Field repetitions (~)
///   - Components (^) and sub-components (&)
///   - HL7 escape sequence decoding (\F\, \S\, \R\, \E\, \T\)
/// </summary>
public class HL7ParserService
{
    // ─────────────────────────────────────────────────────────────────────────
    // Compiled regex to detect segment boundaries in single-line messages.
    // Matches a space followed by a known HL7 segment ID + field separator,
    // e.g. " EVN|", " PID|", " ZBE|".
    // Using known segment IDs (not a generic [A-Z]{3}) prevents false positives
    // on field values like "I ST PAU|" (where PAU is NOT a known segment).
    // ─────────────────────────────────────────────────────────────────────────
    private static readonly Regex _segmentSplitRegex = BuildSegmentSplitRegex();

    private static Regex BuildSegmentSplitRegex()
    {
        // Escape each known segment ID and join with | for alternation
        var knownIds = string.Join("|", HL7KnowledgeBase.KnownSegmentIds);
        // Also match custom Z-segments (e.g. ZBE, ZBA, ZPD, ZNS)
        return new Regex(
            $@" (?=({knownIds}|Z[A-Z0-9]{{2}})\|)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    // Default HL7 v2 separator characters
    private char _fieldSep      = '|';
    private char _componentSep  = '^';
    private char _repetitionSep = '~';
    private char _escapeSep     = '\\';
    private char _subCompSep    = '&';

    /// <summary>
    /// Parses a raw HL7 v2 message string and returns the structured hierarchy.
    /// </summary>
    /// <param name="rawMessage">Raw HL7 text, optionally MLLP-framed.</param>
    public ParsedHL7Message Parse(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return new ParsedHL7Message { ParseError = "Message is empty." };
        }

        try
        {
            // 1. Strip MLLP framing and normalize line endings
            var clean = StripMllp(rawMessage.Trim());

            // 2. Read separator characters from MSH-2 (if present)
            ReadSeparators(clean);

            // 3. Detect HL7 version from MSH-12
            var version = HL7VersionDetector.Detect(clean);

            // 4. Normalize line endings, then handle single-line messages where
            //    segments are separated by spaces (e.g. "...0249629226 EVN|A08|...").
            //    Replace a space that is immediately followed by a known HL7 segment
            //    ID + field separator with \r, so the standard split works correctly.
            //    Using known IDs avoids false positives on field values such as
            //    "I ST PAU|" where PAU is a Spanish city name, not a segment ID.
            var normalized = clean
                .Replace("\r\n", "\r")
                .Replace("\n", "\r");

            normalized = _segmentSplitRegex.Replace(normalized, "\r");

            var lines = normalized.Split('\r', StringSplitOptions.RemoveEmptyEntries);

            var result = new ParsedHL7Message
            {
                Version    = version,
                RawMessage = rawMessage,
                Segments   = new List<HL7SegmentModel>()
            };

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var segment = ParseSegment(trimmed);
                result.Segments.Add(segment);
            }

            if (result.Segments.Count == 0)
                result.ParseError = "No segments found in the message.";

            return result;
        }
        catch (Exception ex)
        {
            return new ParsedHL7Message
            {
                RawMessage = rawMessage,
                ParseError = $"Parse error: {ex.Message}"
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private HL7SegmentModel ParseSegment(string line)
    {
        // Extract segment ID (first 3 characters before the first separator)
        var sepIdx    = line.IndexOf(_fieldSep);
        var segmentId = sepIdx > 0 ? line[..sepIdx] : line;
        segmentId     = segmentId.Trim().ToUpperInvariant();

        var segment = new HL7SegmentModel
        {
            Id      = segmentId,
            Name    = HL7KnowledgeBase.GetSegmentName(segmentId),
            RawLine = line,
            Fields  = new List<HL7FieldModel>()
        };

        // Split the line by field separator
        var rawFields = line.Split(_fieldSep);

        // MSH is a special case:
        //   MSH-1 = field separator '|' (implicit — the character itself)
        //   MSH-2 = encoding characters (rawFields[1])
        //   MSH-N = rawFields[N-1]
        // All other segments:
        //   rawFields[0] = segment ID
        //   rawFields[N] = field N
        if (string.Equals(segmentId, "MSH", StringComparison.OrdinalIgnoreCase))
        {
            // Add MSH-1 as a synthetic field (the separator character "|")
            segment.Fields.Add(CreateField(segmentId, 1, "|"));

            // Add MSH-2 and beyond — field index is rawFields index + 1
            for (int i = 1; i < rawFields.Length; i++)
            {
                var rawValue = rawFields[i];
                // Skip completely empty trailing fields
                if (i == rawFields.Length - 1 && string.IsNullOrEmpty(rawValue)) continue;

                int fieldIndex = i + 1; // MSH offset: rawFields[1] = MSH-2
                segment.Fields.Add(CreateField(segmentId, fieldIndex, rawValue));
            }
        }
        else
        {
            // Standard segments: rawFields[0] = segment ID, rawFields[i] = field i
            for (int i = 1; i < rawFields.Length; i++)
            {
                var rawValue = rawFields[i];
                // Skip completely empty trailing fields
                if (i == rawFields.Length - 1 && string.IsNullOrEmpty(rawValue)) continue;

                int fieldIndex = i; // Direct mapping for non-MSH segments
                segment.Fields.Add(CreateField(segmentId, fieldIndex, rawValue));
            }
        }

        return segment;
    }

    private HL7FieldModel CreateField(string segmentId, int fieldIndex, string rawValue)
    {
        var fieldInfo = HL7KnowledgeBase.TryGetFieldInfo(segmentId, fieldIndex);

        var field = new HL7FieldModel
        {
            Index       = fieldIndex,
            SegmentId   = segmentId,
            Name        = fieldInfo?.Name     ?? string.Empty,
            DataType    = fieldInfo?.DataType ?? string.Empty,
            Required    = fieldInfo?.Required ?? false,
            RawValue    = rawValue,
            Repetitions = new List<HL7RepetitionModel>()
        };

        // Split by repetition separator (~)
        var repetitions = rawValue.Split(_repetitionSep);
        for (int r = 0; r < repetitions.Length; r++)
        {
            var rep = CreateRepetition(segmentId, fieldIndex, r + 1, repetitions[r], field.DataType);
            field.Repetitions.Add(rep);
        }

        return field;
    }

    private HL7RepetitionModel CreateRepetition(
        string segmentId, int fieldIndex, int repIndex, string rawValue, string dataType)
    {
        var repetition = new HL7RepetitionModel
        {
            Index      = repIndex,
            RawValue   = rawValue,
            Components = new List<HL7ComponentModel>()
        };

        // Split by component separator (^)
        var components = rawValue.Split(_componentSep);
        for (int c = 0; c < components.Length; c++)
        {
            var compValue  = components[c];
            var compIndex  = c + 1;

            // Look up component name from the data type knowledge base
            var compName   = HL7KnowledgeBase.GetComponentName(dataType, compIndex);

            var component = new HL7ComponentModel
            {
                Index         = compIndex,
                FieldIndex    = fieldIndex,
                SegmentId     = segmentId,
                Name          = compName,
                DataType      = string.Empty, // sub-type not tracked at this level
                Value         = Decode(compValue),
                SubComponents = new List<HL7SubComponentModel>()
            };

            // Split by sub-component separator (&)
            var subComps = compValue.Split(_subCompSep);
            if (subComps.Length > 1)
            {
                for (int s = 0; s < subComps.Length; s++)
                {
                    component.SubComponents.Add(new HL7SubComponentModel
                    {
                        Index          = s + 1,
                        ComponentIndex = compIndex,
                        FieldIndex     = fieldIndex,
                        SegmentId      = segmentId,
                        Value          = Decode(subComps[s])
                    });
                }
            }

            repetition.Components.Add(component);
        }

        return repetition;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads MSH-2 (encoding characters) to configure separator characters.
    /// Falls back to HL7 defaults if MSH is not present or malformed.
    /// </summary>
    private void ReadSeparators(string message)
    {
        // Default HL7 v2 separators
        _fieldSep      = '|';
        _componentSep  = '^';
        _repetitionSep = '~';
        _escapeSep     = '\\';
        _subCompSep    = '&';

        try
        {
            // MSH line starts at position 0
            // MSH-2 is at positions 4-7 (after "MSH|")
            // E.g. "MSH|^~\&|..." → encoding chars = "^~\&"
            if (message.Length > 8 && message.StartsWith("MSH", StringComparison.OrdinalIgnoreCase))
            {
                // position 3 = _fieldSep (already '|')
                // positions 4-7 = encoding characters
                if (message[3] == _fieldSep && message.Length > 7)
                {
                    _componentSep  = message[4];
                    _repetitionSep = message[5];
                    _escapeSep     = message[6];
                    _subCompSep    = message[7];
                }
            }
        }
        catch
        {
            // Keep defaults on any error
        }
    }

    /// <summary>
    /// Removes MLLP vertical-tab start block (0x0B) and file-separator end block (0x1C)
    /// that wrap HL7 messages in MLLP transport.
    /// </summary>
    private static string StripMllp(string message)
        => message
            .Trim('\u000b', '\u001c', '\r', '\n')
            .Replace("\u000b", string.Empty)
            .Replace("\u001c", string.Empty);

    /// <summary>
    /// Decodes HL7 escape sequences in a string value.
    /// Escape sequences: \F\ = |, \S\ = ^, \R\ = ~, \E\ = \, \T\ = &
    /// </summary>
    private string Decode(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains(_escapeSep))
            return value;

        return value
            .Replace($"{_escapeSep}F{_escapeSep}", _fieldSep.ToString())
            .Replace($"{_escapeSep}S{_escapeSep}", _componentSep.ToString())
            .Replace($"{_escapeSep}R{_escapeSep}", _repetitionSep.ToString())
            .Replace($"{_escapeSep}T{_escapeSep}", _subCompSep.ToString())
            .Replace($"{_escapeSep}E{_escapeSep}", _escapeSep.ToString())
            .Replace($"{_escapeSep}.br{_escapeSep}", Environment.NewLine)
            .Replace($"{_escapeSep}X000d{_escapeSep}", "\r")
            .Replace($"{_escapeSep}X000a{_escapeSep}", "\n");
    }
}
