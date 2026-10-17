using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace HL7Tester.Controls;

/// <summary>
/// Keyboard-and-mouse friendly inline dropdown for desktop platforms.
/// Replaces the native Picker on Mac Catalyst, whose UIKit popover gets
/// auto-dismissed when Tab navigation corrupts the first-responder chain.
/// The list here is a plain inline MAUI layout: no popover, no UIKit focus magic.
/// For large (e.g. DB-driven) lists, use MaxListHeight (internal scrolling)
/// and ShowSearch (filter box) to keep the layout shift bounded.
/// </summary>
public class DropdownButton : Border
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(DropdownButton),
            propertyChanged: (b, _, _) => ((DropdownButton)b).OnItemsSourceChanged());

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(DropdownButton),
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, n) => ((DropdownButton)b).OnSelectedItemChanged(n));

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(DropdownButton), -1,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, n) => ((DropdownButton)b).OnSelectedIndexChanged((int)n));

    public static readonly BindableProperty DisplayConverterProperty =
        BindableProperty.Create(nameof(DisplayConverter), typeof(IValueConverter), typeof(DropdownButton),
            propertyChanged: (b, _, _) => ((DropdownButton)b).UpdateHeaderText());

    public static readonly BindableProperty DisplayMemberPathProperty =
        BindableProperty.Create(nameof(DisplayMemberPath), typeof(string), typeof(DropdownButton),
            propertyChanged: (b, _, _) => ((DropdownButton)b).UpdateHeaderText());

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(DropdownButton), "Select...",
            propertyChanged: (b, _, _) => ((DropdownButton)b).UpdateHeaderText());

    public static readonly BindableProperty MaxListHeightProperty =
        BindableProperty.Create(nameof(MaxListHeight), typeof(double), typeof(DropdownButton), 240d,
            propertyChanged: (b, _, _) => ((DropdownButton)b).ResizeList());

    public static readonly BindableProperty ShowSearchProperty =
        BindableProperty.Create(nameof(ShowSearch), typeof(bool), typeof(DropdownButton), false,
            propertyChanged: (b, _, _) => ((DropdownButton)b).OnShowSearchChanged());

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

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>
    /// Maximum height of the opened item list. Beyond that, the list scrolls
    /// internally instead of pushing the page layout down. Default: 240.
    /// </summary>
    public double MaxListHeight
    {
        get => (double)GetValue(MaxListHeightProperty);
        set => SetValue(MaxListHeightProperty, value);
    }

    /// <summary>
    /// Shows a filter box at the top of the opened list. Recommended for
    /// large data-driven lists (DB queries, remote APIs). Default: false.
    /// </summary>
    public bool ShowSearch
    {
        get => (bool)GetValue(ShowSearchProperty);
        set => SetValue(ShowSearchProperty, value);
    }

    private readonly Button _headerButton;
    private readonly Border _listBorder;
    private readonly ScrollView _listScroll;
    private readonly VerticalStackLayout _listLayout;
    private readonly Entry _searchEntry;
    private bool _isOpen;
    private bool _syncGuard;
    private INotifyCollectionChanged? _trackedCollection;

    public DropdownButton()
    {
        StrokeShape = new RoundRectangle { CornerRadius = 8 };
        StrokeThickness = 1;
        Padding = new Thickness(2);
        Background = new SolidColorBrush(Colors.Transparent);

        // A real Button keeps the control reachable via Tab + Enter/Space,
        // unlike a bare tap gesture. The chevron is part of the text.
        _headerButton = new Button
        {
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            Padding = new Thickness(8, 6),
            MinimumHeightRequest = 36,
            HorizontalOptions = LayoutOptions.Fill
        };
        _headerButton.SetAppThemeColor(Button.TextColorProperty, Colors.Black, Colors.White);
        _headerButton.Clicked += (_, _) => Toggle();

        _searchEntry = new Entry
        {
            Placeholder = "Filter...",
            IsVisible = false,
            Margin = new Thickness(2, 0, 2, 2)
        };
        _searchEntry.TextChanged += (_, _) =>
        {
            RebuildList();
            ResizeList();
        };

        _listLayout = new VerticalStackLayout { Spacing = 2 };
        _listScroll = new ScrollView();
        _listScroll.Content = _listLayout;

        _listBorder = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            StrokeThickness = 1,
            Padding = new Thickness(4),
            Margin = new Thickness(0, 4, 0, 0),
            IsVisible = false,
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children = { _searchEntry, _listScroll }
            }
        };

        Content = new VerticalStackLayout
        {
            Spacing = 0,
            Children = { _headerButton, _listBorder }
        };

        UpdateThemeBrushes();
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged += (_, _) => UpdateThemeBrushes();
        }

        UpdateHeaderText();
    }

    private void Toggle()
    {
        if (_isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private void Open()
    {
        _searchEntry.IsVisible = ShowSearch;
        _searchEntry.Text = string.Empty;
        RebuildList();
        ResizeList();
        _listBorder.IsVisible = true;
        _isOpen = true;
        UpdateHeaderText();
    }

    private void Close()
    {
        _listBorder.IsVisible = false;
        _isOpen = false;
        UpdateHeaderText();
    }

    private void RebuildList()
    {
        _listLayout.Children.Clear();
        if (ItemsSource is null)
        {
            return;
        }

        var filter = _searchEntry.Text;
        foreach (var item in ItemsSource)
        {
            var display = ToDisplay(item);
            if (!string.IsNullOrEmpty(filter)
                && !display.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var captured = item;
            var itemButton = new Button
            {
                Text = display,
                BackgroundColor = Colors.Transparent,
                BorderWidth = 0,
                Padding = new Thickness(8, 4),
                FontSize = 13,
                MinimumHeightRequest = 30,
                HorizontalOptions = LayoutOptions.Fill
            };
            itemButton.SetAppThemeColor(Button.TextColorProperty, Colors.Black, Colors.White);

            if (Equals(item, SelectedItem))
            {
                itemButton.FontAttributes = FontAttributes.Bold;
            }

            itemButton.Clicked += (_, _) =>
            {
                SelectedItem = captured;
                Close();
            };
            _listLayout.Children.Add(itemButton);
        }
    }

    /// <summary>
    /// Sizes the list viewport to the actual measured content height, capped at
    /// MaxListHeight (measuring avoids the gap an estimate would leave under the
    /// last item when rendered items are shorter than expected).
    /// </summary>
    private void ResizeList()
    {
        if (_listLayout.Children.Count == 0)
        {
            _listScroll.HeightRequest = 0;
            return;
        }

        var measured = _listLayout.Measure(double.PositiveInfinity, double.PositiveInfinity).Height;
        _listScroll.HeightRequest = double.IsNaN(measured) || measured <= 0
            ? MaxListHeight
            : Math.Min(measured, MaxListHeight);
    }

    private string ToDisplay(object? item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        if (DisplayConverter is not null)
        {
            var converted = DisplayConverter.Convert(item, typeof(string), null, CultureInfo.InvariantCulture);
            var text = converted?.ToString();
            if (!string.IsNullOrEmpty(text))
            {
                return text!;
            }
        }

        if (!string.IsNullOrEmpty(DisplayMemberPath))
        {
            var value = item.GetType().GetProperty(DisplayMemberPath)?.GetValue(item);
            return value?.ToString() ?? item.ToString() ?? string.Empty;
        }

        return item.ToString() ?? string.Empty;
    }

    private void UpdateHeaderText()
    {
        var caption = SelectedItem is null ? Placeholder : ToDisplay(SelectedItem);
        _headerButton.Text = $"{caption} {(_isOpen ? "⌃" : "⌄")}";
    }

    private void OnItemsSourceChanged()
    {
        // Track collection mutations (e.g. ObservableCollection refreshed from a DB query).
        if (_trackedCollection is not null)
        {
            _trackedCollection.CollectionChanged -= OnCollectionChanged;
            _trackedCollection = null;
        }
        if (ItemsSource is INotifyCollectionChanged notify)
        {
            notify.CollectionChanged += OnCollectionChanged;
            _trackedCollection = notify;
        }

        UpdateHeaderText();
        if (_isOpen)
        {
            RebuildList();
            ResizeList();
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateHeaderText();
        if (_isOpen)
        {
            RebuildList();
            ResizeList();
        }
    }

    private void OnShowSearchChanged()
    {
        _searchEntry.IsVisible = ShowSearch && _isOpen;
    }

    private void OnSelectedItemChanged(object? newItem)
    {
        UpdateHeaderText();

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

        if (_isOpen)
        {
            RebuildList();
        }
    }

    private void OnSelectedIndexChanged(int newIndex)
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

    private void UpdateThemeBrushes()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        // The implicit Border style is not applied to derived types, so the control
        // outline is painted explicitly (same palette as CardBorder).
        Stroke = isDark
            ? new SolidColorBrush(Color.FromArgb("#404040"))
            : new SolidColorBrush(Color.FromArgb("#C8C8C8"));

        _listBorder.Background = isDark
            ? new SolidColorBrush(Color.FromArgb("#212121"))
            : new SolidColorBrush(Colors.White);
        _listBorder.Stroke = isDark
            ? new SolidColorBrush(Color.FromArgb("#404040"))
            : new SolidColorBrush(Color.FromArgb("#C8C8C8"));
    }
}
