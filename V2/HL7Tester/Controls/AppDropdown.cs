using System.Collections;
using System.ComponentModel;
using Microsoft.Maui.Controls.Shapes;

namespace HL7Tester.Controls;

/// <summary>
/// Platform-aware dropdown: the native MAUI Picker on Windows (the WinUI flyout UX is
/// better there), the inline <see cref="DropdownButton"/> on Mac Catalyst (immune to the
/// UIKit popover auto-dismiss bug after Tab navigation).
/// Exposes the same bindable surface as DropdownButton so pages bind identically on both
/// platforms; ShowSearch/MaxListHeight are inline-only and simply ignored on Windows.
/// </summary>
public class AppDropdown : Border
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(AppDropdown),
            propertyChanged: (b, _, _) => ((AppDropdown)b).PushItemsSource());

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(AppDropdown),
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, n) => ((AppDropdown)b).OnWrapperSelectedItemChanged(n));

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(AppDropdown), -1,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, n) => ((AppDropdown)b).OnWrapperSelectedIndexChanged((int)n));

    public static readonly BindableProperty DisplayConverterProperty =
        BindableProperty.Create(nameof(DisplayConverter), typeof(IValueConverter), typeof(AppDropdown),
            propertyChanged: (b, _, _) => ((AppDropdown)b).PushDisplay());

    public static readonly BindableProperty DisplayMemberPathProperty =
        BindableProperty.Create(nameof(DisplayMemberPath), typeof(string), typeof(AppDropdown),
            propertyChanged: (b, _, _) => ((AppDropdown)b).PushDisplay());

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(AppDropdown), null,
            propertyChanged: (b, _, _) => ((AppDropdown)b).PushPlaceholder());

    public static readonly BindableProperty MaxListHeightProperty =
        BindableProperty.Create(nameof(MaxListHeight), typeof(double), typeof(AppDropdown), 240d,
            propertyChanged: (b, _, _) => ((AppDropdown)b).PushInlineSettings());

    public static readonly BindableProperty ShowSearchProperty =
        BindableProperty.Create(nameof(ShowSearch), typeof(bool), typeof(AppDropdown), false,
            propertyChanged: (b, _, _) => ((AppDropdown)b).PushInlineSettings());

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public IValueConverter? DisplayConverter
    {
        get => (IValueConverter?)GetValue(DisplayConverterProperty);
        set => SetValue(DisplayConverterProperty, value);
    }

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    /// <summary>
    /// Null by default: on Windows, Picker.Title renders as a header above the value,
    /// so it is only applied when explicitly set (same look as a bare native Picker).
    /// The inline implementation falls back to "Select..." when no placeholder is given.
    /// </summary>
    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Inline implementation only (Mac Catalyst): ignored on Windows.</summary>
    public double MaxListHeight
    {
        get => (double)GetValue(MaxListHeightProperty);
        set => SetValue(MaxListHeightProperty, value);
    }

    /// <summary>Inline implementation only (Mac Catalyst): ignored on Windows.</summary>
    public bool ShowSearch
    {
        get => (bool)GetValue(ShowSearchProperty);
        set => SetValue(ShowSearchProperty, value);
    }

    private readonly Picker? _picker;
    private readonly DropdownButton? _inline;
    private bool _syncGuard;

    public AppDropdown()
    {
        // Pure container: the selected implementation paints its own chrome.
        StrokeThickness = 0;
        Padding = new Thickness(0);
        Background = new SolidColorBrush(Colors.Transparent);

        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            _picker = new Picker { HorizontalOptions = LayoutOptions.Fill };
            _picker.SelectedIndexChanged += OnPickerSelectedIndexChanged;
            Content = _picker;
        }
        else
        {
            _inline = new DropdownButton { HorizontalOptions = LayoutOptions.Fill };
            _inline.PropertyChanged += OnInlinePropertyChanged;
            Content = _inline;
        }

        // Pages may paint the wrapper Background for contrast (e.g. white on the purple
        // header). The wrapper must follow the inner control's corner radius, otherwise
        // square background corners peek out behind the rounded native/inline chrome.
        StrokeShape = new RoundRectangle { CornerRadius = _picker is not null ? 4 : 8 };

        PushAll();
    }

    private void PushAll()
    {
        PushItemsSource();
        PushDisplay();
        PushPlaceholder();
    }

    private void PushItemsSource()
    {
        if (_picker is not null)
        {
            // MAUI 10 types Picker.ItemsSource as IList: pass through lists/arrays/
            // ObservableCollections as-is (live updates), materialize other sequences.
            _picker.ItemsSource = ItemsSource switch
            {
                null => null,
                IList list => list,
                _ => ItemsSource.Cast<object?>().ToList()
            };
        }
        if (_inline is not null)
        {
            _inline.ItemsSource = ItemsSource;
        }

        // The native Picker resets its selection when ItemsSource changes; re-apply the
        // wrapper selection so the TwoWay contract survives list refreshes.
        PushSelection();
        PushInlineSettings();
    }

    private void PushDisplay()
    {
        // DropdownButton handles DisplayConverter/DisplayMemberPath itself; map them to
        // the Picker's ItemDisplayBinding for the Windows implementation.
        if (_picker is null)
        {
            return;
        }

        if (DisplayConverter is not null)
        {
            _picker.ItemDisplayBinding = new Binding(".", converter: DisplayConverter);
        }
        else if (!string.IsNullOrEmpty(DisplayMemberPath))
        {
            _picker.ItemDisplayBinding = new Binding(DisplayMemberPath);
        }
        else
        {
            _picker.ItemDisplayBinding = null;
        }
    }

    private void PushPlaceholder()
    {
        if (_picker is not null)
        {
            // WinUI renders Picker.Title as a header above the selected value: apply it
            // only when a Placeholder was explicitly provided, so pickers without one
            // render exactly like the original native Picker (no header text).
            _picker.Title = Placeholder;
        }
        if (_inline is not null)
        {
            _inline.Placeholder = Placeholder ?? "Select...";
        }
    }

    private void PushInlineSettings()
    {
        if (_inline is null)
        {
            return;
        }

        _inline.MaxListHeight = MaxListHeight;
        _inline.ShowSearch = ShowSearch;
    }

    private void PushSelection()
    {
        _syncGuard = true;
        try
        {
            if (_picker is not null)
            {
                if (SelectedItem is not null)
                {
                    _picker.SelectedItem = SelectedItem;
                }
                else
                {
                    _picker.SelectedIndex = SelectedIndex;
                }
            }
            if (_inline is not null)
            {
                if (SelectedItem is not null)
                {
                    _inline.SelectedItem = SelectedItem;
                }
                else
                {
                    _inline.SelectedIndex = SelectedIndex;
                }
            }
        }
        finally
        {
            _syncGuard = false;
        }
    }

    // ---- Wrapper-side cross-sync (SelectedItem <-> SelectedIndex), same contract as DropdownButton.

    private void OnWrapperSelectedItemChanged(object? newItem)
    {
        if (!_syncGuard)
        {
            _syncGuard = true;
            try
            {
                SelectedIndex = IndexOfItem(newItem);
            }
            finally
            {
                _syncGuard = false;
            }
        }

        PushSelection();
    }

    private void OnWrapperSelectedIndexChanged(int newIndex)
    {
        if (_syncGuard || newIndex < 0 || ItemsSource is null)
        {
            return;
        }

        var items = ItemsSource.Cast<object?>().ToList();
        if (newIndex < items.Count)
        {
            _syncGuard = true;
            try
            {
                SelectedItem = items[newIndex];
            }
            finally
            {
                _syncGuard = false;
            }
        }

        PushSelection();
    }

    // ---- Inner -> wrapper propagation (guarded against echo loops).

    private void OnPickerSelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_syncGuard || _picker is null)
        {
            return;
        }

        _syncGuard = true;
        try
        {
            SelectedIndex = _picker.SelectedIndex;
            SelectedItem = _picker.SelectedItem;
        }
        finally
        {
            _syncGuard = false;
        }
    }

    private void OnInlinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_syncGuard || _inline is null)
        {
            return;
        }

        if (e.PropertyName == nameof(DropdownButton.SelectedItem))
        {
            _syncGuard = true;
            try
            {
                SelectedItem = _inline.SelectedItem;
                SelectedIndex = _inline.SelectedIndex;
            }
            finally
            {
                _syncGuard = false;
            }
        }
        else if (e.PropertyName == nameof(DropdownButton.SelectedIndex))
        {
            _syncGuard = true;
            try
            {
                SelectedIndex = _inline.SelectedIndex;
                SelectedItem = _inline.SelectedItem;
            }
            finally
            {
                _syncGuard = false;
            }
        }
    }

    private int IndexOfItem(object? item)
    {
        if (ItemsSource is null || item is null)
        {
            return -1;
        }

        var index = 0;
        foreach (var candidate in ItemsSource)
        {
            if (Equals(candidate, item))
            {
                return index;
            }
            index++;
        }
        return -1;
    }
}
