using HL7Tester.Core.Batch.Models;

namespace HL7Tester.Core.Batch;

/// <summary>
/// Generates randomized patient data (name, given name, patient ID, admission number, birth date, sex).
/// Location fields (room, bed, unit, floor) are NOT randomized — they are provided by the user.
/// </summary>
public sealed class PatientDataRandomizer
{
    private static readonly string[] FamilyNames =
    [
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
        "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson",
        "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson",
        "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson",
        "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen",
        "Hill", "Flores", "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera",
        "Campbell", "Mitchell", "Carter", "Roberts", "Chen", "Kumar", "Muller", "Schmidt"
    ];

    private static readonly string[] GivenNames =
    [
        "James", "Mary", "Robert", "Patricia", "John", "Jennifer", "Michael", "Linda",
        "David", "Elizabeth", "William", "Barbara", "Richard", "Susan", "Joseph", "Jessica",
        "Thomas", "Sarah", "Charles", "Karen", "Christopher", "Nancy", "Daniel", "Lisa",
        "Matthew", "Betty", "Anthony", "Margaret", "Mark", "Sandra", "Donald", "Ashley",
        "Steven", "Kimberly", "Paul", "Emily", "Andrew", "Donna", "Joshua", "Michelle",
        "Kenneth", "Carol", "Kevin", "Amanda", "Brian", "Dorothy", "George", "Melissa",
        "Edward", "Deborah", "Raymond", "Stephanie", "Ronald", "Rebecca", "Timothy", "Sharon"
    ];

    private readonly Random _random;

    public PatientDataRandomizer()
    {
        // Use a time-seeded Random for variety across runs
        _random = new Random();
    }

    /// <summary>
    /// Creates a Random instance with a specific seed (for testing).
    /// </summary>
    public PatientDataRandomizer(int seed)
    {
        _random = new Random(seed);
    }

    /// <summary>
    /// Generates a new randomized patient context.
    /// </summary>
    public PatientContext GeneratePatient()
    {
        var sex = _random.Next(2) == 0 ? "M" : "F";

        // Patient ID: 5-8 digit number
        var patientId = _random.Next(10000, 10000000).ToString();

        // Admission number: 5-7 digit number
        var admissionNumber = _random.Next(10000, 1000000).ToString();

        // Birth date: between 1940 and 2010
        var birthYear = _random.Next(1940, 2011);
        var birthMonth = _random.Next(1, 13);
        var birthDay = _random.Next(1, 29);
        var birthDate = $"{birthYear}{birthMonth:D2}{birthDay:D2}";

        return new PatientContext
        {
            PatientId = patientId,
            FamilyName = FamilyNames[_random.Next(FamilyNames.Length)],
            GivenName = GivenNames[_random.Next(GivenNames.Length)],
            AdmissionNumber = admissionNumber,
            Sex = sex,
            BirthDate = birthDate
        };
    }

    /// <summary>
    /// Generates multiple patient contexts.
    /// </summary>
    public List<PatientContext> GeneratePatients(int count)
    {
        var patients = new List<PatientContext>(count);
        for (int i = 0; i < count; i++)
        {
            patients.Add(GeneratePatient());
        }
        return patients;
    }
}
