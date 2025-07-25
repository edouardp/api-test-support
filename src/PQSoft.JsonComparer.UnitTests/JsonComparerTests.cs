using System.Text.Json;
//using JsonComparison; // Ensure this namespace matches where JsonComparer is defined

namespace TestSupport.Json.UnitTests;

public class JsonComparerTests
{
    [Fact]
    public void ExactMatch_IdenticalJson_NoTokens_ShouldReturnTrue()
    {
        string expected = """{ "name": "Alice", "age": 30, "active": true }""";
        string actual = """{ "name": "Alice", "age": 30, "active": true }""";

        bool result = JsonComparer.ExactMatch(expected, actual, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);

        Assert.True(result);
        Assert.Empty(extractedValues);
        Assert.Empty(mismatches);
    }

    [Fact]
    public void ExactMatch_WithToken_ShouldExtractTokenAndReturnTrue()
    {
        // In this test, the expected JSON contains a token that should extract the dynamic value.
        string expected = """{ "JobId": "[[JOBID]]", "status": "pending" }""";
        string actual = """{ "JobId": 12345, "status": "pending" }""";

        bool result = JsonComparer.ExactMatch(expected, actual, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);

        Assert.True(result);
        Assert.Single(extractedValues);
        Assert.True(extractedValues.ContainsKey("JOBID"));

        // Verify that the extracted token value is the actual value from the JSON.
        JsonElement tokenValue = extractedValues["JOBID"];
        Assert.Equal(JsonValueKind.Number, tokenValue.ValueKind);
        Assert.Equal("12345", tokenValue.GetRawText());
        Assert.Equal("12345", tokenValue.ToString());
        Assert.Empty(mismatches);
    }

    [Fact]
    public void ExactMatch_ExtraPropertyInActual_ShouldReturnFalse()
    {
        // For an exact match, extra properties in actual JSON cause a failure.
        string expected = """{ "name": "Alice" }""";
        string actual = """{ "name": "Alice", "extra": "property" }""";

        bool result = JsonComparer.ExactMatch(expected, actual, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);

        Assert.False(result);
        Assert.Contains(mismatches, m => m.Contains("Extra property"));
    }

    [Fact]
    public void SubsetMatch_ExpectedIsSubset_ShouldReturnTrue()
    {
        // In subset mode, extra properties in actual are allowed.
        string expected = """{ "name": "Alice" }""";
        string actual = """{ "name": "Alice", "age": 30, "active": true }""";

        bool result = JsonComparer.SubsetMatch(expected, actual, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);

        Assert.True(result);
        Assert.Empty(mismatches);
    }

    [Fact]
    public void SubsetMatch_MissingProperty_ShouldReturnFalse()
    {
        // When an expected property is missing in actual, the subset match should fail.
        string expected = """{ "name": "Alice", "age": 30 }""";
        string actual = """{ "name": "Alice" }""";

        bool result = JsonComparer.SubsetMatch(expected, actual, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);

        Assert.False(result);
        Assert.Contains(mismatches, m => m.Contains("Missing property 'age'"));
    }

    [Fact]
    public void ExactMatch_TypeMismatch_ShouldReturnFalse()
    {
        // Test where expected type (number) differs from actual type (string).
        string expected = @"{ ""count"": 10 }";
        string actual = @"{ ""count"": ""10"" }";

        bool result = JsonComparer.ExactMatch(expected, actual, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);

        Assert.False(result);
        Assert.Contains(mismatches, m => m.Contains("Type mismatch"));
    }

    [Fact]
    public void ExactMatch_ArrayMatch_ShouldReturnTrue()
    {
        // Test with arrays that match exactly.
        string expected = @"{ ""numbers"": [1, 2, 3] }";
        string actual = @"{ ""numbers"": [1, 2, 3] }";

        bool result = JsonComparer.ExactMatch(expected, actual, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);

        Assert.True(result);
        Assert.Empty(mismatches);
    }

    [Fact]
    public void ExactMatch_ArrayLengthMismatch_ShouldReturnFalse()
    {
        // For exact match, array lengths must be identical.
        string expected = @"{ ""numbers"": [1, 2, 3] }";
        string actual = @"{ ""numbers"": [1, 2] }";

        bool result = JsonComparer.ExactMatch(expected, actual, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);

        Assert.False(result);
        Assert.Contains(mismatches, m => m.Contains("Array length mismatch"));
    }

    [Fact]
    public void SubsetMatch_ArrayPrefix_ShouldReturnTrue()
    {
        // In subset mode, an expected array can be a prefix of the actual array.
        string expected = @"{ ""numbers"": [1, 2] }";
        string actual = @"{ ""numbers"": [1, 2, 3] }";

        bool result = JsonComparer.SubsetMatch(expected, actual, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);

        Assert.True(result);
        Assert.Empty(mismatches);
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

        bool result = JsonComparer.ExactMatch(expected, actual, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);

        // Expect failure due to age mismatch.
        Assert.False(result);

        // Check that token values are extracted.
        Assert.True(extractedValues.ContainsKey("USERID"));
        Assert.True(extractedValues.ContainsKey("EMAIL"));

        Assert.True(extractedValues["EMAIL"].ValueKind == JsonValueKind.String);
        Assert.True(extractedValues["USERID"].ValueKind == JsonValueKind.Number);

        // Verify that the mismatch for age is reported.
        Assert.Contains(mismatches, m => m.Contains("$.user.profile.age"));
    }

