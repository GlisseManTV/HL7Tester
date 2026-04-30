using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HL7Tester.Core.Inspector.Models;
using HL7Tester.Core.Inspector.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static HL7Tester.ViewModels.Hl7TreeNode;

namespace HL7Tester.ViewModels;

/// <summary>
/// ViewModel for the HL7 Inspector page.
/// Parses raw HL7 v2 messages using <see cref="HL7ParserService"/> and builds an
/// expandable flat tree of <see cref="Hl7TreeNode"/> objects for display in a CollectionView.
/// </summary>
public sealed class Hl7InspectorViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly HL7ParserService _parserService;
    private readonly ILogger<Hl7InspectorViewModel>? _logger;
    private bool _isDisposed;

    // ── Bindable state ────────────────────────────────────────────────────────

    private string _rawMessage  = string.Empty;
    private string _parseStatus = string.Empty;
    private string _detectedVersion = string.Empty;
    private bool   _isParsing;

    public string RawMessage
    {
        get => _rawMessage;
        set { _rawMessage = value; OnPropertyChanged(); }
    }

    public string ParseStatus
    {
        get => _parseStatus;
        set { _parseStatus = value; OnPropertyChanged(); }
    }

    public string DetectedVersion
    {
        get => _detectedVersion;
        set { _detectedVersion = value; OnPropertyChanged(); }
    }

    public bool IsParsing
    {
        get => _isParsing;
        set
        {
            _isParsing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotParsing));
            (ParseCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool IsNotParsing => !IsParsing;

    /// <summary>
    /// Flat list of currently visible tree nodes.
    /// Only nodes that are "revealed" by their parent's expand state are included.
    /// </summary>
    public ObservableCollection<Hl7TreeNode> TreeNodes { get; } = new();

    public ICommand ParseCommand { get; }

    // ─────────────────────────────────────────────────────────────────────────

    public Hl7InspectorViewModel(
        HL7ParserService parserService,
        ILogger<Hl7InspectorViewModel>? logger = null)
    {
        _parserService = parserService;
        _logger        = logger ?? NullLogger<Hl7InspectorViewModel>.Instance;
        ParseCommand   = new Command(ParseMessage, () => IsNotParsing);
    }

    // ── Parsing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses <see cref="RawMessage"/> and populates <see cref="TreeNodes"/>.
    /// Called directly from code-behind to avoid MAUI Editor binding timing issues.
    /// </summary>
    public void ParseMessage()
    {
        if (string.IsNullOrWhiteSpace(RawMessage))
        {
            ParseStatus = "Error: No message to parse.";
            return;
        }

        IsParsing = true;
        ParseStatus = "Parsing...";
        TreeNodes.Clear();

        try
        {
            var result = _parserService.Parse(RawMessage);

            if (!result.IsSuccess)
            {
                ParseStatus = $"Error: {result.ParseError}";
                return;
            }

            DetectedVersion = $"HL7 v{result.Version}";

            // Build the flat tree — only segment-level nodes added initially
            foreach (var seg in result.Segments)
            {
                var segNode = BuildSegmentNode(seg);
                TreeNodes.Add(segNode);
            }

            ParseStatus = TreeNodes.Count > 0
                ? $"OK — {result.Segments.Count} segment(s) · HL7 v{result.Version}"
                : "Warning: No segments found.";

            OnPropertyChanged(nameof(TreeNodes));
            _logger?.LogInformation("Parsed HL7 message: {Count} segments, version {Version}",
                result.Segments.Count, result.Version);
        }
        catch (Exception ex)
        {
            ParseStatus = $"Error: {ex.Message[..Math.Min(120, ex.Message.Length)]}";
            _logger?.LogError(ex, "Failed to parse HL7 message");
            OnPropertyChanged(nameof(TreeNodes));
        }
        finally
        {
            IsParsing = false;
        }
    }

    // ── Tree building ─────────────────────────────────────────────────────────

    private Hl7TreeNode BuildSegmentNode(HL7SegmentModel seg)
    {
        // Build field children (not yet added to TreeNodes)
        var fieldChildren = new List<Hl7TreeNode>();
        foreach (var field in seg.Fields)
        {
            var fieldNode = BuildFieldNode(field);
            fieldChildren.Add(fieldNode);
        }

        var segNode = new Hl7TreeNode
        {
            Level    = NodeLevel.Segment,
            Notation = seg.Id,
            Name     = seg.Name,
            DataType = string.Empty,
            Value    = string.Empty,
            Children = fieldChildren
        };

        // Wire the toggle: expand inserts children after this node; collapse removes them
        segNode.ToggleCommand = new Command(() => ToggleNode(segNode));
        return segNode;
    }

    private Hl7TreeNode BuildFieldNode(HL7FieldModel field)
    {
        // Collect component children across all repetitions
        var compChildren = new List<Hl7TreeNode>();

        foreach (var rep in field.Repetitions)
        {
            // If multiple repetitions, add a repetition header node
            string repPrefix = field.Repetitions.Count > 1
                ? $"{field.Notation}[{rep.Index}]"
                : field.Notation;

            if (field.Repetitions.Count > 1)
            {
                var repNode = new Hl7TreeNode
                {
                    Level    = NodeLevel.Component,
                    Notation = repPrefix,
                    Name     = $"Repetition {rep.Index}",
                    DataType = string.Empty,
                    Value    = rep.RawValue,
                };
                compChildren.Add(repNode);
            }

            // Add component nodes only if more than one component has a value
            var nonEmptyComps = rep.Components.Where(c => !string.IsNullOrEmpty(c.Value)).ToList();

            if (nonEmptyComps.Count > 1 || (nonEmptyComps.Count == 1 && rep.Components.Count > 1))
            {
                foreach (var comp in rep.Components)
                {
                    if (string.IsNullOrEmpty(comp.Value)) continue;

                    var subCompChildren = new List<Hl7TreeNode>();
                    foreach (var sub in comp.SubComponents)
                    {
                        if (string.IsNullOrEmpty(sub.Value)) continue;
                        subCompChildren.Add(new Hl7TreeNode
                        {
                            Level    = NodeLevel.SubComponent,
                            Notation = sub.Notation,
                            Name     = string.Empty,
                            Value    = sub.Value,
                        });
                    }

                    var compNode = new Hl7TreeNode
                    {
                        Level    = NodeLevel.Component,
                        Notation = field.Repetitions.Count > 1
                            ? $"{repPrefix}.{comp.Index}"
                            : comp.Notation,
                        Name     = comp.Name,
                        DataType = string.Empty,
                        Value    = comp.Value,
                        Children = subCompChildren
                    };

                    if (subCompChildren.Count > 0)
                        compNode.ToggleCommand = new Command(() => ToggleNode(compNode));

                    compChildren.Add(compNode);
                }
            }
        }

        // Determine display value (first non-empty repetition's raw value or single component)
        var displayValue = field.RawValue;
        if (field.Repetitions.Count == 1 && field.Repetitions[0].Components.Count == 1)
            displayValue = field.Repetitions[0].Components[0].Value;

        var fieldNode = new Hl7TreeNode
        {
            Level    = NodeLevel.Field,
            Notation = field.Notation,
            Name     = field.Name,
            DataType = field.DataType,
            Value    = displayValue,
            Required = field.Required,
            Children = compChildren
        };

        if (compChildren.Count > 0)
            fieldNode.ToggleCommand = new Command(() => ToggleNode(fieldNode));

        return fieldNode;
    }

    // ── Expand / Collapse ─────────────────────────────────────────────────────

    /// <summary>
    /// Toggles the expand state of a node: inserts its children after it in
    /// <see cref="TreeNodes"/> when expanding, removes them when collapsing.
    /// </summary>
    private void ToggleNode(Hl7TreeNode node)
    {
        if (!node.HasChildren) return;

        if (node.IsExpanded)
        {
            // Collapse: remove all descendants
            CollapseNode(node);
        }
        else
        {
            // Expand: insert direct children after this node
            int idx = TreeNodes.IndexOf(node);
            if (idx < 0) return; // node not in collection
            int insertAt = idx + 1;

            foreach (var child in node.Children)
            {
                // Reset child expansion state on re-insertion
                if (child.IsExpanded)
                    CollapseNode(child);

                TreeNodes.Insert(insertAt++, child);
            }

            node.IsExpanded = true;
        }
    }

    private void CollapseNode(Hl7TreeNode node)
    {
        // Recursively collapse and remove all descendant nodes first
        foreach (var child in node.Children)
        {
            if (child.IsExpanded)
                CollapseNode(child);

            TreeNodes.Remove(child);
        }

        node.IsExpanded = false;
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (!_isDisposed)
        {
            TreeNodes.Clear();
            _isDisposed = true;
        }
    }
}
