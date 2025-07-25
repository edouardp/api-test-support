using System.Text.Json;
using AwesomeAssertions;

namespace PQSoft.JsonComparer.AwesomeAssertions;

/// <summary>
/// A simple wrapper around a JSON string that serves as the subject for JSON-specific assertions.
/// </summary>
public class JsonSubject
{
    public string Json { get; }

    public JsonSubject(string json)
    {
        Json = json ?? throw new ArgumentNullException(nameof(json));
    }
}

/// <summary>
/// Custom AwesomeAssertions assertions for <see cref="JsonSubject"/>.
/// </summary>
[CustomAssertions]
public class JsonSubjectAssertions
{
    /// <summary>
    /// Holds the extracted token values from the last JSON comparison.
    /// Keys are token names (e.g. "JOBID") and values are the corresponding JsonElement extracted from the actual JSON.
    /// </summary>
    public Dictionary<string, JsonElement> ExtractedValues { get; private set; } = new Dictionary<string, JsonElement>();

    private readonly JsonSubject _subject;

    public JsonSubjectAssertions(JsonSubject subject)
    {
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
    }

    /// <summary>
    /// Asserts that the subject JSON is exactly equivalent to the expected JSON.
    /// Any tokens in the expected JSON (e.g. "{{JOBID}}") are extracted and stored in <see cref="ExtractedValues"/>.
    /// </summary>
    /// <param name="expectedJson">The expected JSON string.</param>
    /// <param name="because">An optional reason message to include in the failure.</param>
    /// <param name="becauseArgs">Optional parameters for the reason message.</param>
    public JsonSubjectAssertions FullyMatch(string expectedJson, string because = "", params object[] becauseArgs)
    {
        bool result = JsonComparer.ExactMatch(expectedJson, _subject.Json, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);
        ExtractedValues = extractedValues;

        if (!result)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" {string.Format(because, becauseArgs)}";
            throw new InvalidOperationException($"Expected JSON to be equivalent{reason}, but found the following mismatches: {string.Join(", ", mismatches)}");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the subject JSON contains the expected JSON as a subset.
    /// Any tokens in the expected JSON are extracted and stored in <see cref="ExtractedValues"/>.
    /// </summary>
    /// <param name="expectedJson">The expected JSON subset.</param>
    /// <param name="because">An optional reason message to include in the failure.</param>
    /// <param name="becauseArgs">Optional parameters for the reason message.</param>
    public JsonSubjectAssertions ContainSubset(string expectedJson, string because = "", params object[] becauseArgs)
    {
        bool result = JsonComparer.SubsetMatch(expectedJson, _subject.Json, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);
        ExtractedValues = extractedValues;

        if (!result)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" {string.Format(because, becauseArgs)}";
            throw new InvalidOperationException($"Expected JSON to contain the subset JSON{reason}, but found the following mismatches: {string.Join(", ", mismatches)}");
        }

        return this;
    }
}

/// <summary>
/// Extension methods to easily wrap a JSON string as a <see cref="JsonSubject"/> and obtain its custom assertions.
/// </summary>
public static class JsonSubjectExtensions
{
    /// <summary>
    /// Wraps the JSON string in a <see cref="JsonSubject"/>.
    /// </summary>
    public static JsonSubject AsJsonString(this string json)
    {
        return new JsonSubject(json);
    }

    /// <summary>
    /// Returns a <see cref="JsonSubjectAssertions"/> instance that can be used to assert on the JSON.
    /// </summary>
    public static JsonSubjectAssertions Should(this JsonSubject subject)
    {
        return new JsonSubjectAssertions(subject);
    }
}