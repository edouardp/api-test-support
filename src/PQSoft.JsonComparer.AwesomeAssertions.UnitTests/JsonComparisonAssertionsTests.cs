using System.Text.Json;
using PQSoft.JsonComparer.AwesomeAssertions;
using Xunit;

namespace PQSoft.JsonComparer.AwesomeAssertions.UnitTests;

public class JsonComparisonAssertionsTests
{
    [Fact]
    public void FullyMatch_WithIdenticalJson_ShouldPass()
    {
        // Arrange
        var actualJson = """{"name": "John", "age": 30}""";
        var expectedJson = """{"name": "John", "age": 30}""";

        // Act & Assert
        actualJson.AsJsonString().Should().FullyMatch(expectedJson);
    }

    [Fact]
    public void FullyMatch_WithDifferentJson_ShouldThrow()
    {
        // Arrange
        var actualJson = """{"name": "John", "age": 30}""";
        var expectedJson = """{"name": "Jane", "age": 25}""";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            actualJson.AsJsonString().Should().FullyMatch(expectedJson));
    }

    [Fact]
    public void FullyMatch_WithTokenExtraction_ShouldExtractValues()
    {
        // Arrange
        var actualJson = """{"id": "12345", "name": "John"}""";
        var expectedJson = """{"id": "[[USERID]]", "name": "John"}""";

        // Act
        var assertions = actualJson.AsJsonString().Should();
        assertions.FullyMatch(expectedJson);

        // Assert
        Assert.Contains("USERID", assertions.ExtractedValues.Keys);
        Assert.Equal("12345", assertions.ExtractedValues["USERID"].GetString());
    }

    [Fact]
    public void ContainSubset_WithValidSubset_ShouldPass()
    {
        // Arrange
        var actualJson = """{"name": "John", "age": 30, "city": "New York"}""";
        var expectedSubset = """{"name": "John", "age": 30}""";

        // Act & Assert
        actualJson.AsJsonString().Should().ContainSubset(expectedSubset);
    }

    [Fact]
    public void ContainSubset_WithInvalidSubset_ShouldThrow()
    {
        // Arrange
        var actualJson = """{"name": "John", "age": 30}""";
        var expectedSubset = """{"name": "Jane", "city": "Boston"}""";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            actualJson.AsJsonString().Should().ContainSubset(expectedSubset));
    }

    [Fact]
    public void ContainSubset_WithTokenExtraction_ShouldExtractValues()
    {
        // Arrange
        var actualJson = """{"user": {"id": "67890", "name": "Alice"}, "status": "active"}""";
        var expectedSubset = """{"user": {"id": "[[USERID]]"}}""";

        // Act
        var assertions = actualJson.AsJsonString().Should();
        assertions.ContainSubset(expectedSubset);

        // Assert
        Assert.Contains("USERID", assertions.ExtractedValues.Keys);
        Assert.Equal("67890", assertions.ExtractedValues["USERID"].GetString());
    }

    [Fact]
    public void AsJsonString_WithValidJson_ShouldCreateJsonSubject()
    {
        // Arrange
        var json = """{"test": "value"}""";

        // Act
        var subject = json.AsJsonString();

        // Assert
        Assert.NotNull(subject);
        Assert.Equal(json, subject.Json);
    }

    [Fact]
    public void AsJsonString_WithNullJson_ShouldThrow()
    {
        // Arrange
        string json = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => json.AsJsonString());
    }
}