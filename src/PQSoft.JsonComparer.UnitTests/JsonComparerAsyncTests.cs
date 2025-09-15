using System.Text.Json;
using AwesomeAssertions;

namespace PQSoft.JsonComparer.UnitTests;

public class JsonComparerAsyncTests
{
    [Fact]
    public async Task ExactMatchAsync_WithIdenticalJson_ShouldReturnTrue()
    {
        // Arrange
        const string expectedJson = """{"name": "John", "age": 30}""";
        const string actualJson = """{"name": "John", "age": 30}""";

        // Act
        var result = await JsonComparer.ExactMatchAsync(expectedJson, actualJson);

        // Assert
        result.IsMatch.Should().BeTrue();
        result.ExtractedValues.Should().BeEmpty();
        result.Mismatches.Should().BeEmpty();
    }

    [Fact]
    public async Task ExactMatchAsync_WithTokenExtraction_ShouldExtractValues()
    {
        // Arrange
        const string expectedJson = """{"id": "[[USER_ID]]", "name": "John"}""";
        const string actualJson = """{"id": "12345", "name": "John"}""";

        // Act
        var result = await JsonComparer.ExactMatchAsync(expectedJson, actualJson);

        // Assert
        result.IsMatch.Should().BeTrue();
        result.ExtractedValues.Should().ContainKey("USER_ID");
        result.ExtractedValues["USER_ID"].GetString().Should().Be("12345");
        result.Mismatches.Should().BeEmpty();
    }

    [Fact]
    public async Task SubsetMatchAsync_WithValidSubset_ShouldReturnTrue()
    {
        // Arrange
        const string expectedJson = """{"name": "John"}""";
        const string actualJson = """{"name": "John", "age": 30, "city": "New York"}""";

        // Act
        var result = await JsonComparer.SubsetMatchAsync(expectedJson, actualJson);

        // Assert
        result.IsMatch.Should().BeTrue();
        result.ExtractedValues.Should().BeEmpty();
        result.Mismatches.Should().BeEmpty();
    }

    [Fact]
    public async Task ExactMatchAsync_WithCancellation_ShouldSupportCancellation()
    {
        // Arrange
        const string expectedJson = """{"name": "John", "age": 30}""";
        const string actualJson = """{"name": "John", "age": 30}""";
        using var cts = new CancellationTokenSource();

        // Act
        var result = await JsonComparer.ExactMatchAsync(expectedJson, actualJson, cts.Token);

        // Assert
        result.IsMatch.Should().BeTrue();
    }

    [Fact]
    public async Task ExactMatchAsync_WithFunction_ShouldProcessFunctions()
    {
        // Arrange - use a token that will extract the actual GUID value
        const string expectedJson = """{"id": "[[GUID_ID]]", "requestId": "[[REQUEST_ID]]"}""";
        const string actualJson = """{"id": "550e8400-e29b-41d4-a716-446655440000", "requestId": "abc123"}""";

        // Act
        var result = await JsonComparer.ExactMatchAsync(expectedJson, actualJson);

        // Assert
        result.IsMatch.Should().BeTrue();
        result.ExtractedValues.Should().ContainKey("REQUEST_ID");
        result.ExtractedValues["REQUEST_ID"].GetString().Should().Be("abc123");
        result.ExtractedValues.Should().ContainKey("GUID_ID");
        result.ExtractedValues["GUID_ID"].GetString().Should().Be("550e8400-e29b-41d4-a716-446655440000");
    }
}