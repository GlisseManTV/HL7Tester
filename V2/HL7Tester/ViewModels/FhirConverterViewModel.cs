using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HL7Tester.Core;
using HL7Tester.Core.Fhir;
using HL7Tester.Core.Inspector.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui.Graphics;

namespace HL7Tester.ViewModels;

/// <summary>
/// ViewModel for the bidirectional HL7v2 ⇄ FHIR converter page.
///
/// HL7 → FHIR : HL7ParserService.Parse → Hl7MessageExtractor.Extract →
///              IFhirResourceGenerator.Generate → ToJson.
/// FHIR → HL7 : FhirToHl7Converter.Convert → AdtMessageGenerator.Generate.
/// </summary>
public sealed class FhirConverterViewModel : INotifyPropertyChanged
{
    private static readonly Color ActiveHl7Color = Color.FromArgb("#512BD4");   // indigo / Primary
    private static readonly Color ActiveFhirColor = Color.FromArgb("#009688");  // teal
    private static readonly Color InactiveColor = Color.FromArgb("#9CA3AF");    // gray

    private readonly HL7ParserService _parserService;
    private readonly IFhirResourceGenerator _fhirGenerator;
    private readonly AdtMessageGenerator _hl7Generator;
    private readonly ILogger<FhirConverterViewModel> _logger;

    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Bindable state ────────────────────────────────────────────────────────

    private string _inputMessage = string.Empty;
    public string InputMessage
    {
        get => _inputMessage;
        set => SetField(ref _inputMessage, value);
    }

    private string _outputMessage = string.Empty;
    public string OutputMessage
    {
        get => _outputMessage;
        set
        {
            if (SetField(ref _outputMessage, value))
                OnPropertyChanged(nameof(HasOutput));
        }
    }

    public bool HasOutput => !string.IsNullOrEmpty(_outputMessage);

    private bool _isHl7ToFhir = true;
    public bool IsHl7ToFhir
    {
        get => _isHl7ToFhir;
        set
        {
            if (SetField(ref _isHl7ToFhir, value))
            {
                OnPropertyChanged(nameof(IsFhirToHl7));
                OnPropertyChanged(nameof(Hl7DirectionColor));
                OnPropertyChanged(nameof(FhirDirectionColor));
                OnPropertyChanged(nameof(InputPlaceholder));
                OnPropertyChanged(nameof(OutputLabel));
            }
        }
    }

    public bool IsFhirToHl7 => !IsHl7ToFhir;

    /// <summary>Background of the "HL7 → FHIR" toggle button.</summary>
    public Color Hl7DirectionColor => IsHl7ToFhir ? ActiveHl7Color : InactiveColor;

    /// <summary>Background of the "FHIR → HL7" toggle button.</summary>
    public Color FhirDirectionColor => IsFhirToHl7 ? ActiveFhirColor : InactiveColor;

    public string InputPlaceholder => IsHl7ToFhir
        ? "Paste a raw HL7 v2 message here (MSH|…)"
        : "Paste a FHIR R4 JSON resource here ({\"resourceType\": …})";

    public string OutputLabel => IsHl7ToFhir ? "FHIR JSON Output" : "HL7v2 Message Output";

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !IsBusy;

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand ConvertCommand { get; }
    public ICommand SwapDirectionCommand { get; }
    public ICommand CopyOutputCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand SendToMainPageCommand { get; }

    // ─────────────────────────────────────────────────────────────────────────

    public FhirConverterViewModel(
        HL7ParserService parserService,
        IFhirResourceGenerator fhirGenerator,
        AdtMessageGenerator hl7Generator,
        ILogger<FhirConverterViewModel>? logger = null)
    {
        _parserService = parserService;
        _fhirGenerator = fhirGenerator;
        _hl7Generator = hl7Generator;
        _logger = logger ?? NullLogger<FhirConverterViewModel>.Instance;

        ConvertCommand = new Command(Convert, () => IsNotBusy);
        SwapDirectionCommand = new Command(SwapDirection);
        CopyOutputCommand = new Command(async () => await CopyOutputAsync());
        ClearCommand = new Command(Clear);
        SendToMainPageCommand = new Command(async () => await SendToMainPageAsync());
    }

    // ── Conversion logic ──────────────────────────────────────────────────────

    private void Convert()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(InputMessage))
            {
                StatusMessage = "Input is empty — paste a message first.";
                return;
            }

            IsBusy = true;
            StatusMessage = string.Empty;

            if (IsHl7ToFhir)
            {
                var parsed = _parserService.Parse(InputMessage);
                if (!parsed.IsSuccess)
                {
                    StatusMessage = $"HL7 parse error: {parsed.ParseError}";
                    return;
                }

                var request = Hl7MessageExtractor.Extract(parsed);
                var resource = _fhirGenerator.Generate(request);
                OutputMessage = _fhirGenerator.ToJson(resource);

                StatusMessage = $"Converted {request.MessageTypeCode} → FHIR {resource.TypeName}.";
                _logger.LogInformation("HL7→FHIR conversion succeeded for type {Type}.", request.MessageTypeCode);
            }
            else
            {
                var request = FhirToHl7Converter.Convert(InputMessage);
                if (request is null)
                {
                    StatusMessage = "Unrecognized FHIR resource — expected Bundle, Patient, Encounter, ServiceRequest or Appointment.";
                    return;
                }

                OutputMessage = _hl7Generator.Generate(request);
                StatusMessage = $"Converted FHIR → HL7 {request.MessageTypeCode}.";
                _logger.LogInformation("FHIR→HL7 conversion succeeded for type {Type}.", request.MessageTypeCode);
            }
        }
        catch (Exception ex)
        {
            OutputMessage = string.Empty;
            StatusMessage = $"Conversion failed: {ex.Message}";
            _logger.LogError(ex, "FHIR converter failed.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SwapDirection()
    {
        // Chain conversions: move the output into the input and flip the direction.
        if (!string.IsNullOrEmpty(OutputMessage))
            InputMessage = OutputMessage;

        OutputMessage = string.Empty;
        StatusMessage = string.Empty;
        IsHl7ToFhir = !IsHl7ToFhir;
    }

    private async Task CopyOutputAsync()
    {
        if (string.IsNullOrEmpty(OutputMessage))
            return;

        await Clipboard.Default.SetTextAsync(OutputMessage);
        StatusMessage = "Output copied to clipboard.";
    }

    private void Clear()
    {
        InputMessage = string.Empty;
        OutputMessage = string.Empty;
        StatusMessage = string.Empty;
    }

    private async Task SendToMainPageAsync()
    {
        if (string.IsNullOrEmpty(OutputMessage))
        {
            StatusMessage = "Nothing to send — convert first.";
            return;
        }

        // Reuse the existing static bridge consumed by MainPage.OnAppearing.
        Hl7InspectorViewModel.PendingParsedMessage = OutputMessage;
        await Shell.Current.GoToAsync("//MainPage");
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value!;
        OnPropertyChanged(propertyName);
        return true;
    }
}
