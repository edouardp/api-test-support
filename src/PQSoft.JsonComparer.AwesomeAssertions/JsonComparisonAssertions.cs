using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AwesomeAssertions;

namespace PQSoft.JsonComparer.AwesomeAssertions;

/// <summary>
/// A simple wrapper around a JSON string that serves as the subject for JSON-specific assertions.
/// </summary>
/// <param name="json">The JSON string to wrap for assertions.</param>
/// <example>
/// <code>
/// var subject = new JsonSubject("""{"id": "12345", "createdAt": "2024-01-01T10:00:00Z"}""");
/// subject.WithTimeProvider(TimeProvider.System)
///        .Should()
///        .FullyMatch("""{"id": "[[ID]]", "createdAt": "{{NOW()}}"}""");
/// </code>
/// </example>
public class JsonSubject(string json)
{
    public string Json { get; } = json ?? throw new ArgumentNullException(nameof(json));
    public TimeProvider? TimeProvider { get; private set; }

    /// <summary>
    /// Configures the TimeProvider to use for time-based functions in JSON comparisons.
    /// </summary>
    /// <param name="timeProvider">The TimeProvider to use for time-based functions.</param>
    /// <returns>This JsonSubject instance for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// var fixedTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero));
    /// actualJson.AsJsonString()
    ///          .WithTimeProvider(fixedTime)
    ///          .Should()
    ///          .FullyMatch("""{"timestamp": "{{NOW()}}"}""");
    /// </code>
    /// </example>
    public JsonSubject WithTimeProvider(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        return this;
    }
}

/// <summary>
/// Custom AwesomeAssertions assertions for <see cref="JsonSubject"/>.
/// </summary>
[CustomAssertions]
public class JsonSubjectAssertions(JsonSubject subject)
{
    /// <summary>
    /// Holds the extracted token values from the last JSON comparison.
    /// Keys are token names (e.g. "JOBID") and values are the corresponding JsonElement extracted from the actual JSON.
    /// </summary>
    public Dictionary<string, JsonElement> ExtractedValues { get; private set; } = new Dictionary<string, JsonElement>();

    private readonly JsonSubject subject = subject ?? throw new ArgumentNullException(nameof(subject));

    /// <summary>
    /// Asserts that the subject JSON is exactly equivalent to the expected JSON.
    /// Any tokens in the expected JSON (e.g. "{{JOBID}}") are extracted and stored in <see cref="ExtractedValues"/>.
    /// </summary>
    /// <param name="expectedJson">The expected JSON string.</param>
    /// <param name="because">An optional reason message to include in the failure.</param>
    /// <param name="becauseArgs">Optional parameters for the reason message.</param>
    public JsonSubjectAssertions FullyMatch([StringSyntax(StringSyntaxAttribute.Json)] string expectedJson, string because = "", params object[] becauseArgs)
    {
        var comparer = new JsonComparer(subject.TimeProvider);
        var (isMatch, extractedValues, mismatches) = comparer.ExactMatch(expectedJson, subject.Json);
        ExtractedValues = extractedValues;

        if (!isMatch)
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
    public JsonSubjectAssertions ContainSubset([StringSyntax(StringSyntaxAttribute.Json)] string expectedJson, string because = "", params object[] becauseArgs)
    {
        var comparer = new JsonComparer(subject.TimeProvider);
        var (isMatch, extractedValues, mismatches) = comparer.SubsetMatch(expectedJson, subject.Json);
        ExtractedValues = extractedValues;

        if (!isMatch)
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
    /// <param name="json">The JSON string to wrap.</param>
    /// <returns>A JsonSubject that can be used for fluent assertions.</returns>
    /// <example>
    /// <code>
    /// string actualJson = """{"id": "12345", "name": "John"}""";
    /// actualJson.AsJsonString().Should().FullyMatch("""{"id": "[[ID]]", "name": "John"}""");
    /// </code>
    /// </example>
    public static JsonSubject AsJsonString(this string json)
    {
        return new JsonSubject(json);
    }

    /// <summary>
    /// Returns a <see cref="JsonSubjectAssertions"/> instance that can be used to assert on the JSON.
    /// </summary>
    /// <param name="subject">The JsonSubject to create assertions for.</param>
    /// <returns>A JsonSubjectAssertions instance for fluent assertion chaining.</returns>
    /// <example>
    /// <code>
    /// var jsonSubject = actualJson.AsJsonString();
    /// jsonSubject.Should().ContainSubset("""{"name": "John"}""");
    /// </code>
    /// </example>
    public static JsonSubjectAssertions Should(this JsonSubject subject)
    {
        return new JsonSubjectAssertions(subject);
    }
}
