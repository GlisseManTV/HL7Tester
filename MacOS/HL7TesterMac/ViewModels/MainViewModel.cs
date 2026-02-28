using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HL7Tester.Core;
using Microsoft.Extensions.Logging;

namespace HL7TesterMac.ViewModels;

/// <summary>
/// ViewModel principal pour la page de génération de messages HL7.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AdtMessageGenerator _generator;
    private readonly INetworkSettingsService _networkSettingsService;
    private readonly IHL7NetworkSender _networkSender;
    private readonly ILogger<MainViewModel> _logger;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> AvailableMessageTypes { get; } =
        new([
            "ADT A01 - Inpatient or Day Hospital Admission",
            "ADT A02 - Patient Movement",
            "ADT A03 - Discharge",
            "ADT A04 - Outpatient Admission",
            "ADT A05 - Pre-admission",
            "ADT A06 - Transformation of an Outpatient Visit into Admission",
            "ADT A07 - Transformation of an Admission into Outpatient Visit",
            "ADT A08 - Update Patient Stay",
            "ADT A09 - Temporary Movement",
            "ADT A10 - Return from Temporary Movement",
            "ADT A11 - Admission Cancellation",
            "ADT A12 - Movement Cancellation",
            "ADT A13 - Discharge Cancellation",
            "ADT A14 - Scheduled Admission in the Future (not used)",
            "ADT A15 - Scheduled Movement in the Future (not used)",
            "ADT A16 - Scheduled Discharge in the Future (not used)",
            "ADT A18 - Merge Patient Records",
            "ADT A21 - Leave of Absence Departure",
            "ADT A22 - Return from Leave of Absence",
            "ADT A24 - Link between Two Patients (not used)",
            "ADT A25 - Cancellation of Future Scheduled Admission (not used)",
            "ADT A26 - Cancellation of Future Scheduled Movement (not used)",
            "ADT A27 - Cancellation of Future Scheduled Discharge (not used)",
            "ADT A28 - Patient Creation",
            "ADT A31 - Update Patient",
            "ADT A32 - Cancellation of a Return from Temporary Movement",
            "ADT A33 - Cancellation of Temporary Movement",
            "ADT A37 - Cancellation of a Patient Link (not used)",
            "ADT A38 - Pre-admission Cancellation",
            "ADT A40 - Patient Record Merge"
        ]);

    private string _selectedMessageType;
    public string SelectedMessageType
    {
        get => _selectedMessageType;
        set
        {
            if (SetField(ref _selectedMessageType, value))
            {
                UpdateUiForSelectedMessageType();
            }
        }
    }

    private string _patientIdLabel = "Patient ID";
    public string PatientIdLabel
    {
        get => _patientIdLabel;
        private set => SetField(ref _patientIdLabel, value);
    }

    private string _patientId = string.Empty;
    public string PatientId
    {
        get => _patientId;
        set => SetField(ref _patientId, value);
    }

    private string? _newPatientId;
    public string? NewPatientId
    {
        get => _newPatientId;
        set => SetField(ref _newPatientId, value);
    }

    private string _patientFamilyName = string.Empty;
    public string PatientFamilyName
    {
        get => _patientFamilyName;
        set => SetField(ref _patientFamilyName, value);
    }

    private string _patientGivenName = string.Empty;
    public string PatientGivenName
    {
        get => _patientGivenName;
        set => SetField(ref _patientGivenName, value);
    }

    private string _birthDate = string.Empty;
    public string BirthDate
    {
        get => _birthDate;
        set => SetField(ref _birthDate, value);
    }

    private string _sex = string.Empty;
    public string Sex
    {
        get => _sex;
        set => SetField(ref _sex, value);
    }

    private string _unit = string.Empty;
    public string Unit
    {
        get => _unit;
        set => SetField(ref _unit, value);
    }

    private string _room = string.Empty;
    public string Room
    {
        get => _room;
        set => SetField(ref _room, value);
    }

    private string _bed = string.Empty;
    public string Bed
    {
        get => _bed;
        set => SetField(ref _bed, value);
    }

    private string _facility = string.Empty;
    public string Facility
    {
        get => _facility;
        set => SetField(ref _facility, value);
    }

    private string _floor = string.Empty;
    public string Floor
    {
        get => _floor;
        set => SetField(ref _floor, value);
    }

    private string _admissionNumber = string.Empty;
    public string AdmissionNumber
    {
        get => _admissionNumber;
        set => SetField(ref _admissionNumber, value);
    }

    private string? _eventDateTime;
    /// <summary>
    /// Date/heure d'événement EVN-6-1 au format HL7 (yyyyMMddHHmm).
    /// Si null ou vide, la date courante sera utilisée côté Core.
    /// </summary>
    public string? EventDateTime
    {
        get => _eventDateTime;
        set => SetField(ref _eventDateTime, value);
    }

    private string _generatedMessage = string.Empty;
    public string GeneratedMessage
    {
        get => _generatedMessage;
        set => SetField(ref _generatedMessage, value);
    }

    private string _sendLog = string.Empty;
    /// <summary>
    /// Journal des envois (succès / erreurs), séparé du message HL7 lui-même.
    /// </summary>
    public string SendLog
    {
        get => _sendLog;
        set => SetField(ref _sendLog, value);
    }

    // OBX (jusqu'à 3 lignes comme dans l'UI WinForms actuelle)

    private string? _obx1Type;
    public string? Obx1Type
    {
        get => _obx1Type;
        set => SetField(ref _obx1Type, value);
    }

    private string? _obx1Reason;
    public string? Obx1Reason
    {
        get => _obx1Reason;
        set => SetField(ref _obx1Reason, value);
    }

    private string? _obx2Type;
    public string? Obx2Type
    {
        get => _obx2Type;
        set => SetField(ref _obx2Type, value);
    }

    private string? _obx2Reason;
    public string? Obx2Reason
    {
        get => _obx2Reason;
        set => SetField(ref _obx2Reason, value);
    }

    private string? _obx3Type;
    public string? Obx3Type
    {
        get => _obx3Type;
        set => SetField(ref _obx3Type, value);
    }

    private string? _obx3Reason;
    public string? Obx3Reason
    {
        get => _obx3Reason;
        set => SetField(ref _obx3Reason, value);
    }

    private bool _isNewPatientIdVisible;
    public bool IsNewPatientIdVisible
    {
        get => _isNewPatientIdVisible;
        private set => SetField(ref _isNewPatientIdVisible, value);
    }

    public ICommand GenerateCommand { get; }
    public ICommand SendCommand { get; }

    public MainViewModel(AdtMessageGenerator generator,
                         INetworkSettingsService networkSettingsService,
                         IHL7NetworkSender networkSender,
                         ILogger<MainViewModel> logger)
    {
        _generator = generator;
        _networkSettingsService = networkSettingsService;
        _networkSender = networkSender;
        _logger = logger;
        _selectedMessageType = AvailableMessageTypes.First();
        UpdateUiForSelectedMessageType();
        GenerateCommand = new Command(OnGenerate);
        SendCommand = new Command(async () => await OnSendAsync());
    }

    private void OnGenerate()
    {
        try
        {
            var request = new AdtMessageRequest
            {
                MessageTypeCode = SelectedMessageType,
                PatientId = PatientId ?? string.Empty,
                NewPatientId = NewPatientId,
                PatientFamilyName = PatientFamilyName ?? string.Empty,
                PatientGivenName = PatientGivenName ?? string.Empty,
                BirthDate = BirthDate ?? string.Empty,
                Sex = Sex ?? string.Empty,
                Unit = Unit ?? string.Empty,
                Room = Room ?? string.Empty,
                Bed = Bed ?? string.Empty,
                Facility = Facility ?? string.Empty,
                Floor = Floor ?? string.Empty,
                AdmissionNumber = AdmissionNumber ?? string.Empty,
                EventDateTime = string.IsNullOrWhiteSpace(EventDateTime) ? null : EventDateTime
            };

            // OBX 1..3
            request.ObxEntries.Add(new ObxEntry { Type = Obx1Type, Reason = Obx1Reason });
            request.ObxEntries.Add(new ObxEntry { Type = Obx2Type, Reason = Obx2Reason });
            request.ObxEntries.Add(new ObxEntry { Type = Obx3Type, Reason = Obx3Reason });

            var hl7 = _generator.Generate(request);

            _logger.LogDebug("HL7 message generated. Type={MessageType}", request.MessageTypeCode);
            // Pour l'affichage, on remplace les CR par des sauts de ligne de la plateforme.
            GeneratedMessage = hl7.Replace("\r", Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating HL7 message.");
            GeneratedMessage = $"Error generating message: {ex.Message}";
        }
    }

    private async Task OnSendAsync()
    {
        try
        {
            // On envoie simplement le message déjà généré dans l'UI (comme le WinForms)
            var messageToSend = (_generatedMessage ?? string.Empty).Replace(Environment.NewLine, "\r");
            if (string.IsNullOrWhiteSpace(messageToSend))
            {
                _logger.LogWarning("Send requested but HL7 message is empty.");
                AppendToSendLog("[Send] Nothing to send (HL7 message is empty).");
                return;
            }

            // 2. Récupérer la config réseau
            var settings = await _networkSettingsService.LoadAsync();
            var ip = settings.LastIpAddress ?? string.Empty;
            var portStr = settings.LastPort ?? string.Empty;

            if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
            {
                _logger.LogWarning("Invalid port configured: {Port}", portStr);
                AppendToSendLog("[Send] Invalid port configured.");
                return;
            }

            // 3. Envoyer via TCP/MLLP
            _logger.LogInformation("Sending HL7 message to {Ip}:{Port}.", ip, port);
            _logger.LogDebug("HL7 message length: {Length} characters.", messageToSend.Length);
            await _networkSender.SendAsync(messageToSend, ip, port);
            _logger.LogInformation("HL7 message successfully sent to {Ip}:{Port}.", ip, port);
            AppendToSendLog($"[Send] Message sent to {ip}:{port}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending HL7 message.");
            AppendToSendLog($"[Send] Error: {ex.Message}");
        }
    }

    private void UpdateUiForSelectedMessageType()
    {
        var type = SelectedMessageType ?? string.Empty;

        if (string.IsNullOrWhiteSpace(type))
        {
            // Cas par défaut quand aucun type n'est sélectionné
            PatientIdLabel = "Patient ID";
            IsNewPatientIdVisible = false;
            return;
        }

        // Pour A18 / A40 (merge), on affiche explicitement "Old PatientID" et le champ NewPatientId.
        // Pour les autres types, le label reste simplement "Patient ID" et NewPatientId est caché.
        var isMergeType =
            type.StartsWith("ADT A40", StringComparison.OrdinalIgnoreCase) ||
            type.StartsWith("ADT A18", StringComparison.OrdinalIgnoreCase);

        PatientIdLabel = isMergeType ? "Old PatientID" : "Patient ID";
        IsNewPatientIdVisible = isMergeType;
    }

    private void AppendToSendLog(string line)
    {
        if (string.IsNullOrWhiteSpace(_sendLog))
        {
            SendLog = line;
        }
        else
        {
            SendLog = _sendLog + Environment.NewLine + line;
        }
    }

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
