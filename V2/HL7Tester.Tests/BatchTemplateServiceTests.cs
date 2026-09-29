using HL7Tester.Core.Batch;
using HL7Tester.Core.Batch.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HL7Tester.Tests;

[TestClass]
public class BatchTemplateServiceTests
{
    private string _tempDir = null!;
    private string _filePath = null!;
    private FileBatchTemplateService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"hl7test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "batchtemplates.json");
        _service = new FileBatchTemplateService(_filePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [TestMethod]
    public async Task LoadAsync_FileNotExists_ReturnsEmptyList()
    {
        var result = await _service.LoadAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task SaveAsync_CreatesFile_WithTemplate()
    {
        var template = CreateTestTemplate("A01 → A02 → A03");

        await _service.SaveAsync(template);

        Assert.IsTrue(File.Exists(_filePath));
        var loaded = await _service.LoadAsync();
        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual("A01 → A02 → A03", loaded[0].Name);
        Assert.AreEqual(3, loaded[0].Steps.Count);
    }

    [TestMethod]
    public async Task SaveAsync_SameName_OverwritesExisting()
    {
        var template1 = CreateTestTemplate("A01 → A02 → A03");
        template1.Steps.Add(new WorkflowStep { MessageType = "ADT A08 - Update Patient Stay" });

        var template2 = CreateTestTemplate("A01 → A02 → A03");
        // template2 has 3 steps (default), template1 has 4

        await _service.SaveAsync(template1);
        await _service.SaveAsync(template2);

        var loaded = await _service.LoadAsync();
        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual(3, loaded[0].Steps.Count); // overwritten by template2
    }

    [TestMethod]
    public async Task SaveAsync_DifferentName_AddsNewTemplate()
    {
        var template1 = CreateTestTemplate("A01 → A02 → A03");
        var template2 = CreateTestTemplate("A01 → A31 → A03");

        await _service.SaveAsync(template1);
        await _service.SaveAsync(template2);

        var loaded = await _service.LoadAsync();
        Assert.AreEqual(2, loaded.Count);
    }

    [TestMethod]
    public async Task DeleteAsync_RemovesTemplate()
    {
        var template = CreateTestTemplate("A01 → A02 → A03");
        await _service.SaveAsync(template);

        await _service.DeleteAsync("A01 → A02 → A03");

        var loaded = await _service.LoadAsync();
        Assert.AreEqual(0, loaded.Count);
    }

    [TestMethod]
    public async Task DeleteAsync_NonExistent_DoesNotThrow()
    {
        // Should not throw when deleting a non-existent template
        await _service.DeleteAsync("Nonexistent Template");

        var loaded = await _service.LoadAsync();
        Assert.AreEqual(0, loaded.Count);
    }

    [TestMethod]
    public async Task LoadAsync_CorruptedFile_ReturnsEmptyList()
    {
        await File.WriteAllTextAsync(_filePath, "{ invalid json }");

        var result = await _service.LoadAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task SaveAsync_PreservesStepOverrides()
    {
        var template = new BatchWorkflowTemplate
        {
            Name = "A01 → A02 → A03",
            IsBuiltIn = false,
            Steps = new List<WorkflowStep>
            {
                new() { MessageType = "ADT A01 - Inpatient or Day Hospital Admission", Room = "101", Bed = "A" },
                new() { MessageType = "ADT A02 - Patient Movement", Unit = "ICU" },
                new() { MessageType = "ADT A03 - Discharge" }
            }
        };

        await _service.SaveAsync(template);
        var loaded = await _service.LoadAsync();

        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual("101", loaded[0].Steps[0].Room);
        Assert.AreEqual("A", loaded[0].Steps[0].Bed);
        Assert.AreEqual("ICU", loaded[0].Steps[1].Unit);
        Assert.IsNull(loaded[0].Steps[2].Room);
    }

    [TestMethod]
    public void GenerateNameFromSteps_ProducesCorrectName()
    {
        var steps = new List<WorkflowStep>
        {
            new() { MessageType = "ADT A01 - Inpatient or Day Hospital Admission" },
            new() { MessageType = "ADT A02 - Patient Movement" },
            new() { MessageType = "ADT A08 - Update Patient Stay" },
            new() { MessageType = "ADT A03 - Discharge" }
        };

        var name = BatchWorkflowTemplate.GenerateNameFromSteps(steps);

        Assert.AreEqual("A01 → A02 → A08 → A03", name);
    }

    [TestMethod]
    public void GenerateNameFromSteps_SingleStep()
    {
        var steps = new List<WorkflowStep>
        {
            new() { MessageType = "ADT A31 - Update Patient" }
        };

        var name = BatchWorkflowTemplate.GenerateNameFromSteps(steps);

        Assert.AreEqual("A31", name);
    }

    private static BatchWorkflowTemplate CreateTestTemplate(string name)
    {
        return new BatchWorkflowTemplate
        {
            Name = name,
            Description = "Test template",
            IsBuiltIn = false,
            Steps = new List<WorkflowStep>
            {
                new() { MessageType = "ADT A01 - Inpatient or Day Hospital Admission" },
                new() { MessageType = "ADT A02 - Patient Movement" },
                new() { MessageType = "ADT A03 - Discharge" }
            }
        };
    }
}