    [Fact]
    public void SubsetMatch_ArrayLengthExpectedGreaterThanActual_ShouldReturnFalse()
    {
        // Expected JSON has an array with three elements,
        // while the actual JSON has an array with only two elements.
        string expected = @"{ ""arr"": [1, 2, 3] }";
        string actual = @"{ ""arr"": [1, 2] }";

        bool result = JsonComparer.SubsetMatch(expected, actual, out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches);

        // Expecting a failure because the expected array is longer than the actual array in subset mode.
        Assert.False(result);
        Assert.Contains(mismatches, m => m.Contains("Array length mismatch in subset mode"));
    }

    [Fact]
    public void ExactMatch_StringMismatch_ShouldReportMismatch()
    {
        // Expected JSON with a specific string value.
        string expected = @"{ ""greeting"": ""Hello"" }";
        // Actual JSON with a different string value.
        string actual = @"{ ""greeting"": ""Hi"" }";

        bool result = JsonComparer.ExactMatch(expected, actual,
            out Dictionary<string, JsonElement> extractedValues,
            out List<string> mismatches);

        // The match should fail due to string mismatch.
        Assert.False(result);
        // Check that a mismatch message was added for the "greeting" property.
        Assert.Contains(mismatches, m => m.Contains("$.greeting") && m.Contains("String mismatch"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ExactMatch_BooleanMismatch_ShouldReportBooleanMismatch(bool expectedValue, bool actualValue)
    {
        string expected = $$"""{ "active": {{expectedValue.ToString().ToLower()}} }""";
        string actual = $$"""{ "active": {{actualValue.ToString().ToLower()}} }""";

        bool result = JsonComparer.ExactMatch(expected, actual,
            out Dictionary<string, JsonElement> extractedValues,
            out List<string> mismatches);

        // The match should fail because of the boolean mismatch.
        Assert.False(result);
        
        // Check that the mismatch message indicates a boolean mismatch (not type mismatch).
        var booleanMismatchFound = mismatches.Any(m => m.Contains("$.active") && m.Contains("Boolean mismatch"));
        var actualMismatchMessage = mismatches.FirstOrDefault(m => m.Contains("$.active"));
        
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

        bool result = JsonComparer.ExactMatch(expected, actual,
            out Dictionary<string, JsonElement> extractedValues,
            out List<string> mismatches);

        // The match should succeed because both values are null.
        Assert.True(result);
        // Ensure no mismatches were reported.
        Assert.Empty(mismatches);
    }

    [Fact]
    public void ExactMatch_EmptyJsonObjects_ShouldMatch()
    {
        // Both expected and actual JSON are empty objects.
        string expected = "{}";
        string actual = "{}";

        bool result = JsonComparer.ExactMatch(expected, actual,
            out Dictionary<string, JsonElement> extractedValues,
            out List<string> mismatches);

        // The match should succeed because both are empty objects.
        Assert.True(result);
        // Ensure no extracted values or mismatches.
        Assert.Empty(extractedValues);  
        Assert.Empty(mismatches);
    }

    [Fact]
    public void ExactMatch_EmptyArrays_ShouldMatch()
    {
        // Both expected and actual JSON contain empty arrays.
        string expected = """{ "items": [] }""";
        string actual = """{ "items": [] }""";

        bool result = JsonComparer.ExactMatch(expected, actual,
            out Dictionary<string, JsonElement> extractedValues,
            out List<string> mismatches);

        // The match should succeed because both arrays are empty.
        Assert.True(result);
        // Ensure no extracted values or mismatches.
        Assert.Empty(extractedValues);
        Assert.Empty(mismatches);
    }

    [Fact]
    public void ExactMatch_EmptyStrings_ShouldThrowJsonException()
    {
        // Empty JSON strings should throw JsonException during parsing.
        Assert.ThrowsAny<JsonException>(() =>
        {
            JsonComparer.ExactMatch("", "",
                out Dictionary<string, JsonElement> extractedValues,
                out List<string> mismatches);
        });
    }

    [Fact]
    public void ExactMatch_NullStrings_ShouldThrowArgumentNullException()
    {
        // Null JSON strings should throw ArgumentNullException during parsing.
        Assert.Throws<ArgumentNullException>(() =>
        {
            JsonComparer.ExactMatch(null!, null!,
                out Dictionary<string, JsonElement> extractedValues,
                out List<string> mismatches);
        });
    }
}
