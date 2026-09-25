using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HL7Tester.Core;
using HL7Tester.Core.Batch;
using HL7Tester.Core.Batch.Models;
using Microsoft.Extensions.Logging;

namespace HL7Tester.ViewModels;

public sealed class BatchSendViewModel : INotifyPropertyChanged
{
    private readonly BatchSendEngine _engine;
    private readonly INetworkSettingsService _networkSettingsService;
    private readonly ILogger<BatchSendViewModel> _logger;

    private CancellationTokenSource? _cts;

    public event PropertyChangedEventHandler? PropertyChanged;

    // ─── Templates ───────────────────────────────────────────────

    public IReadOnlyList<BatchWorkflowTemplate> Templates { get; } = BatchWorkflowTemplates.All;

    private BatchWorkflowTemplate _selectedTemplate;
    public BatchWorkflowTemplate SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (SetField(ref _selectedTemplate, value))
            {
                LoadTemplateIntoWorkflow();
            }
        }
    }

    // ─── Workflow steps ────────────────────────────────────────────

    public ObservableCollection<WorkflowStep> Steps { get; } = new();

    // ─── Location fields (global) ──────────────────────────────────

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

    private string _unit = string.Empty;
    public string Unit
    {
        get => _unit;
        set => SetField(ref _unit, value);
    }

    private string _floor = string.Empty;
    public string Floor
    {
        get => _floor;
        set => SetField(ref _floor, value);
    }

    // ─── Parameters ────────────────────────────────────────────────

    private int _patientCount = 10;
    public int PatientCount
    {
        get => _patientCount;
        set => SetField(ref _patientCount, value);
    }

    private int _globalDelayMs = 500;
    public int GlobalDelayMs
    {
        get => _globalDelayMs;
        set => SetField(ref _globalDelayMs, value);
    }

    // ─── State ─────────────────────────────────────────────────────

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        private set => SetField(ref _isRunning, value);
    }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    // ─── Results ───────────────────────────────────────────────────

    public ObservableCollection<PatientBatchResult> Results { get; } = new();

    private string _summaryText = string.Empty;
    public string SummaryText
    {
        get => _summaryText;
        set => SetField(ref _summaryText, value);
    }

    // ─── Commands ──────────────────────────────────────────────────

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand AddStepCommand { get; }
    public ICommand RemoveStepCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }

    // ─── Constructor ───────────────────────────────────────────────

    public BatchSendViewModel(
        BatchSendEngine engine,
        INetworkSettingsService networkSettingsService,
        ILogger<BatchSendViewModel> logger)
    {
        _engine = engine;
        _networkSettingsService = networkSettingsService;
        _logger = logger;

        _selectedTemplate = Templates[0];
        LoadTemplateIntoWorkflow();

        StartCommand = new Command(OnStart, () => !IsRunning);
        StopCommand = new Command(OnStop, () => IsRunning);
        AddStepCommand = new Command(OnAddStep);
        RemoveStepCommand = new Command(OnRemoveStep);
        MoveUpCommand = new Command(OnMoveUp);
        MoveDownCommand = new Command(OnMoveDown);
    }

    // ─── Template loading ──────────────────────────────────────────

    private void LoadTemplateIntoWorkflow()
    {
        Steps.Clear();
        var workflow = _selectedTemplate.CreateWorkflow();
        foreach (var step in workflow.Steps)
        {
            Steps.Add(step);
        }
    }

    // ─── Step management ───────────────────────────────────────────

    private void OnAddStep()
    {
        if (Steps.Count >= 6) return;
        Steps.Add(new WorkflowStep { MessageType = "ADT A01 - Inpatient or Day Hospital Admission" });
    }

    private void OnRemoveStep()
    {
        if (Steps.Count <= 2) return;
        // Remove last step by default (UI can bind to a specific index)
        Steps.RemoveAt(Steps.Count - 1);
    }

    public void RemoveStepAt(int index)
    {
        if (index < 0 || index >= Steps.Count || Steps.Count <= 2) return;
        Steps.RemoveAt(index);
    }

    public void MoveStepUp(int index)
    {
        if (index <= 0 || index >= Steps.Count) return;
        var item = Steps[index];
        Steps.RemoveAt(index);
        Steps.Insert(index - 1, item);
    }

    public void MoveStepDown(int index)
    {
        if (index < 0 || index >= Steps.Count - 1) return;
        var item = Steps[index];
        Steps.RemoveAt(index);
        Steps.Insert(index + 1, item);
    }

    private void OnMoveUp() { }
    private void OnMoveDown() { }

    // ─── Start / Stop ──────────────────────────────────────────────

    private async void OnStart()
    {
        if (IsRunning) return;

        // Build the workflow
        var workflow = new BatchWorkflow
        {
            PatientCount = PatientCount,
            GlobalDelayMs = GlobalDelayMs,
            Room = Room,
            Bed = Bed,
            Unit = Unit,
            Floor = Floor
        };
        foreach (var step in Steps)
        {
            workflow.Steps.Add(step.Clone());
        }

        if (!workflow.IsValid(out var error))
        {
            StatusText = $"Error: {error}";
            return;
        }

        // Load network settings
        var settings = await _networkSettingsService.LoadAsync().ConfigureAwait(false);
        var ip = settings.LastIpAddress;
        var port = int.TryParse(settings.LastPort, out var p) ? p : 6667;

        if (string.IsNullOrWhiteSpace(ip))
        {
            StatusText = "Error: No IP address configured. Set it in Network Settings.";
            return;
        }

        // Start
        IsRunning = true;
        Results.Clear();
        SummaryText = string.Empty;
        StatusText = $"Running: {PatientCount} patients × {Steps.Count} steps...";

        _cts = new CancellationTokenSource();

        try
        {
            var result = await _engine.RunAsync(
                workflow, ip, port,
                settings.MessageEncoding,
                _cts.Token).ConfigureAwait(false);

            // Populate results
            foreach (var pr in result.PatientResults)
            {
                Results.Add(pr);
            }

            SummaryText = result.WasCancelled
                ? $"Stopped: {result.SuccessfulPatients}/{result.TotalPatients} patients completed, {result.TotalMessagesSent}/{result.TotalMessagesAttempted} messages sent"
                : $"Done: {result.SuccessfulPatients}/{result.TotalPatients} patients OK, {result.TotalMessagesSent}/{result.TotalMessagesAttempted} messages sent, {result.Elapsed:mm\\:ss}";

            StatusText = SummaryText;
        }
        catch (OperationCanceledException)
        {
            StatusText = "Batch stopped by user.";
            SummaryText = StatusText;
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            _logger.LogError(ex, "Batch send failed");
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnStop()
    {
        _cts?.Cancel();
        StatusText = "Stopping...";
    }

    // ─── INotifyPropertyChanged ────────────────────────────────────

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
