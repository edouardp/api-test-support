using PQSoft.JsonComparer.Functions;

namespace PQSoft.JsonComparer.UnitTests;

/// <summary>
/// Tests specifically designed to increase code coverage for JsonComparer
/// </summary>
public class JsonComparerCoverageTests
{
    [Fact]
    public void RegisterFunction_NullName_ShouldThrowArgumentException()
    {
        // Arrange
        var comparer = new JsonComparer();
        var mockFunction = new TestFunction();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => comparer.RegisterFunction(null!, mockFunction));
        Assert.Contains("Function name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void RegisterFunction_EmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var comparer = new JsonComparer();
        var mockFunction = new TestFunction();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => comparer.RegisterFunction("", mockFunction));
        Assert.Contains("Function name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void RegisterFunction_WhitespaceName_ShouldThrowArgumentException()
    {
        // Arrange
        var comparer = new JsonComparer();
        var mockFunction = new TestFunction();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => comparer.RegisterFunction("   ", mockFunction));
        Assert.Contains("Function name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void RegisterFunction_NullFunction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var comparer = new JsonComparer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => comparer.RegisterFunction("TEST", null!));
    }

    [Fact]
    public void RegisterFunction_DuplicateName_ShouldThrowArgumentException()
    {
        // Arrange
        var comparer = new JsonComparer();
        var mockFunction = new TestFunction();
        comparer.RegisterFunction("TEST", mockFunction);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => comparer.RegisterFunction("TEST", mockFunction));
        Assert.Contains("Function 'TEST' is already registered", exception.Message);
    }

    [Fact]
    public void RegisterFunction_CaseInsensitiveDuplicate_ShouldThrowArgumentException()
    {
        // Arrange
        var comparer = new JsonComparer();
        var mockFunction = new TestFunction();
        comparer.RegisterFunction("test", mockFunction);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => comparer.RegisterFunction("TEST", mockFunction));
        Assert.Contains("Function 'TEST' is already registered", exception.Message);
    }

    [Fact]
    public void ExactMatch_FunctionExecutionFailure_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var comparer = new JsonComparer();
        var failingFunction = new FailingFunction();
        comparer.RegisterFunction("FAIL", failingFunction);

        var expectedJson = """{"value": "{{FAIL()}}"}""";
        var actualJson = """{"value": "test"}""";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            comparer.ExactMatch(expectedJson, actualJson, out _, out _));
        Assert.Contains("Failed to execute function 'FAIL'", exception.Message);
    }

    [Fact]
    public void ExactMatch_UnregisteredFunction_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var comparer = new JsonComparer();
        var expectedJson = """{"value": "{{UNKNOWN()}}"}""";
        var actualJson = """{"value": "test"}""";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            comparer.ExactMatch(expectedJson, actualJson, out _, out _));
        Assert.Contains("Failed to execute function 'UNKNOWN'", exception.Message);
    }

    [Fact]
    public void GetRegisteredFunctions_ShouldReturnBuiltInFunctions()
    {
        // Arrange
        var comparer = new JsonComparer();

        // Act
        var functions = comparer.GetRegisteredFunctions();

        // Assert
        Assert.Contains("GUID", functions);
        Assert.Contains("NOW", functions);
        Assert.Contains("UTCNOW", functions);
        Assert.Equal(3, functions.Length);
    }

    [Fact]
    public void GetRegisteredFunctions_WithCustomFunction_ShouldIncludeCustom()
    {
        // Arrange
        var comparer = new JsonComparer();
        var customFunction = new TestFunction();
        comparer.RegisterFunction("CUSTOM", customFunction);

        // Act
        var functions = comparer.GetRegisteredFunctions();

        // Assert
        Assert.Contains("CUSTOM", functions);
        Assert.Equal(4, functions.Length);
    }

    [Fact]
    public void ExactMatch_BothNullJsonElements_ShouldReturnTrue()
    {
        // Arrange
        var comparer = new JsonComparer();
        var expectedJson = """{"value": null}""";
        var actualJson = """{"value": null}""";

        // Act
        var result = comparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);

        // Assert
        Assert.True(result);
        Assert.Empty(extractedValues);
        Assert.Empty(mismatches);
    }

    [Fact]
    public void ExactMatch_ExpectedNullActualValue_ShouldReturnFalse()
    {
        // Arrange
        var comparer = new JsonComparer();
        var expectedJson = """{"value": null}""";
        var actualJson = """{"value": "test"}""";

        // Act
        var result = comparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);

        // Assert
        Assert.False(result);
        Assert.Single(mismatches);
    }

    [Fact]
    public void ExactMatch_ExpectedValueActualNull_ShouldReturnFalse()
    {
        // Arrange
        var comparer = new JsonComparer();
        var expectedJson = """{"value": "test"}""";
        var actualJson = """{"value": null}""";

        // Act
        var result = comparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);

        // Assert
        Assert.False(result);
        Assert.Single(mismatches);
    }

    private class TestFunction : IJsonFunction
    {
        public string Execute() => "test-result";
    }

    private class FailingFunction : IJsonFunction
    {
        public string Execute() => throw new InvalidOperationException("Test failure");
    }
}
