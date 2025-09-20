using System.Text.Json;
//using JsonComparison; // Ensure this namespace matches where JsonComparer is defined

namespace PQSoft.JsonComparer.UnitTests;

public class JsonComparerTests
{
    [Fact]
    public void ExactMatch_IdenticalJson_NoTokens_ShouldReturnTrue()
    {
        // Arrange
        const string expected = """{ "name": "Alice", "age": 30, "active": true }""";
        const string actual = """{ "name": "Alice", "age": 30, "active": true }""";

        // Act
        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        // Assert
        Assert.True(result.IsMatch);
        Assert.Empty(result.ExtractedValues);
        Assert.Empty(result.Mismatches);
    }

    [Fact]
    public void ExactMatch_WithToken_ShouldExtractTokenAndReturnTrue()
    {
        // In this test, the expected JSON contains a token that should extract the dynamic value.
        string expected = """{ "JobId": "[[JOBID]]", "status": "pending" }""";
        string actual = """{ "JobId": 12345, "status": "pending" }""";

        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        Assert.True(result.IsMatch);
        Assert.Single(result.ExtractedValues);
        Assert.True(result.ExtractedValues.ContainsKey("JOBID"));

        // Verify that the extracted token value is the actual value from the JSON.
        JsonElement tokenValue = result.ExtractedValues["JOBID"];
        Assert.Equal(JsonValueKind.Number, tokenValue.ValueKind);
        Assert.Equal("12345", tokenValue.GetRawText());
        Assert.Equal("12345", tokenValue.ToString());
        Assert.Empty(result.Mismatches);
    }

    [Fact]
    public void ExactMatch_WithEmptyToken_ShouldExtractTokenAndReturnTrue()
    {
        // Test that empty tokens [[]] work as true discard tokens
        string expected = """{ "id": "[[]]", "name": "John", "status": "active" }""";
        string actual = """{ "id": "12345", "name": "John", "status": "active" }""";

        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        Assert.True(result.IsMatch);
        Assert.Empty(result.ExtractedValues); // Empty tokens should not be extracted
        Assert.False(result.ExtractedValues.ContainsKey("")); // No empty key should exist
        Assert.Empty(result.Mismatches);
    }

    [Fact]
    public void ExactMatch_ExtraPropertyInActual_ShouldReturnFalse()
    {
        // For an exact match, extra properties in actual JSON cause a failure.
        string expected = """{ "name": "Alice" }""";
        string actual = """{ "name": "Alice", "extra": "property" }""";

        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        Assert.False(result.IsMatch);
        Assert.Contains(result.Mismatches, m => m.Contains("Extra property"));
    }

    [Fact]
    public void SubsetMatch_ExpectedIsSubset_ShouldReturnTrue()
    {
        // In subset mode, extra properties in actual are allowed.
        string expected = """{ "name": "Alice" }""";
        string actual = """{ "name": "Alice", "age": 30, "active": true }""";

        var comparer = new JsonComparer();
        var result = comparer.SubsetMatch(expected, actual);

        Assert.True(result.IsMatch);
        Assert.Empty(result.Mismatches);
    }

    [Fact]
    public void SubsetMatch_MissingProperty_ShouldReturnFalse()
    {
        // When an expected property is missing in actual, the subset match should fail.
        string expected = """{ "name": "Alice", "age": 30 }""";
        string actual = """{ "name": "Alice" }""";

        var comparer = new JsonComparer();
        var result = comparer.SubsetMatch(expected, actual);

        Assert.False(result.IsMatch);
        Assert.Contains(result.Mismatches, m => m.Contains("Missing property 'age'"));
    }

    [Fact]
    public void ExactMatch_TypeMismatch_ShouldReturnFalse()
    {
        // Test where expected type (number) differs from actual type (string).
        string expected = @"{ ""count"": 10 }";
        string actual = @"{ ""count"": ""10"" }";

        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        Assert.False(result.IsMatch);
        Assert.Contains(result.Mismatches, m => m.Contains("Type mismatch"));
    }

    [Fact]
    public void ExactMatch_ArrayMatch_ShouldReturnTrue()
    {
        // Test with arrays that match exactly.
        string expected = @"{ ""numbers"": [1, 2, 3] }";
        string actual = @"{ ""numbers"": [1, 2, 3] }";

        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        Assert.True(result.IsMatch);
        Assert.Empty(result.Mismatches);
    }

    [Fact]
    public void ExactMatch_ArrayLengthMismatch_ShouldReturnFalse()
    {
        // For exact match, array lengths must be identical.
        string expected = @"{ ""numbers"": [1, 2, 3] }";
        string actual = @"{ ""numbers"": [1, 2] }";

        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        Assert.False(result.IsMatch);
        Assert.Contains(result.Mismatches, m => m.Contains("Array length mismatch"));
    }

    [Fact]
    public void SubsetMatch_ArrayPrefix_ShouldReturnTrue()
    {
        // In subset mode, an expected array can be a prefix of the actual array.
        string expected = @"{ ""numbers"": [1, 2] }";
        string actual = @"{ ""numbers"": [1, 2, 3] }";

        var comparer = new JsonComparer();
        var result = comparer.SubsetMatch(expected, actual);

        Assert.True(result.IsMatch);
        Assert.Empty(result.Mismatches);
    }

