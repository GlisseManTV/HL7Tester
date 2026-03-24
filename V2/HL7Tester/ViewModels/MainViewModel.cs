using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HL7Tester.Core;
using Microsoft.Extensions.Logging;

namespace HL7Tester.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> MessageTypesByFamily =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ADT"] =
            [
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
            ],
            ["ORM"] =
            [
                "ORM O01 - General Order Message"
            ],
            ["SIU"] =
            [
                "SIU S12 - Notification of New Appointment Booking",
                "SIU S13 - Notification of Appointment Rescheduling",
                "SIU S14 - Notification of Appointment Modification",
                "SIU S15 - Notification of Appointment Cancellation"
            ]
        };

    private readonly AdtMessageGenerator _generator;
    private readonly INetworkSettingsService _networkSettingsService;
    private readonly IHL7NetworkSender _networkSender;
    private readonly ILogger<MainViewModel> _logger;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> AvailableMessageFamilies { get; } = new(["ADT", "ORM", "SIU"]);
    public ObservableCollection<string> AvailableMessageTypes { get; } = new();

    private string _selectedMessageFamily = "ADT";
    public string SelectedMessageFamily
    {
        get => _selectedMessageFamily;
        set
        {
            var safeValue = string.IsNullOrWhiteSpace(value) ? "ADT" : value;

            if (SetField(ref _selectedMessageFamily, safeValue))
            {
                RefreshMessageTypes();
                UpdateUiForSelectedContext();
            }
        }
    }

    private string _selectedMessageType = string.Empty;
    public string SelectedMessageType
    {
        get => _selectedMessageType;
        set
        {
            var safeValue = value ?? string.Empty;

            if (SetField(ref _selectedMessageType, safeValue))
            {
                UpdateUiForSelectedContext();
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
    public string SendLog
    {
        get => _sendLog;
        set => SetField(ref _sendLog, value);
    }

    private bool _isSendLogExpanded = false; // Collapsed by default as per user request
    public bool IsSendLogExpanded
    {
        get => _isSendLogExpanded;
        set => SetField(ref _isSendLogExpanded, value);
    }

    // Height calculation: ~23px per line (including padding) for up to 5 lines max
    private const double LineHeight = 23;
    private const int MaxLogLines = 5;
    
    private double _sendLogHeightRequest = LineHeight; // 1 line when collapsed
    public double SendLogHeightRequest
    {
        get => _sendLogHeightRequest;
        set => SetField(ref _sendLogHeightRequest, value);
    }

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

    private bool _isAdtMode;
    public bool IsAdtMode
    {
        get => _isAdtMode;
        private set => SetField(ref _isAdtMode, value);
    }

    private bool _isNonAdtMode;
    public bool IsNonAdtMode
    {
        get => _isNonAdtMode;
        private set => SetField(ref _isNonAdtMode, value);
    }

    private bool _isEventDateTimeVisible;
    public bool IsEventDateTimeVisible
    {
        get => _isEventDateTimeVisible;
        private set => SetField(ref _isEventDateTimeVisible, value);
    }

    private bool _isOrmSectionVisible;
    public bool IsOrmSectionVisible
    {
        get => _isOrmSectionVisible;
        private set => SetField(ref _isOrmSectionVisible, value);
    }

    private bool _isSiuSectionVisible;
    public bool IsSiuSectionVisible
    {
        get => _isSiuSectionVisible;
        private set => SetField(ref _isSiuSectionVisible, value);
    }

    private string _orderControl = "NW";
    public string OrderControl
    {
        get => _orderControl;
        set => SetField(ref _orderControl, value);
    }

    private string _ormPlacerOrderNumber = string.Empty;
    public string OrmPlacerOrderNumber
    {
        get => _ormPlacerOrderNumber;
        set => SetField(ref _ormPlacerOrderNumber, value);
    }

    private string _ormFillerOrderNumber = string.Empty;
    public string OrmFillerOrderNumber
    {
        get => _ormFillerOrderNumber;
        set => SetField(ref _ormFillerOrderNumber, value);
    }

    private string _ormOrderStatus = "Final";
    public string OrmOrderStatus
    {
        get => _ormOrderStatus;
        set => SetField(ref _ormOrderStatus, value);
    }

    private string _ormUniversalServiceId = string.Empty;
    public string OrmUniversalServiceId
    {
        get => _ormUniversalServiceId;
        set => SetField(ref _ormUniversalServiceId, value);
    }

    private string? _ormRequestedDateTime;
    public string? OrmRequestedDateTime
    {
        get => _ormRequestedDateTime;
        set => SetField(ref _ormRequestedDateTime, value);
    }

    private string _ormOrderingProviderId = string.Empty;
    public string OrmOrderingProviderId
    {
        get => _ormOrderingProviderId;
        set => SetField(ref _ormOrderingProviderId, value);
    }

    private string _ormOrderingProviderFamilyName = string.Empty;
    public string OrmOrderingProviderFamilyName
    {
        get => _ormOrderingProviderFamilyName;
        set => SetField(ref _ormOrderingProviderFamilyName, value);
    }

    private string _ormOrderingProviderGivenName = string.Empty;
    public string OrmOrderingProviderGivenName
    {
        get => _ormOrderingProviderGivenName;
        set => SetField(ref _ormOrderingProviderGivenName, value);
    }

    private string _ormDiagnosisCode = string.Empty;
    public string OrmDiagnosisCode
    {
        get => _ormDiagnosisCode;
        set => SetField(ref _ormDiagnosisCode, value);
    }

    private string _ormDiagnosisText = string.Empty;
    public string OrmDiagnosisText
    {
        get => _ormDiagnosisText;
        set => SetField(ref _ormDiagnosisText, value);
    }

    private string _siuPlacerAppointmentId = string.Empty;
    public string SiuPlacerAppointmentId
    {
        get => _siuPlacerAppointmentId;
        set => SetField(ref _siuPlacerAppointmentId, value);
    }

    private string _siuFillerAppointmentId = string.Empty;
    public string SiuFillerAppointmentId
    {
        get => _siuFillerAppointmentId;
        set => SetField(ref _siuFillerAppointmentId, value);
    }

    private string _siuOccurrenceNumber = "1";
    public string SiuOccurrenceNumber
    {
        get => _siuOccurrenceNumber;
        set => SetField(ref _siuOccurrenceNumber, value);
    }

    private string _siuAppointmentType = "OFFICE^Office visit";
    public string SiuAppointmentType
    {
        get => _siuAppointmentType;
        set => SetField(ref _siuAppointmentType, value);
    }

    private string _siuAppointmentReason = string.Empty;
    public string SiuAppointmentReason
    {
        get => _siuAppointmentReason;
        set => SetField(ref _siuAppointmentReason, value);
    }

    private string? _siuAppointmentStartDateTime;
    public string? SiuAppointmentStartDateTime
    {
        get => _siuAppointmentStartDateTime;
        set => SetField(ref _siuAppointmentStartDateTime, value);
    }

    private string? _siuAppointmentEndDateTime;
    public string? SiuAppointmentEndDateTime
    {
        get => _siuAppointmentEndDateTime;
        set => SetField(ref _siuAppointmentEndDateTime, value);
    }

    private string _siuAppointmentDuration = "60";
    public string SiuAppointmentDuration
    {
        get => _siuAppointmentDuration;
        set => SetField(ref _siuAppointmentDuration, value);
    }

    private string _siuAppointmentDurationUnits = "m";
    public string SiuAppointmentDurationUnits
    {
        get => _siuAppointmentDurationUnits;
        set => SetField(ref _siuAppointmentDurationUnits, value);
    }

    private string _siuAppointmentStatus = "Scheduled";
    public string SiuAppointmentStatus
    {
        get => _siuAppointmentStatus;
        set => SetField(ref _siuAppointmentStatus, value);
    }

    private string _siuGeneralResourceId = string.Empty;
    public string SiuGeneralResourceId
    {
        get => _siuGeneralResourceId;
        set => SetField(ref _siuGeneralResourceId, value);
    }

    private string _siuGeneralResourceName = string.Empty;
    public string SiuGeneralResourceName
    {
        get => _siuGeneralResourceName;
        set => SetField(ref _siuGeneralResourceName, value);
    }

    private string _siuGeneralResourceType = "D";
    public string SiuGeneralResourceType
    {
        get => _siuGeneralResourceType;
        set => SetField(ref _siuGeneralResourceType, value);
    }

    private string _siuLocationCode = string.Empty;
    public string SiuLocationCode
    {
        get => _siuLocationCode;
        set => SetField(ref _siuLocationCode, value);
    }

    private string _siuLocationDescription = string.Empty;
    public string SiuLocationDescription
    {
        get => _siuLocationDescription;
        set => SetField(ref _siuLocationDescription, value);
    }

    private string _siuLocationSite = string.Empty;
    public string SiuLocationSite
    {
        get => _siuLocationSite;
        set => SetField(ref _siuLocationSite, value);
    }

    private string _siuPersonnelId = string.Empty;
    public string SiuPersonnelId
    {
        get => _siuPersonnelId;
        set => SetField(ref _siuPersonnelId, value);
    }

    private string _siuPersonnelFamilyName = string.Empty;
    public string SiuPersonnelFamilyName
    {
        get => _siuPersonnelFamilyName;
        set => SetField(ref _siuPersonnelFamilyName, value);
    }

    private string _siuPersonnelGivenName = string.Empty;
    public string SiuPersonnelGivenName
    {
        get => _siuPersonnelGivenName;
        set => SetField(ref _siuPersonnelGivenName, value);
    }

    private string _siuPersonnelRole = "D";
    public string SiuPersonnelRole
    {
        get => _siuPersonnelRole;
        set => SetField(ref _siuPersonnelRole, value);
    }

    public ICommand GenerateCommand { get; }
    public ICommand SendCommand { get; }

    public MainViewModel(
        AdtMessageGenerator generator,
        INetworkSettingsService networkSettingsService,
        IHL7NetworkSender networkSender,
        ILogger<MainViewModel> logger)
    {
        _generator = generator;
        _networkSettingsService = networkSettingsService;
        _networkSender = networkSender;
        _logger = logger;

        RefreshMessageTypes();
        UpdateUiForSelectedContext();

        GenerateCommand = new Command(OnGenerate);
        SendCommand = new Command(async () => await OnSendAsync());
    }

    private void RefreshMessageTypes()
    {
        AvailableMessageTypes.Clear();

        var familyKey = string.IsNullOrWhiteSpace(SelectedMessageFamily) ? "ADT" : SelectedMessageFamily;

        if (!MessageTypesByFamily.TryGetValue(familyKey, out var familyTypes))
        {
            familyTypes = MessageTypesByFamily["ADT"];
        }

        foreach (var type in familyTypes)
        {
            AvailableMessageTypes.Add(type);
        }

        if (!AvailableMessageTypes.Contains(SelectedMessageType) && AvailableMessageTypes.Count > 0)
        {
            SelectedMessageType = AvailableMessageTypes[0];
        }
        else if (AvailableMessageTypes.Count == 0)
        {
            SelectedMessageType = string.Empty;
        }
    }

    private void UpdateUiForSelectedContext()
    {
        bool isAdt = string.Equals(SelectedMessageFamily, "ADT", StringComparison.OrdinalIgnoreCase);
        bool isOrm = string.Equals(SelectedMessageFamily, "ORM", StringComparison.OrdinalIgnoreCase);
        bool isSiu = string.Equals(SelectedMessageFamily, "SIU", StringComparison.OrdinalIgnoreCase);

        IsAdtMode = isAdt;
        IsNonAdtMode = !isAdt;
        IsEventDateTimeVisible = isAdt;
        IsOrmSectionVisible = isOrm;
        IsSiuSectionVisible = isSiu;

        string selectedType = SelectedMessageType ?? string.Empty;

        bool isMergeAdtType =
            isAdt &&
            (selectedType.StartsWith("ADT A18", StringComparison.OrdinalIgnoreCase) ||
             selectedType.StartsWith("ADT A40", StringComparison.OrdinalIgnoreCase));

        PatientIdLabel = isMergeAdtType ? "Old Patient ID" : "Patient ID";
        IsNewPatientIdVisible = isMergeAdtType;
    }

    // Toggle method for Send log visibility
    public void ToggleSendLog()
    {
        IsSendLogExpanded = !IsSendLogExpanded;
        
        if (IsSendLogExpanded)
        {
            // Set height to show up to MaxLogLines when expanded
            SendLogHeightRequest = LineHeight * Math.Min(MaxLogLines, 5);
        }
        else
        {
            // Set to minimal height when collapsed
            SendLogHeightRequest = LineHeight;
        }
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
                EventDateTime = IsEventDateTimeVisible && !string.IsNullOrWhiteSpace(EventDateTime) ? EventDateTime : null,

                OrderControl = OrderControl ?? "NW",
                OrmPlacerOrderNumber = OrmPlacerOrderNumber ?? string.Empty,
                OrmFillerOrderNumber = OrmFillerOrderNumber ?? string.Empty,
                OrmOrderStatus = OrmOrderStatus ?? "Final",
                OrmUniversalServiceId = OrmUniversalServiceId ?? string.Empty,
                OrmRequestedDateTime = string.IsNullOrWhiteSpace(OrmRequestedDateTime) ? null : OrmRequestedDateTime,
                OrmOrderingProviderId = OrmOrderingProviderId ?? string.Empty,
                OrmOrderingProviderFamilyName = OrmOrderingProviderFamilyName ?? string.Empty,
                OrmOrderingProviderGivenName = OrmOrderingProviderGivenName ?? string.Empty,
                OrmDiagnosisCode = OrmDiagnosisCode ?? string.Empty,
                OrmDiagnosisText = OrmDiagnosisText ?? string.Empty,

                SiuPlacerAppointmentId = SiuPlacerAppointmentId ?? string.Empty,
                SiuFillerAppointmentId = SiuFillerAppointmentId ?? string.Empty,
                SiuOccurrenceNumber = SiuOccurrenceNumber ?? "1",
                SiuAppointmentType = SiuAppointmentType ?? "OFFICE^Office visit",
                SiuAppointmentReason = SiuAppointmentReason ?? string.Empty,
                SiuAppointmentStartDateTime = string.IsNullOrWhiteSpace(SiuAppointmentStartDateTime) ? null : SiuAppointmentStartDateTime,
                SiuAppointmentEndDateTime = string.IsNullOrWhiteSpace(SiuAppointmentEndDateTime) ? null : SiuAppointmentEndDateTime,
                SiuAppointmentDuration = SiuAppointmentDuration ?? "60",
                SiuAppointmentDurationUnits = SiuAppointmentDurationUnits ?? "m",
                SiuAppointmentStatus = SiuAppointmentStatus ?? "Scheduled",
                SiuGeneralResourceId = SiuGeneralResourceId ?? string.Empty,
                SiuGeneralResourceName = SiuGeneralResourceName ?? string.Empty,
                SiuGeneralResourceType = SiuGeneralResourceType ?? "D",
                SiuLocationCode = SiuLocationCode ?? string.Empty,
                SiuLocationDescription = SiuLocationDescription ?? string.Empty,
                SiuLocationSite = SiuLocationSite ?? string.Empty,
                SiuPersonnelId = SiuPersonnelId ?? string.Empty,
                SiuPersonnelFamilyName = SiuPersonnelFamilyName ?? string.Empty,
                SiuPersonnelGivenName = SiuPersonnelGivenName ?? string.Empty,
                SiuPersonnelRole = SiuPersonnelRole ?? "D"
            };

            request.ObxEntries.Add(new ObxEntry { Type = Obx1Type, Reason = Obx1Reason });
            request.ObxEntries.Add(new ObxEntry { Type = Obx2Type, Reason = Obx2Reason });
            request.ObxEntries.Add(new ObxEntry { Type = Obx3Type, Reason = Obx3Reason });

            string hl7 = _generator.Generate(request);
            _logger.LogDebug("HL7 message generated. Type={MessageType}", request.MessageTypeCode);
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
            string messageToSend = (GeneratedMessage ?? string.Empty).Replace(Environment.NewLine, "\r");
            if (string.IsNullOrWhiteSpace(messageToSend))
            {
                _logger.LogWarning("Send requested but HL7 message is empty.");
                AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Message send cancelled - Nothing to send (HL7 message is empty).");
                return;
            }

            var settings = await _networkSettingsService.LoadAsync();
            string ip = settings.LastIpAddress ?? string.Empty;
            string portStr = settings.LastPort ?? string.Empty;

            if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
            {
                _logger.LogWarning("Invalid port configured: {Port}", portStr);
                AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Message send cancelled - Invalid port configured.");
                return;
            }

            _logger.LogInformation("Sending HL7 message to {Ip}:{Port}.", ip, port);
            _logger.LogDebug("HL7 message length: {Length} characters.", messageToSend.Length);

            var result = await _networkSender.SendAsync(messageToSend, ip, port);

            string messageTypeStr = !string.IsNullOrWhiteSpace(result.MessageCode) 
                ? result.MessageCode 
                : "Unknown";

            if (result.Success)
            {
                string ackStatus = result.AckMessage != null 
                    ? (result.AckMessage.StartsWith("ERR") ? "NACK" : "ACK") 
                    : "No ACK";

                AppendToSendLog(
                    $"[{DateTime.Now:HH:mm:ss}] Message SEND {messageTypeStr} to {ip}:{port}. -> {ackStatus}");
            }
            else
            {
                _logger.LogError("Failed to send HL7 message to {Ip}:{Port}: {Error}", ip, port, result.ErrorMessage);
                AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Message SEND FAILED to {ip}:{port}. Error: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending HL7 message.");
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Message SEND FAILED - Error: {ex.Message}");
        }
    }

    private void AppendToSendLog(string line)
    {
        if (string.IsNullOrWhiteSpace(_sendLog))
        {
            SendLog = line;
        }
        else
        {
            // Add new line at the beginning (most recent first)
            SendLog = line + Environment.NewLine + _sendLog;

            // Limit to MaxLogLines (keep only the most recent lines)
            var lines = SendLog.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > MaxLogLines)
            {
                SendLog = string.Join(Environment.NewLine, lines.Take(MaxLogLines));
            }
        }

        // Auto-expand when log is updated if collapsed
        if (!_isSendLogExpanded)
        {
            IsSendLogExpanded = true;
            // Set height to show up to MaxLogLines
            SendLogHeightRequest = LineHeight * MaxLogLines;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value!;
        OnPropertyChanged(propertyName);
        return true;
    }
}
