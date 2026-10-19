using HL7Tester.Core.Batch.Models;

namespace HL7Tester.Core.Batch;

/// <summary>
/// A predefined batch workflow template.
/// </summary>
public sealed class BatchWorkflowTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<WorkflowStep> Steps { get; set; } = new();

    /// <summary>
    /// True if this is a built-in template that cannot be deleted.
    /// </summary>
    public bool IsBuiltIn { get; set; } = false;

    public override string ToString() => Name;

    /// <summary>
    /// Creates a new BatchWorkflow from this template.
    /// </summary>
    public BatchWorkflow CreateWorkflow()
    {
        var workflow = new BatchWorkflow();
        foreach (var step in Steps)
        {
            workflow.Steps.Add(step.Clone());
        }
        return workflow;
    }

    /// <summary>
    /// Generates a display name from a list of steps, e.g. "A01 → A02 → A08 → A03".
    /// </summary>
    public static string GenerateNameFromSteps(List<WorkflowStep> steps)
    {
        var codes = steps.Select(s =>
        {
            // Extract the code part: "ADT A01 - Inpatient..." → "A01"
            var parts = s.MessageType.Split(' ');
            return parts.Length >= 2 ? parts[1] : s.MessageType;
        });
        return string.Join(" → ", codes);
    }
}

/// <summary>
/// Static collection of predefined batch workflow templates.
/// </summary>
public static class BatchWorkflowTemplates
{
    public static IReadOnlyList<BatchWorkflowTemplate> All { get; } = BuildTemplates();

    private static List<BatchWorkflowTemplate> BuildTemplates()
    {
        return new List<BatchWorkflowTemplate>
        {
            new()
            {
                Name = "A01 → A02 → A08 → A03",
                Description = "Full admission cycle",
                IsBuiltIn = true,
                Steps = new List<WorkflowStep>
                {
                    new() { MessageType = "ADT A01 - Inpatient or Day Hospital Admission" },
                    new() { MessageType = "ADT A02 - Patient Movement" },
                    new() { MessageType = "ADT A08 - Update Patient Stay" },
                    new() { MessageType = "ADT A03 - Discharge" }
                }
            },
            new()
            {
                Name = "A01 → A02 → A12 → A02 → A03",
                Description = "Movement with cancellation",
                IsBuiltIn = true,
                Steps = new List<WorkflowStep>
                {
                    new() { MessageType = "ADT A01 - Inpatient or Day Hospital Admission" },
                    new() { MessageType = "ADT A02 - Patient Movement" },
                    new() { MessageType = "ADT A12 - Movement Cancellation" },
                    new() { MessageType = "ADT A02 - Patient Movement" },
                    new() { MessageType = "ADT A03 - Discharge" }
                }
            },
            new()
            {
                Name = "A01 → A02 → A12 → A02 → A31 → A03",
                Description = "Full cycle with update",
                IsBuiltIn = true,
                Steps = new List<WorkflowStep>
                {
                    new() { MessageType = "ADT A01 - Inpatient or Day Hospital Admission" },
                    new() { MessageType = "ADT A02 - Patient Movement" },
                    new() { MessageType = "ADT A12 - Movement Cancellation" },
                    new() { MessageType = "ADT A02 - Patient Movement" },
                    new() { MessageType = "ADT A31 - Update Patient" },
                    new() { MessageType = "ADT A03 - Discharge" }
                }
            },
            new()
            {
                Name = "A01 → A31 → A03",
                Description = "Admission with patient update",
                IsBuiltIn = true,
                Steps = new List<WorkflowStep>
                {
                    new() { MessageType = "ADT A01 - Inpatient or Day Hospital Admission" },
                    new() { MessageType = "ADT A31 - Update Patient" },
                    new() { MessageType = "ADT A03 - Discharge" }
                }
            }
        };
    }
}
