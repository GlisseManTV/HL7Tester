using HL7Tester.Core;
using HL7Tester.Core.Batch;
using HL7Tester.Core.Batch.Models;
using HL7Tester.Core.Fhir;
using HL7Tester.Core.Sending;
using Microsoft.Extensions.Logging.Abstractions;

namespace HL7Tester.Tests;

/// <summary>
/// A mock protocol-agnostic message sender for testing BatchSendEngine.
/// Captures the payload of every <see cref="SendRequest"/> so MLLP-mode
/// assertions on HL7 content keep working unchanged.
/// </summary>
internal sealed class MockMessageSender : IMessageSender
{
    public List<string> SentMessages { get; } = new();
    public List<SendRequest> SentRequests { get; } = new();
    public bool ShouldFail { get; set; }
    public string? FailureMessage { get; set; }

    public Task<MessageSendResult> SendAsync(SendRequest request, CancellationToken ct = default)
    {
        lock (SentMessages)
        {
            SentMessages.Add(request.Payload);
            SentRequests.Add(request);
        }

        if (ShouldFail)
        {
            return Task.FromResult(new MessageSendResult
            {
                Success = false,
                Protocol = request.Protocol,
                ErrorMessage = FailureMessage ?? "Mock failure"
            });
        }

        return Task.FromResult(new MessageSendResult
        {
            Success = true,
            Protocol = request.Protocol,
            MsaStatus = "AA",
            HttpStatusCode = 200
        });
    }
}

[TestClass]
public class BatchSendEngineTests
{
    private static BatchSendEngine CreateEngine(MockMessageSender? sender = null)
    {
        var generator = new AdtMessageGenerator();
        sender ??= new MockMessageSender();
        var fhirGenerator = new FhirR4Generator();
        var randomizer = new PatientDataRandomizer(42);
        return new BatchSendEngine(generator, sender, fhirGenerator, randomizer, NullLogger<BatchSendEngine>.Instance);
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
        var sender = new MockMessageSender();
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
        var sender = new MockMessageSender();
        var engine = CreateEngine(sender);
        var workflow = CreateSimpleWorkflow(patientCount: 5);

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        Assert.AreEqual(10, sender.SentMessages.Count); // 5 patients × 2 steps
    }

    [TestMethod]
    public async Task RunAsync_FailureStopsPatientAtFailedStep()
    {
        var sender = new MockMessageSender
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
        var sender = new MockMessageSender();
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
            Assert.IsTrue(ex.Message.Contains("at least 1"));
        }

