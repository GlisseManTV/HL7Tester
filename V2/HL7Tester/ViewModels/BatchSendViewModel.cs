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
    private readonly IBatchTemplateService _templateService;
    private readonly ILogger<BatchSendViewModel> _logger;

    private CancellationTokenSource? _cts;

    public event PropertyChangedEventHandler? PropertyChanged;

    // ─── Templates ───────────────────────────────────────────────

    public ObservableCollection<BatchWorkflowTemplate> Templates { get; } = new();

    private BatchWorkflowTemplate? _selectedTemplate;

    /// <summary>
    /// True if the currently selected template is a custom (non-built-in) template that can be deleted.
    /// </summary>
    public bool CanDeleteTemplate => _selectedTemplate is not null && !_selectedTemplate.IsBuiltIn;

    // ─── Workflow steps ────────────────────────────────────────────

    public ObservableCollection<WorkflowStep> Steps { get; } = new();

    /// <summary>
    /// Available HL7 message types for adding new steps.
    /// </summary>
    public IReadOnlyList<string> AvailableStepTypes { get; } = WorkflowStep.AvailableMessageTypes;

    private string _selectedNewStepType;
    /// <summary>
    /// The message type selected in the "Add step" picker.
    /// </summary>
    public string SelectedNewStepType
    {
        get => _selectedNewStepType;
        set => SetField(ref _selectedNewStepType, value);
    }

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
    public ICommand SaveTemplateCommand { get; }
    public ICommand DeleteTemplateCommand { get; }
    public ICommand SelectTemplateCommand { get; }

    // ─── Constructor ───────────────────────────────────────────────

    public BatchSendViewModel(
        BatchSendEngine engine,
        INetworkSettingsService networkSettingsService,
        IBatchTemplateService templateService,
        ILogger<BatchSendViewModel> logger)
    {
        _engine = engine;
        _networkSettingsService = networkSettingsService;
        _templateService = templateService;
        _logger = logger;

        _selectedNewStepType = AvailableStepTypes[0];

        StartCommand = new Command(OnStart, () => !IsRunning);
        StopCommand = new Command(OnStop, () => IsRunning);
        AddStepCommand = new Command(OnAddStep);
        SaveTemplateCommand = new Command(OnSaveTemplate);
        DeleteTemplateCommand = new Command(OnDeleteTemplate);
        SelectTemplateCommand = new Command<BatchWorkflowTemplate>(OnSelectTemplate);
    }

    /// <summary>
    /// Loads all templates (built-in + custom) into the Templates collection.
    /// </summary>
    public async Task LoadTemplatesAsync()
    {
        Templates.Clear();

        // Add built-in templates
        foreach (var template in BatchWorkflowTemplates.All)
        {
            Templates.Add(template);
        }

        // Add custom templates from file
        try
        {
            var customTemplates = await _templateService.LoadAsync().ConfigureAwait(false);
            foreach (var template in customTemplates)
            {
                Templates.Add(template);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load custom batch templates");
        }

        // Select first template
        if (Templates.Count > 0)
        {
            OnSelectTemplate(Templates[0]);
        }
    }

    // ─── Template loading ──────────────────────────────────────────

    private void OnSelectTemplate(BatchWorkflowTemplate template)
    {
        _selectedTemplate = template;
        OnPropertyChanged(nameof(CanDeleteTemplate));
        LoadTemplateIntoWorkflow();
    }

    private void LoadTemplateIntoWorkflow()
    {
        Steps.Clear();
        if (_selectedTemplate is null) return;
        var workflow = _selectedTemplate.CreateWorkflow();
        foreach (var step in workflow.Steps)
        {
            WireStepCommands(step);
            Steps.Add(step);
        }
    }

    // ─── Step management ───────────────────────────────────────────

    private void OnAddStep()
    {
        if (Steps.Count >= 6) return;
        var step = new WorkflowStep { MessageType = SelectedNewStepType };
        WireStepCommands(step);
        Steps.Add(step);
    }

    /// <summary>
    /// Wires the MoveUp/MoveDown/Remove commands directly on a WorkflowStep.
    /// </summary>
    private void WireStepCommands(WorkflowStep step)
    {
        step.MoveUpCommand = new Command(() =>
        {
            int index = Steps.IndexOf(step);
            if (index <= 0) return;
            Steps.RemoveAt(index);
            Steps.Insert(index - 1, step);
        });

        step.MoveDownCommand = new Command(() =>
        {
            int index = Steps.IndexOf(step);
            if (index < 0 || index >= Steps.Count - 1) return;
            Steps.RemoveAt(index);
            Steps.Insert(index + 1, step);
        });

        step.RemoveCommand = new Command(() =>
        {
            int index = Steps.IndexOf(step);
            if (index < 0 || Steps.Count <= 2) return;
            Steps.RemoveAt(index);
        });
    }

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

    // ─── Template management ───────────────────────────────────────

    private async void OnSaveTemplate()
    {
        if (Steps.Count < 2)
        {
            StatusText = "Cannot save: workflow must have at least 2 steps.";
            return;
        }

        var name = BatchWorkflowTemplate.GenerateNameFromSteps(Steps.ToList());

        // Prevent overwriting a built-in template
        var existing = Templates.FirstOrDefault(t => t.Name == name);
        if (existing is not null && existing.IsBuiltIn)
        {
            StatusText = $"Cannot save: \"{name}\" is a built-in template.";
            return;
        }

        var template = new BatchWorkflowTemplate
        {
            Name = name,
            Description = "Custom template",
            IsBuiltIn = false,
            Steps = Steps.Select(s => s.Clone()).ToList()
        };

        try
        {
            await _templateService.SaveAsync(template).ConfigureAwait(false);

            // Update the collection in-place (no Clear)
            if (existing is not null)
            {
                // Overwrite existing custom template
                int index = Templates.IndexOf(existing);
                Templates[index] = template;
            }
            else
            {
                Templates.Add(template);
            }

            // Select the saved template (loads workflow into steps)
            OnSelectTemplate(template);

            StatusText = $"Template \"{name}\" saved.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error saving template: {ex.Message}";
            _logger.LogError(ex, "Failed to save batch template");
        }
    }

    private async void OnDeleteTemplate()
    {
        if (_selectedTemplate is null || _selectedTemplate.IsBuiltIn) return;

        var name = _selectedTemplate.Name;

        try
        {
            await _templateService.DeleteAsync(name).ConfigureAwait(false);
            // Reload templates — LoadTemplatesAsync selects the first template
            await LoadTemplatesAsync().ConfigureAwait(false);
            StatusText = $"Template \"{name}\" deleted.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error deleting template: {ex.Message}";
            _logger.LogError(ex, "Failed to delete batch template");
        }
    }

    // ─── INotifyPropertyChanged ────────────────────────────────────

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
