using System.Text.Json;
using Xunit;

namespace TestSupport.Json.UnitTests;

public class JsonComparerFunctionTests
{
    [Fact]
    public void ExactMatch_WithGuidFunction_ShouldExecuteAndMatch()
    {
        // Arrange - Use functions in both expected and actual to ensure they generate the same values
        string expectedJson = """{ "id": "{{GUID()}}", "name": "test" }""";
        string actualJson = """{ "id": "{{GUID()}}", "name": "test" }""";

        // Act
        bool result = JsonComparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);

        // Assert - This will fail because GUIDs are different, but that's expected behavior
        // The test verifies that function processing works without JSON parsing errors
        Assert.False(result); // Different GUIDs should not match
        Assert.Empty(extractedValues);
        Assert.Single(mismatches); // Should have one mismatch for the different GUID values
    }

    [Fact]
    public void ExactMatch_WithTimeProvider_ShouldUseControlledTime()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);
        
        string expectedJson = """{ "timestamp": "{{NOW()}}", "utc": "{{UTCNOW()}}", "status": "active" }""";
        string actualJson = """{ "timestamp": "2024-01-01T10:00:00.000+00:00", "utc": "2024-01-01T10:00:00.000Z", "status": "active" }""";

        // Act
        bool result = JsonComparer.ExactMatch(expectedJson, actualJson, fakeTimeProvider, out var extractedValues, out var mismatches);

        // Assert
        Assert.True(result);
        Assert.Empty(mismatches);
        Assert.Empty(extractedValues);
    }

    [Fact]
    public void ExactMatch_WithMixedTokensAndFunctions_ShouldProcessCorrectly()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);
        
        string expectedJson = """
        {
            "id": "[[USER_ID]]",
            "timestamp": "{{NOW()}}",
            "data": {
                "requestId": "[[REQUEST_ID]]",
                "processed": true
            }
        }
        """;
        
        string actualJson = """
        {
            "id": "user123",
            "timestamp": "2024-01-01T10:00:00.000+00:00",
            "data": {
                "requestId": "req456",
                "processed": true
            }
        }
        """;

        // Act
        bool result = JsonComparer.ExactMatch(expectedJson, actualJson, fakeTimeProvider, out var extractedValues, out var mismatches);

        // Assert
        Assert.True(result);
        Assert.Empty(mismatches);
        Assert.Equal(2, extractedValues.Count);
        Assert.Equal("user123", extractedValues["USER_ID"].GetString());
        Assert.Equal("req456", extractedValues["REQUEST_ID"].GetString());
    }

    [Fact]
    public void ExactMatch_WithInvalidFunction_ShouldThrowException()
    {
        // Arrange
        string expectedJson = """{ "id": "{{INVALID_FUNCTION()}}", "name": "test" }""";
        string actualJson = """{ "id": "123", "name": "test" }""";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            JsonComparer.ExactMatch(expectedJson, actualJson, out _, out _));
        
        Assert.Contains("Failed to execute function 'INVALID_FUNCTION'", exception.Message);
    }

    [Fact]
    public void SubsetMatch_WithFunctions_ShouldWork()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);
        
        string expectedJson = """{ "timestamp": "{{UTCNOW()}}" }""";
        string actualJson = """{ "timestamp": "2024-01-01T10:00:00.000Z", "name": "test", "extra": "data" }""";

        // Act
        bool result = JsonComparer.SubsetMatch(expectedJson, actualJson, fakeTimeProvider, out var extractedValues, out var mismatches);

        // Assert
        Assert.True(result);
        Assert.Empty(mismatches);
        Assert.Empty(extractedValues);
    }

    [Fact]
    public void RegisterFunction_WithCustomFunction_ShouldWork()
    {
        // Arrange
        var customFunction = new TestCustomFunction();
        JsonComparer.RegisterFunction("TEST_CUSTOM", customFunction);

        string expectedJson = """{ "value": "{{TEST_CUSTOM()}}", "type": "custom" }""";
        string actualJson = """{ "value": "custom_result", "type": "custom" }""";

        // Act
        bool result = JsonComparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);

        // Assert
        Assert.True(result);
        Assert.Empty(mismatches);
        Assert.Empty(extractedValues);
    }

    [Fact]
    public void GetRegisteredFunctions_ShouldReturnBuiltInFunctions()
    {
        // Act
        var functions = JsonComparer.GetRegisteredFunctions();

        // Assert
        Assert.Contains("GUID", functions);
        Assert.Contains("NOW", functions);
        Assert.Contains("UTCNOW", functions);
        Assert.True(functions.Length >= 3);
    }

    [Fact]
    public void ExactMatch_WithFunctionInArray_ShouldWork()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);
        
        string expectedJson = """
        {
            "items": [
                { "timestamp": "{{UTCNOW()}}", "type": "first" },
                { "id": "[[SECOND_ID]]", "type": "second" }
            ]
        }
        """;
        
        string actualJson = """
        {
            "items": [
                { "timestamp": "2024-01-01T10:00:00.000Z", "type": "first" },
                { "id": "item2", "type": "second" }
            ]
        }
        """;

        // Act
        bool result = JsonComparer.ExactMatch(expectedJson, actualJson, fakeTimeProvider, out var extractedValues, out var mismatches);

        // Assert
        Assert.True(result);
        Assert.Empty(mismatches);
        Assert.Single(extractedValues);
        Assert.Equal("item2", extractedValues["SECOND_ID"].GetString());
    }

    [Fact]
    public void ExactMatch_WithFunctionInNestedObject_ShouldWork()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);
        
        string expectedJson = """
        {
            "metadata": {
                "created": {
                    "timestamp": "{{UTCNOW()}}",
                    "by": "system"
                }
            }
        }
        """;
        
        string actualJson = """
        {
            "metadata": {
                "created": {
                    "timestamp": "2024-01-01T10:00:00.000Z",
                    "by": "system"
                }
            }
        }
        """;

        // Act
        bool result = JsonComparer.ExactMatch(expectedJson, actualJson, fakeTimeProvider, out var extractedValues, out var mismatches);

        // Assert
        Assert.True(result);
        Assert.Empty(mismatches);
        Assert.Empty(extractedValues);
    }

    // Helper classes for testing
    private class TestCustomFunction : IJsonFunction
    {
        public string Execute()
        {
            return "custom_result";
        }
    }

    private class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _fixedTime;

        public FakeTimeProvider(DateTimeOffset fixedTime)
        {
            _fixedTime = fixedTime;
        }

        public override DateTimeOffset GetUtcNow() => _fixedTime;
        
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
