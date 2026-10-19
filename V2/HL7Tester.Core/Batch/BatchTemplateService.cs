using System.Text.Json;
using HL7Tester.Core.Batch.Models;

namespace HL7Tester.Core.Batch;

/// <summary>
/// Service for persisting custom batch workflow templates to a JSON file.
/// </summary>
public interface IBatchTemplateService
{
    Task<List<BatchWorkflowTemplate>> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(BatchWorkflowTemplate template, CancellationToken cancellationToken = default);
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// File-based implementation of <see cref="IBatchTemplateService"/>.
/// Persists templates in a JSON file (one array of templates).
/// </summary>
public sealed class FileBatchTemplateService : IBatchTemplateService
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public FileBatchTemplateService(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public async Task<List<BatchWorkflowTemplate>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new List<BatchWorkflowTemplate>();
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var templates = await JsonSerializer.DeserializeAsync<List<BatchWorkflowTemplate>>(stream, _jsonOptions, cancellationToken)
                               .ConfigureAwait(false);
            return templates ?? new List<BatchWorkflowTemplate>();
        }
        catch (JsonException)
        {
            // Corrupted file — return empty list
            return new List<BatchWorkflowTemplate>();
        }
    }

    public async Task SaveAsync(BatchWorkflowTemplate template, CancellationToken cancellationToken = default)
    {
        if (template is null) throw new ArgumentNullException(nameof(template));
        if (string.IsNullOrWhiteSpace(template.Name)) throw new ArgumentException("Template name cannot be empty.", nameof(template));

        var templates = await LoadAsync(cancellationToken).ConfigureAwait(false);

        // Replace existing template with same name (overwrite)
        var existingIndex = templates.FindIndex(t => t.Name == template.Name);
        if (existingIndex >= 0)
        {
            templates[existingIndex] = template;
        }
        else
        {
            templates.Add(template);
        }

        await WriteAsync(templates, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Template name cannot be empty.", nameof(name));

        var templates = await LoadAsync(cancellationToken).ConfigureAwait(false);

        var toRemove = templates.Find(t => t.Name == name);
        if (toRemove is not null)
        {
            templates.Remove(toRemove);
            await WriteAsync(templates, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WriteAsync(List<BatchWorkflowTemplate> templates, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(templates, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
    }
}
