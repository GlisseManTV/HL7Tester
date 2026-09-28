using HL7Tester.Core;
using HL7Tester.Core.Batch;
using HL7Tester.Core.Batch.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace HL7Tester.Tests;

/// <summary>
/// A mock HL7 network sender for testing BatchSendEngine.
/// </summary>
internal sealed class MockHl7NetworkSender : IHL7NetworkSender
{
    public List<string> SentMessages { get; } = new();
    public bool ShouldFail { get; set; }
    public string? FailureMessage { get; set; }

    public Task<SendResult> SendAsync(string hl7Message, string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        return SendAsync(hl7Message, ipAddress, port, cancellationToken, null);
    }

    public Task<SendResult> SendAsync(string hl7Message, string ipAddress, int port, CancellationToken cancellationToken = default, string? encodingName = null)
    {
        lock (SentMessages)
        {
            SentMessages.Add(hl7Message);
        }

        if (ShouldFail)
        {
            return Task.FromResult(new SendResult
            {
                Success = false,
                ErrorMessage = FailureMessage ?? "Mock failure"
            });
        }

        return Task.FromResult(new SendResult
        {
            Success = true,
            MessageCode = "ACK"
        });
    }
}

[TestClass]
public class BatchSendEngineTests
{
    private static BatchSendEngine CreateEngine(MockHl7NetworkSender? sender = null)
    {
        var generator = new AdtMessageGenerator();
        sender ??= new MockHl7NetworkSender();
        var randomizer = new PatientDataRandomizer(42);
        return new BatchSendEngine(generator, sender, randomizer, NullLogger<BatchSendEngine>.Instance);
    }

    private static BatchWorkflow CreateSimpleWorkflow(int patientCount = 2, int delayMs = 0)
    {
        var workflow = new BatchWorkflow
        {
            PatientCount = patientCount,
            GlobalDelayMs = delayMs,
            Room = "405",
            Bed = "2",
            Unit = "ICU",
            Floor = "4"
        };
        workflow.Steps.Add(new WorkflowStep { MessageType = "ADT A01 - Inpatient or Day Hospital Admission" });
        workflow.Steps.Add(new WorkflowStep { MessageType = "ADT A03 - Discharge" });
        return workflow;
    }

    [TestMethod]
    public async Task RunAsync_AllPatientsCompleteSuccessfully()
    {
        var sender = new MockHl7NetworkSender();
        var engine = CreateEngine(sender);
        var workflow = CreateSimpleWorkflow(patientCount: 3);

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        Assert.AreEqual(3, result.TotalPatients);
        Assert.AreEqual(3, result.SuccessfulPatients);
        Assert.AreEqual(0, result.FailedPatients);
        Assert.AreEqual(6, result.TotalMessagesSent); // 3 patients × 2 steps
        Assert.AreEqual(6, sender.SentMessages.Count);
    }

    [TestMethod]
    public async Task RunAsync_SendsCorrectNumberOfMessagesPerPatient()
    {
        var sender = new MockHl7NetworkSender();
        var engine = CreateEngine(sender);
        var workflow = CreateSimpleWorkflow(patientCount: 5);

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        Assert.AreEqual(10, sender.SentMessages.Count); // 5 patients × 2 steps
    }

    [TestMethod]
    public async Task RunAsync_FailureStopsPatientAtFailedStep()
    {
        var sender = new MockHl7NetworkSender
        {
            ShouldFail = true,
            FailureMessage = "Connection refused"
        };
        var engine = CreateEngine(sender);
        var workflow = CreateSimpleWorkflow(patientCount: 2);

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        Assert.AreEqual(2, result.TotalPatients);
        Assert.AreEqual(0, result.SuccessfulPatients);
        Assert.AreEqual(2, result.FailedPatients);

        foreach (var pr in result.PatientResults)
        {
            Assert.IsFalse(pr.Success);
            Assert.AreEqual(1, pr.FailedStepIndex);
            Assert.AreEqual("Connection refused", pr.Error);
        }
    }

    [TestMethod]
    public async Task RunAsync_CancellationStopsAllPatients()
    {
        var sender = new MockHl7NetworkSender();
        var engine = CreateEngine(sender);
        var workflow = CreateSimpleWorkflow(patientCount: 2, delayMs: 100);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel quickly

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667, cancellationToken: cts.Token);

        Assert.IsTrue(result.WasCancelled);
    }

    [TestMethod]
    public async Task RunAsync_InvalidWorkflow_ThrowsException()
    {
        var engine = CreateEngine();
        var workflow = new BatchWorkflow { PatientCount = 1 };
        // No steps added → invalid

        bool threw = false;
        try
        {
            await engine.RunAsync(workflow, "127.0.0.1", 6667);
        }
        catch (InvalidOperationException ex)
        {
            threw = true;
            Assert.IsTrue(ex.Message.Contains("at least 2"));
        }

        Assert.IsTrue(threw, "Expected InvalidOperationException was not thrown");
    }

    [TestMethod]
    public async Task RunAsync_StepsAreSequentialPerPatient()
    {
        var sender = new MockHl7NetworkSender();
        var engine = CreateEngine(sender);

        var workflow = new BatchWorkflow
        {
            PatientCount = 1,
            GlobalDelayMs = 0,
            Room = "101"
        };
        workflow.Steps.Add(new WorkflowStep { MessageType = "ADT A01 - Inpatient or Day Hospital Admission" });
        workflow.Steps.Add(new WorkflowStep { MessageType = "ADT A02 - Patient Movement" });
        workflow.Steps.Add(new WorkflowStep { MessageType = "ADT A03 - Discharge" });

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        Assert.AreEqual(3, sender.SentMessages.Count);

        // Verify order: A01 → A02 → A03 (check trigger event in MSH segment)
        Assert.IsTrue(sender.SentMessages[0].Contains("A01"));
        Assert.IsTrue(sender.SentMessages[1].Contains("A02"));
        Assert.IsTrue(sender.SentMessages[2].Contains("A03"));
    }

    [TestMethod]
    public async Task RunAsync_PatientContextIsConsistentAcrossSteps()
    {
        var sender = new MockHl7NetworkSender();
        var engine = CreateEngine(sender);

        var workflow = new BatchWorkflow
        {
            PatientCount = 1,
            GlobalDelayMs = 0,
            Room = "505"
        };
        workflow.Steps.Add(new WorkflowStep { MessageType = "ADT A01 - Inpatient or Day Hospital Admission" });
        workflow.Steps.Add(new WorkflowStep { MessageType = "ADT A03 - Discharge" });

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        var patientId = result.PatientResults[0].Patient.PatientId;

        // Both messages should contain the same patient ID
        Assert.IsTrue(sender.SentMessages[0].Contains(patientId));
        Assert.IsTrue(sender.SentMessages[1].Contains(patientId));
    }

    [TestMethod]
    public void BatchWorkflow_Validation_Works()
    {
        var workflow = new BatchWorkflow { PatientCount = 5 };

        // No steps → invalid
        Assert.IsFalse(workflow.IsValid(out var err));
        Assert.IsTrue(err.Contains("at least 2"));

        // 1 step → invalid
        workflow.Steps.Add(new WorkflowStep());
        Assert.IsFalse(workflow.IsValid(out _));

        // 2 steps → valid
        workflow.Steps.Add(new WorkflowStep());
        Assert.IsTrue(workflow.IsValid(out _));

        // 0 patients → invalid
        workflow.PatientCount = 0;
        Assert.IsFalse(workflow.IsValid(out _));
    }
}