    [Fact]
    public void NestedTokens_ExtractionAndMismatchReporting()
    {
        // Test a nested structure with tokens and an intentional mismatch in a non-token field.
        string expected = """
            {
                "user": {
                    "id": [[USERID]],
                    "profile": {
                        "email": [[EMAIL]],
                        "age": 25
                    }
                }
            }
            """;
        string actual = """
            {
                "user": {
                    "id": 789,
                    "profile": {
                        "email": "user@example.com",
                        "age": 30
                    }
                }
            }
            """;

        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        // Expect failure due to age mismatch.
        Assert.False(result.IsMatch);

        // Check that token values are extracted.
        Assert.True(result.ExtractedValues.ContainsKey("USERID"));
        Assert.True(result.ExtractedValues.ContainsKey("EMAIL"));

        Assert.True(result.ExtractedValues["EMAIL"].ValueKind == JsonValueKind.String);
        Assert.True(result.ExtractedValues["USERID"].ValueKind == JsonValueKind.Number);

        // Verify that the mismatch for age is reported.
        Assert.Contains(result.Mismatches, m => m.Contains("$.user.profile.age"));
    }

    [Fact]
    public void SubsetMatch_ArrayLengthExpectedGreaterThanActual_ShouldReturnFalse()
    {
        // Expected JSON has an array with three elements,
        // while the actual JSON has an array with only two elements.
        string expected = @"{ ""arr"": [1, 2, 3] }";
        string actual = @"{ ""arr"": [1, 2] }";

        var comparer = new JsonComparer();
        var result = comparer.SubsetMatch(expected, actual);

        // Expecting a failure because the expected array is longer than the actual array in subset mode.
        Assert.False(result.IsMatch);
        Assert.Contains(result.Mismatches, m => m.Contains("Array length mismatch in subset mode"));
    }

    [Fact]
    public void ExactMatch_StringMismatch_ShouldReportMismatch()
    {
        // Expected JSON with a specific string value.
        string expected = @"{ ""greeting"": ""Hello"" }";
        // Actual JSON with a different string value.
        string actual = @"{ ""greeting"": ""Hi"" }";

        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        // The match should fail due to string mismatch.
        Assert.False(result.IsMatch);
        // Check that a mismatch message was added for the "greeting" property.
        Assert.Contains(result.Mismatches, m => m.Contains("$.greeting") && m.Contains("String mismatch"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ExactMatch_BooleanMismatch_ShouldReportBooleanMismatch(bool expectedValue, bool actualValue)
    {
        string expected = $$"""{ "active": {{expectedValue.ToString().ToLower()}} }""";
        string actual = $$"""{ "active": {{actualValue.ToString().ToLower()}} }""";

        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        // The match should fail because of the boolean mismatch.
        Assert.False(result.IsMatch);

        // Check that the mismatch message indicates a boolean mismatch (not type mismatch).
        var booleanMismatchFound = result.Mismatches.Any(m => m.Contains("$.active") && m.Contains("Boolean mismatch"));
        var actualMismatchMessage = result.Mismatches.FirstOrDefault(m => m.Contains("$.active"));

        Assert.True(booleanMismatchFound,
            $"Expected 'Boolean mismatch' message but got: '{actualMismatchMessage}'. " +
            $"Testing {expectedValue} -> {actualValue}");
    }

    [Fact]
    public void ExactMatch_NullValues_ShouldMatch()
    {
        // Expected JSON with a property set to null.
        string expected = """{ "value": null }""";
        // Actual JSON with the same property also set to null.
        string actual = """{ "value": null }""";

        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        // The match should succeed because both values are null.
        Assert.True(result.IsMatch);
        // Ensure no mismatches were reported.
        Assert.Empty(result.Mismatches);
    }

    [Fact]
    public void ExactMatch_EmptyJsonObjects_ShouldMatch()
    {
        // Both expected and actual JSON are empty objects.
        string expected = "{}";
        string actual = "{}";

        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        // The match should succeed because both are empty objects.
        Assert.True(result.IsMatch);
        // Ensure no extracted values or mismatches.
        Assert.Empty(result.ExtractedValues);
        Assert.Empty(result.Mismatches);
    }

    [Fact]
    public void ExactMatch_EmptyArrays_ShouldMatch()
    {
        // Both expected and actual JSON contain empty arrays.
        string expected = """{ "items": [] }""";
        string actual = """{ "items": [] }""";

        var comparer = new JsonComparer();
        var result = comparer.ExactMatch(expected, actual);

        // The match should succeed because both arrays are empty.
        Assert.True(result.IsMatch);
        // Ensure no extracted values or mismatches.
        Assert.Empty(result.ExtractedValues);
        Assert.Empty(result.Mismatches);
    }

    [Fact]
    public void ExactMatch_EmptyStrings_ShouldThrowJsonException()
    {
        // Empty JSON strings should throw JsonException during parsing.
        Assert.ThrowsAny<JsonException>(() =>
        {
            var comparer = new JsonComparer();
            comparer.ExactMatch("", "");
        });
    }

    [Fact]
    public void ExactMatch_NullStrings_ShouldThrowArgumentNullException()
    {
        // Null JSON strings should throw ArgumentNullException during parsing.
        Assert.Throws<ArgumentNullException>(() =>
        {
            var comparer = new JsonComparer();
            comparer.ExactMatch(null!, null!);
        });
    }
}
