using System.Net.Http.Headers;
using System.Text;
using HL7Tester.Core.Fhir.FhirAuth;
using HL7Tester.Core.Security;
using HL7Tester.Core.Sending;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace HL7Tester.Core.Fhir;

/// <summary>
/// Result of a FHIR REST call performed by <see cref="FhirRestClient"/>.
/// </summary>
public sealed class FhirSendResult
{
    public bool Success { get; init; }
    public int? HttpStatusCode { get; init; }
    public string? ResponseBody { get; init; }
    public string? OperationOutcomeJson { get; init; }
    public string? ErrorDescription { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}

/// <summary>
/// Thin HttpClient wrapper around the FHIR RESTful API. Performs GET / POST / PUT /
/// PATCH / DELETE against a server configured by a <see cref="FhirProfile"/>,
/// applies authentication via <see cref="FhirAuthDelegatingHandler"/>, and parses
/// responses using the Firely SDK serializers.
/// </summary>
public sealed class FhirRestClient
{
    private const string FhirJsonMediaType = "application/fhir+json";

    private readonly ISecretStore _secretStore;

    public FhirRestClient(ISecretStore secretStore)
    {
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    }

    /// <summary>
    /// Executes a FHIR REST interaction described by <paramref name="request"/>.
    /// </summary>
    public async Task<FhirSendResult> SendAsync(SendRequest request, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        var profile = request.Profile;
        if (profile is null)
            return new FhirSendResult { Success = false, ErrorMessage = "No FHIR profile configured." };
        if (string.IsNullOrWhiteSpace(profile.BaseUrl))
            return new FhirSendResult { Success = false, ErrorMessage = "FHIR profile BaseUrl is empty." };

        try
        {
            using var httpClient = BuildHttpClient(profile);
            using var message = BuildRequest(request, profile);

            using var response = await httpClient.SendAsync(message, ct).ConfigureAwait(false);
            var status = (int)response.StatusCode;
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            string? operationOutcomeJson = null;
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    var deserializer = new FhirJsonDeserializer();
                    var resource = deserializer.Deserialize<Resource>(body);
                    if (resource is OperationOutcome)
                        operationOutcomeJson = body;
                }
                catch
                {
                    // Response is not a parsable FHIR resource (HTML error page, etc.).
                }
            }

            return new FhirSendResult
            {
                Success = response.IsSuccessStatusCode,
                HttpStatusCode = status,
                ResponseBody = body,
                OperationOutcomeJson = operationOutcomeJson,
                ErrorDescription = response.IsSuccessStatusCode
                    ? null
                    : $"{(int)response.StatusCode} {response.ReasonPhrase}".Trim(),
                ErrorMessage = response.IsSuccessStatusCode ? string.Empty : $"{(int)response.StatusCode} {response.ReasonPhrase}".Trim()
            };
        }
        catch (Exception ex)
        {
            return new FhirSendResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorDescription = ex.Message
            };
        }
    }

    /// <summary>
    /// Convenience helper used by the acceptance test: issues GET [base]/metadata and
    /// deserializes the result into a Firely <see cref="CapabilityStatement"/>.
    /// </summary>
    public async Task<CapabilityStatement?> GetCapabilityStatementAsync(FhirProfile profile, CancellationToken ct = default)
    {
        var request = new SendRequest
        {
            Protocol = "FHIR",
            Profile = profile,
            HttpMethod = FhirHttpMethod.Get,
            ResourceType = "metadata"
        };
        var result = await SendAsync(request, ct).ConfigureAwait(false);
        if (!result.Success || string.IsNullOrWhiteSpace(result.ResponseBody))
            return null;

        try
        {
            var deserializer = new FhirJsonDeserializer();
            return deserializer.Deserialize<CapabilityStatement>(result.ResponseBody);
        }
        catch
        {
            return null;
        }
    }

    private HttpClient BuildHttpClient(FhirProfile profile)
    {
        var handler = new HttpClientHandler();
        if (!profile.TlsVerify)
        {
            handler.ServerCertificateCustomValidationCallback =
                (message, cert, chain, errors) => true;
        }

        // mTLS client certificate binding (profile.Auth.ClientCertPath) is wired in a later phase.

        var authHandler = new FhirAuthDelegatingHandler(profile, _secretStore)
        {
            InnerHandler = handler
        };

        var client = new HttpClient(authHandler, disposeHandler: true);
        client.BaseAddress = null; // absolute URLs are built explicitly
        var timeoutSeconds = profile.TimeoutSeconds > 0 ? profile.TimeoutSeconds : 30;
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        return client;
    }

    private HttpRequestMessage BuildRequest(SendRequest request, FhirProfile profile)
    {
        var url = BuildUrl(profile, request);
        var method = request.HttpMethod switch
        {
            FhirHttpMethod.Get => HttpMethod.Get,
            FhirHttpMethod.Post => HttpMethod.Post,
            FhirHttpMethod.Put => HttpMethod.Put,
            FhirHttpMethod.Patch => new HttpMethod("PATCH"),
            FhirHttpMethod.Delete => HttpMethod.Delete,
            _ => HttpMethod.Post
        };

        var message = new HttpRequestMessage(method, url);

        // Accept header with FHIR media type + version.
        var accept = string.IsNullOrWhiteSpace(profile.FhirVersion)
            ? FhirJsonMediaType
            : $"{FhirJsonMediaType}; fhirVersion={profile.FhirVersion}";
        message.Headers.TryAddWithoutValidation("Accept", accept);

        bool hasBody = request.HttpMethod is FhirHttpMethod.Post or FhirHttpMethod.Put or FhirHttpMethod.Patch;
        if (hasBody)
        {
            message.Content = new StringContent(request.Payload ?? string.Empty, Encoding.UTF8, FhirJsonMediaType);
        }

        return message;
    }

    private static string BuildUrl(FhirProfile profile, SendRequest request)
    {
        var base_ = profile.BaseUrl.TrimEnd('/');

        // Special-case: server capability statement discovery.
        if (request.HttpMethod == FhirHttpMethod.Get &&
            string.Equals(request.ResourceType, "metadata", StringComparison.OrdinalIgnoreCase))
        {
            return $"{base_}/metadata";
        }

        var resourceType = string.IsNullOrWhiteSpace(request.ResourceType)
            ? profile.DefaultEndpoint
            : request.ResourceType;

        var sb = new StringBuilder(base_);
        if (!string.IsNullOrWhiteSpace(resourceType))
            sb.Append('/').Append(resourceType!.TrimStart('/'));
        if (!string.IsNullOrWhiteSpace(request.ResourceId))
            sb.Append('/').Append(request.ResourceId!.TrimStart('/'));

        return sb.ToString();
    }
}
