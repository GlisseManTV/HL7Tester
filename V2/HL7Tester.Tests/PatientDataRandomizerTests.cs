using HL7Tester.Core.Batch;
using HL7Tester.Core.Batch.Models;

namespace HL7Tester.Tests;

[TestClass]
public class PatientDataRandomizerTests
{
    [TestMethod]
    public void GeneratePatient_ReturnsNonNullFields()
    {
        var randomizer = new PatientDataRandomizer(42);
        var patient = randomizer.GeneratePatient();

        Assert.IsFalse(string.IsNullOrWhiteSpace(patient.PatientId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(patient.FamilyName));
        Assert.IsFalse(string.IsNullOrWhiteSpace(patient.GivenName));
        Assert.IsFalse(string.IsNullOrWhiteSpace(patient.AdmissionNumber));
        Assert.IsFalse(string.IsNullOrWhiteSpace(patient.BirthDate));
        Assert.IsTrue(patient.Sex is "M" or "F");
    }

    [TestMethod]
    public void GeneratePatient_PatientIdIsNumeric()
    {
        var randomizer = new PatientDataRandomizer(42);
        var patient = randomizer.GeneratePatient();

        Assert.IsTrue(int.TryParse(patient.PatientId, out _));
    }

    [TestMethod]
    public void GeneratePatient_AdmissionNumberIsNumeric()
    {
        var randomizer = new PatientDataRandomizer(42);
        var patient = randomizer.GeneratePatient();

        Assert.IsTrue(int.TryParse(patient.AdmissionNumber, out _));
    }

    [TestMethod]
    public void GeneratePatient_BirthDateIsValidFormat()
    {
        var randomizer = new PatientDataRandomizer(42);
        var patient = randomizer.GeneratePatient();

        Assert.AreEqual(8, patient.BirthDate.Length);
        Assert.IsTrue(int.TryParse(patient.BirthDate, out var date));
        Assert.IsTrue(date >= 19400101 && date <= 20101228);
    }

    [TestMethod]
    public void GeneratePatient_SexIsMOrF()
    {
        var randomizer = new PatientDataRandomizer(1);
        for (int i = 0; i < 100; i++)
        {
            var patient = randomizer.GeneratePatient();
            Assert.IsTrue(patient.Sex is "M" or "F");
        }
    }

    [TestMethod]
    public void GeneratePatients_ReturnsCorrectCount()
    {
        var randomizer = new PatientDataRandomizer(42);
        var patients = randomizer.GeneratePatients(15);

        Assert.AreEqual(15, patients.Count);
    }

    [TestMethod]
    public void GeneratePatients_AllHaveUniquePatientIds()
    {
        var randomizer = new PatientDataRandomizer(42);
        var patients = randomizer.GeneratePatients(50);

        var uniqueIds = patients.Select(p => p.PatientId).Distinct().Count();
        // With 50 patients and 7-digit IDs, collisions are extremely unlikely
        Assert.AreEqual(50, uniqueIds);
    }

    [TestMethod]
    public void GeneratePatient_WithSameSeed_ProducesSameFirstPatient()
    {
        var r1 = new PatientDataRandomizer(123);
        var r2 = new PatientDataRandomizer(123);

        var p1 = r1.GeneratePatient();
        var p2 = r2.GeneratePatient();

        Assert.AreEqual(p1.PatientId, p2.PatientId);
        Assert.AreEqual(p1.FamilyName, p2.FamilyName);
        Assert.AreEqual(p1.GivenName, p2.GivenName);
    }
}
