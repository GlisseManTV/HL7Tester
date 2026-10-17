namespace HL7Tester.Core.Fhir;

/// <summary>
/// Configuration for the $merge operation payload (decision D10).
/// Parameter names are not standardized across FHIR servers, so they can be
/// customized per profile. The generator uses these names when building the
/// <c>Parameters</c> resource for ADT A18/A40.
/// </summary>
public sealed class FhirMergeConfig
{
    /// <summary>Name of the parameter carrying the source (merged/retired) patient id.</summary>
    public string SourceParamName { get; set; } = "sourcePatientId";

    /// <summary>Name of the parameter carrying the target (surviving) patient id.</summary>
    public string TargetParamName { get; set; } = "targetPatientId";

    /// <summary>
    /// Optional "operation" parameter name. When set, the generator adds an
    /// extra parameter with this name and value "merge" (some servers expect it).
    /// </summary>
    public string? OperationParamName { get; set; }

    /// <summary>
    /// Optional "reference" parameter name. When set, the generator adds an
    /// extra parameter with this name carrying the target patient reference
    /// (e.g. "Patient/123") for servers following the $merge [reference] variant.
    /// </summary>
    public string? ReferenceParamName { get; set; }
}
