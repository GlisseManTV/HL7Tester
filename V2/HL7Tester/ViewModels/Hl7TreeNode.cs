using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HL7Tester.ViewModels;

/// <summary>
/// Represents a single visible node in the HL7 Inspector tree.
/// Nodes are kept in a flat <see cref="ObservableCollection{T}"/> in the ViewModel;
/// expand/collapse inserts or removes child nodes from that collection.
/// </summary>
public sealed class Hl7TreeNode : INotifyPropertyChanged
{
    /// <summary>Depth level of this node in the hierarchy.</summary>
    public enum NodeLevel { Segment = 0, Field = 1, Component = 2, SubComponent = 3 }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Hierarchy depth (Segment / Field / Component / SubComponent).</summary>
    public NodeLevel Level { get; init; }

    /// <summary>HL7 notation (e.g. "PV1", "PV1-3", "PV1-3.1", "PV1-3.1.2").</summary>
    public string Notation { get; init; } = string.Empty;

    /// <summary>Human-readable description from the HL7 knowledge base.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>HL7 data type code (e.g. "PL", "CWE", "IS").</summary>
    public string DataType { get; init; } = string.Empty;

    /// <summary>Actual decoded value from the message.</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>Whether this field is required per the HL7 specification.</summary>
    public bool Required { get; init; }

    /// <summary>Whether this node can be expanded (has children).</summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>Child nodes managed for expand/collapse (not directly in the flat collection).</summary>
    public List<Hl7TreeNode> Children { get; init; } = new();

    // ── Expand / Collapse ─────────────────────────────────────────────────────

    private bool _isExpanded;

    /// <summary>Whether this node is currently expanded, showing its children.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ExpandIcon));
        }
    }

    /// <summary>Arrow icon showing expand/collapse state. ▶ when collapsed, ▼ when expanded.</summary>
    public string ExpandIcon => HasChildren
        ? (_isExpanded ? "▼" : "▶")
        : string.Empty;

    /// <summary>
    /// Command to toggle the expanded state of this node.
    /// Set by the ViewModel to allow controlling the flat collection.
    /// </summary>
    public ICommand ToggleCommand { get; set; }

    // ── Visual helpers ────────────────────────────────────────────────────────

    /// <summary>Left padding in pixels for visual indentation (16px per level).</summary>
    public double IndentPadding => (int)Level * 18.0;

    /// <summary>Font size shrinks slightly at deeper levels for visual hierarchy.</summary>
    public double FontSize => Level switch
    {
        NodeLevel.Segment      => 14,
        NodeLevel.Field        => 13,
        NodeLevel.Component    => 12,
        NodeLevel.SubComponent => 11,
        _                      => 13
    };

    /// <summary>True if the node has a non-empty value to display.</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>True if the node has a data type badge to display.</summary>
    public bool HasDataType => !string.IsNullOrEmpty(DataType)
        && DataType != "ST" && DataType != "ID" && DataType != "IS"
        && DataType != "NM" && DataType != "DT" && DataType != "DTM"
        && DataType != "SI" && DataType != "TX" && DataType != "FT"
        && DataType != "varies";

    // ── Display helpers ───────────────────────────────────────────────────────

    /// <summary>Display label for the node (notation + name).</summary>
    public string DisplayLabel
    {
        get
        {
            if (!string.IsNullOrEmpty(Name))
                return $"{Notation}   {Name}";
            return Notation;
        }
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public Hl7TreeNode()
    {
        ToggleCommand = new Command(() =>
        {
            if (HasChildren)
                IsExpanded = !IsExpanded;
        });
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