        Assert.IsTrue(threw, "Expected InvalidOperationException was not thrown");
    }

    [TestMethod]
    public async Task RunAsync_StepsAreSequentialPerPatient()
    {
        var sender = new MockMessageSender();
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
        var sender = new MockMessageSender();
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
        Assert.IsTrue(err.Contains("at least 1"));

        // 1 step → valid (changed from 2 to 1)
        workflow.Steps.Add(new WorkflowStep());
        Assert.IsTrue(workflow.IsValid(out _));

        // 0 patients → invalid
        workflow.PatientCount = 0;
        Assert.IsFalse(workflow.IsValid(out _));
    }

    [TestMethod]
    public void BatchWorkflow_Validation_AllowsUpTo7Steps()
    {
        var workflow = new BatchWorkflow();

        // 7 steps → valid
        for (int i = 0; i < 7; i++)
        {
            workflow.Steps.Add(new WorkflowStep());
        }
        Assert.IsTrue(workflow.IsValid(out _));

        // 8 steps → invalid
        workflow.Steps.Add(new WorkflowStep());
        Assert.IsFalse(workflow.IsValid(out _));
    }

    [TestMethod]
    public async Task RunAsync_SingleStep_Works()
    {
        var sender = new MockMessageSender();
        var engine = CreateEngine(sender);
        var workflow = new BatchWorkflow
        {
            PatientCount = 2,
            GlobalDelayMs = 0,
            Room = "405",
            Bed = "2",
            Unit = "ICU",
            Floor = "4"
        };
        workflow.Steps.Add(new WorkflowStep { MessageType = "ADT A01 - Inpatient or Day Hospital Admission" });

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        Assert.AreEqual(2, sender.SentMessages.Count); // 2 patients × 1 step
    }

    [TestMethod]
    public async Task RunAsync_EventDateTime_ExplicitValue_UsedInMessage()
    {
        var sender = new MockMessageSender();
        var engine = CreateEngine(sender);
        var workflow = CreateSimpleWorkflow(patientCount: 1);
        workflow.Steps[0].EventDateTime = "20261005120000";

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        var firstMsg = sender.SentMessages.First();
        var evnLine = firstMsg.Split('\r').FirstOrDefault(l => l.StartsWith("EVN|"));
        Assert.IsNotNull(evnLine, "EVN segment not found");
        Assert.IsTrue(evnLine.Contains("20261005120000"), $"EVN should contain '20261005120000', got: {evnLine}");
    }

    [TestMethod]
    public async Task RunAsync_EventDateTime_ReferenceToPreviousStep_UsesSameValue()
    {
        var sender = new MockMessageSender();
        var engine = CreateEngine(sender);
        var workflow = CreateSimpleWorkflow(patientCount: 1);
        workflow.Steps[0].EventDateTime = "20261005120000";
        workflow.Steps[1].EventDateTime = "@1"; // Reference to step 1

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        // Both messages should have the same EVN-6.1
        var msg1 = sender.SentMessages[0];
        var msg2 = sender.SentMessages[1];

        var evn1 = msg1.Split('\r').First(l => l.StartsWith("EVN|"));
        var evn2 = msg2.Split('\r').First(l => l.StartsWith("EVN|"));

        Assert.IsTrue(evn1.Contains("20261005120000"), $"Step 1 EVN should contain '20261005120000', got: {evn1}");
        Assert.IsTrue(evn2.Contains("20261005120000"), $"Step 2 EVN should contain '20261005120000' (from @1 reference), got: {evn2}");
    }

    [TestMethod]
    public async Task RunAsync_GlobalSexOverride_ForcesSexForAllPatients()
    {
        var sender = new MockMessageSender();
        var engine = CreateEngine(sender);
        var workflow = CreateSimpleWorkflow(patientCount: 5);
        workflow.Sex = "F";

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        // All sent messages should contain "|F|" in the PID segment (PID-8)
        foreach (var msg in sender.SentMessages)
        {
            var pidLine = msg.Split('\r').FirstOrDefault(l => l.StartsWith("PID|"));
            Assert.IsNotNull(pidLine, "PID segment not found in message");
            var fields = pidLine.Split('|');
            // PID-8 is field index 8 (0-based after splitting)
            Assert.AreEqual("F", fields[8], $"Expected Sex 'F' in PID-8, got '{fields[8]}'");
        }
    }

    [TestMethod]
    public async Task RunAsync_CsvLocation_PicksValidValuePerPatient()
    {
        var sender = new MockMessageSender();
        var engine = CreateEngine(sender);
        var workflow = CreateSimpleWorkflow(patientCount: 10);
        workflow.Room = "301A, 302B, 308A";
        workflow.Bed = "1, 2, 3";
        workflow.Unit = "ICU, NURSING";
        workflow.Floor = "4, 5";

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        var validRooms = new[] { "301A", "302B", "308A" };
        var validUnits = new[] { "ICU", "NURSING" };

        // Each patient's first message (A01) should have a valid location in PV1
        var patientMessages = sender.SentMessages
            .Where(m => m.Contains("ADT^A01"))
            .ToList();

        Assert.AreEqual(10, patientMessages.Count);

        foreach (var msg in patientMessages)
        {
            var pv1Line = msg.Split('\r').FirstOrDefault(l => l.StartsWith("PV1|"));
            Assert.IsNotNull(pv1Line, "PV1 segment not found");

            // The PV1-3 location field must contain one of the valid values
            var pv1Fields = pv1Line.Split('|');
            var location = pv1Fields[3];

            Assert.IsTrue(validRooms.Any(r => location.Contains(r)),
                $"PV1-3 '{location}' does not contain any valid room from '301A, 302B, 308A'");
            Assert.IsTrue(validUnits.Any(u => location.Contains(u)),
                $"PV1-3 '{location}' does not contain any valid unit from 'ICU, NURSING'");
        }
    }

    [TestMethod]
    public async Task RunAsync_SingleLocation_NoCsv_ReturnsAsIs()
    {
        var sender = new MockMessageSender();
        var engine = CreateEngine(sender);
        var workflow = CreateSimpleWorkflow(patientCount: 5);
        workflow.Room = "405";
        workflow.Bed = "2";
        workflow.Unit = "ICU";
        workflow.Floor = "4";

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        var patientMessages = sender.SentMessages
            .Where(m => m.Contains("ADT^A01"))
            .ToList();

        Assert.AreEqual(5, patientMessages.Count);

        foreach (var msg in patientMessages)
        {
            var pv1Line = msg.Split('\r').FirstOrDefault(l => l.StartsWith("PV1|"));
            Assert.IsNotNull(pv1Line, "PV1 segment not found");
            var pv1Fields = pv1Line.Split('|');
            var location = pv1Fields[3];

            Assert.IsTrue(location.Contains("405"), $"PV1-3 '{location}' should contain room '405'");
            Assert.IsTrue(location.Contains("ICU"), $"PV1-3 '{location}' should contain unit 'ICU'");
        }
    }

    [TestMethod]
    public async Task RunAsync_EmptyGlobalSex_UsesRandomSex()
    {
        var sender = new MockMessageSender();
        var engine = CreateEngine(sender);
        var workflow = CreateSimpleWorkflow(patientCount: 10);
        // Sex is empty (default) → random M/F

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667);

        // All PID segments should have a valid sex (M or F)
        foreach (var msg in sender.SentMessages)
        {
            var pidLine = msg.Split('\r').FirstOrDefault(l => l.StartsWith("PID|"));
            Assert.IsNotNull(pidLine, "PID segment not found in message");
            var fields = pidLine.Split('|');
            Assert.IsTrue(fields[8] is "M" or "F", $"Expected 'M' or 'F' in PID-8, got '{fields[8]}'");
        }
    }

    [TestMethod]
    public async Task RunAsync_FhirProtocol_SendsJsonWithMappedEndpointsAndVerbs()
    {
        var sender = new MockMessageSender();
        var engine = CreateEngine(sender);
        var workflow = CreateSimpleWorkflow(patientCount: 1); // A01 then A03
        var profile = new FhirProfile { Name = "Test", BaseUrl = "http://localhost/fhir" };

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667,
            protocol: "FHIR", fhirProfile: profile);

        Assert.AreEqual(2, sender.SentRequests.Count);

        // A01 → transaction Bundle via POST
        var first = sender.SentRequests[0];
        Assert.AreEqual("FHIR", first.Protocol);
        Assert.AreEqual("Bundle", first.ResourceType);
        Assert.AreEqual(FhirHttpMethod.Post, first.HttpMethod);
        Assert.IsTrue(sender.SentMessages[0].TrimStart().StartsWith("{"), "FHIR payload should be JSON");

        // A03 → Encounter via PATCH (discharge)
        var second = sender.SentRequests[1];
        Assert.AreEqual("Encounter", second.ResourceType);
        Assert.AreEqual(FhirHttpMethod.Patch, second.HttpMethod);

        Assert.AreEqual(1, result.SuccessfulPatients);
    }

    [TestMethod]
    public async Task RunAsync_FhirMergeStep_PostsToMergeEndpoint()
    {
        var sender = new MockMessageSender();
        var engine = CreateEngine(sender);
        var workflow = new BatchWorkflow { PatientCount = 1, GlobalDelayMs = 0, Room = "101" };
        workflow.Steps.Add(new WorkflowStep
        {
            MessageType = "ADT A18 - Merge Patient Records",
            NewPatientId = "NEW-1"
        });
        var profile = new FhirProfile { Name = "Test", BaseUrl = "http://localhost/fhir" };

        var result = await engine.RunAsync(workflow, "127.0.0.1", 6667,
            protocol: "FHIR", fhirProfile: profile);

        Assert.AreEqual(1, result.TotalMessagesSent);

        var req = sender.SentRequests[0];
        Assert.AreEqual("Patient", req.ResourceType);
        Assert.AreEqual(FhirHttpMethod.Post, req.HttpMethod);
        Assert.IsNotNull(req.ResourceId);
        Assert.IsTrue(req.ResourceId!.EndsWith("/$merge"), $"Merge endpoint should end with '/$merge', got '{req.ResourceId}'");
    }
}
