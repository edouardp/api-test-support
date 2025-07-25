using Xunit;

namespace TestSupport.Json.UnitTests;

public class JsonFunctionTests
{
    [Fact]
    public void GuidFunction_Execute_ShouldReturnValidGuid()
    {
        // Arrange
        var function = new GuidFunction();

        // Act
        string result = function.Execute();

        // Assert
        Assert.True(Guid.TryParse(result, out _), $"Result '{result}' should be a valid GUID");
        Assert.Equal(36, result.Length); // Standard GUID string length
    }

    [Fact]
    public void GuidFunction_Execute_ShouldReturnUniqueValues()
    {
        // Arrange
        var function = new GuidFunction();

        // Act
        string result1 = function.Execute();
        string result2 = function.Execute();

        // Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void NowFunction_Execute_ShouldReturnValidDateTime()
    {
        // Arrange
        var function = new NowFunction();

        // Act
        string result = function.Execute();

        // Assert
        Assert.True(DateTime.TryParse(result, out _), $"Result '{result}' should be a valid DateTime");
        Assert.Contains("T", result); // ISO 8601 format should contain 'T'
    }

    [Fact]
    public void NowFunction_Execute_WithTimeProvider_ShouldReturnControlledTime()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);
        var function = new NowFunction(fakeTimeProvider);

        // Act
        string result = function.Execute();

        // Assert
        Assert.Equal("2024-01-01T10:00:00.000+00:00", result);
    }

    [Fact]
    public void UtcNowFunction_Execute_ShouldReturnValidDateTime()
    {
        // Arrange
        var function = new UtcNowFunction();

        // Act
        string result = function.Execute();

        // Assert
        Assert.True(DateTime.TryParse(result, out _), $"Result '{result}' should be a valid DateTime");
        Assert.Contains("T", result); // ISO 8601 format should contain 'T'
        Assert.EndsWith("Z", result); // UTC format should end with 'Z'
    }

    [Fact]
    public void UtcNowFunction_Execute_WithTimeProvider_ShouldReturnControlledTime()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);
        var function = new UtcNowFunction(fakeTimeProvider);

        // Act
        string result = function.Execute();

        // Assert
        Assert.Equal("2024-01-01T10:00:00.000Z", result);
    }

    // Helper class for testing
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

public class JsonFunctionRegistryTests
{
    [Fact]
    public void Constructor_ShouldRegisterBuiltInFunctions()
    {
        // Arrange & Act
        var registry = new JsonFunctionRegistry();

        // Assert
        var functions = registry.GetRegisteredFunctions();
        Assert.Contains("GUID", functions);
        Assert.Contains("NOW", functions);
        Assert.Contains("UTCNOW", functions);
        Assert.Equal(3, functions.Length);
    }

    [Fact]
    public void Constructor_WithTimeProvider_ShouldRegisterBuiltInFunctions()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);

        // Act
        var registry = new JsonFunctionRegistry(fakeTimeProvider);

        // Assert
        var functions = registry.GetRegisteredFunctions();
        Assert.Contains("GUID", functions);
        Assert.Contains("NOW", functions);
        Assert.Contains("UTCNOW", functions);
        Assert.Equal(3, functions.Length);
        
        // Verify time functions use the provided TimeProvider
        var nowResult = registry.ExecuteFunction("NOW");
        var utcNowResult = registry.ExecuteFunction("UTCNOW");
        Assert.Equal("2024-01-01T10:00:00.000+00:00", nowResult);
        Assert.Equal("2024-01-01T10:00:00.000Z", utcNowResult);
    }

    [Fact]
    public void RegisterFunction_WithValidFunction_ShouldSucceed()
    {
        // Arrange
        var registry = new JsonFunctionRegistry();
        var customFunction = new TestFunction();

        // Act
        registry.RegisterFunction("TEST", customFunction);

        // Assert
        Assert.True(registry.TryGetFunction("TEST", out var retrievedFunction));
        Assert.Same(customFunction, retrievedFunction);
    }

    [Fact]
    public void RegisterFunction_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = new JsonFunctionRegistry();
        var customFunction = new TestFunction();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => registry.RegisterFunction(null!, customFunction));
    }

    [Fact]
    public void RegisterFunction_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = new JsonFunctionRegistry();
        var customFunction = new TestFunction();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => registry.RegisterFunction("", customFunction));
    }

    [Fact]
    public void RegisterFunction_WithNullFunction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var registry = new JsonFunctionRegistry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.RegisterFunction("TEST", null!));
    }

    [Fact]
    public void RegisterFunction_WithDuplicateName_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = new JsonFunctionRegistry();
        var function1 = new TestFunction();
        var function2 = new TestFunction();
        registry.RegisterFunction("TEST", function1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => registry.RegisterFunction("TEST", function2));
        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void TryGetFunction_WithExistingFunction_ShouldReturnTrue()
    {
        // Arrange
        var registry = new JsonFunctionRegistry();

        // Act
        bool result = registry.TryGetFunction("GUID", out var function);

        // Assert
        Assert.True(result);
        Assert.NotNull(function);
        Assert.IsType<GuidFunction>(function);
    }

    [Fact]
    public void TryGetFunction_WithNonExistingFunction_ShouldReturnFalse()
    {
        // Arrange
        var registry = new JsonFunctionRegistry();

        // Act
        bool result = registry.TryGetFunction("NONEXISTENT", out var function);

        // Assert
        Assert.False(result);
        Assert.Null(function);
    }

    [Fact]
    public void TryGetFunction_ShouldBeCaseInsensitive()
    {
        // Arrange
        var registry = new JsonFunctionRegistry();

        // Act
        bool result1 = registry.TryGetFunction("guid", out var function1);
        bool result2 = registry.TryGetFunction("GUID", out var function2);
        bool result3 = registry.TryGetFunction("Guid", out var function3);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
        Assert.NotNull(function1);
        Assert.NotNull(function2);
        Assert.NotNull(function3);
    }

    [Fact]
    public void ExecuteFunction_WithValidFunction_ShouldReturnResult()
    {
        // Arrange
        var registry = new JsonFunctionRegistry();

        // Act
        string result = registry.ExecuteFunction("GUID");

        // Assert
        Assert.True(Guid.TryParse(result, out _));
    }

    [Fact]
    public void ExecuteFunction_WithNonExistentFunction_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = new JsonFunctionRegistry();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => registry.ExecuteFunction("NONEXISTENT"));
        Assert.Contains("not registered", exception.Message);
    }

    [Fact]
    public void ExecuteFunction_WithThrowingFunction_ShouldWrapException()
    {
        // Arrange
        var registry = new JsonFunctionRegistry();
        var throwingFunction = new ThrowingTestFunction();
        registry.RegisterFunction("THROWING", throwingFunction);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => registry.ExecuteFunction("THROWING"));
        Assert.Contains("Error executing function 'THROWING'", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    // Helper classes for testing
    private class TestFunction : IJsonFunction
    {
        public string Execute() => "test_result";
    }

    private class ThrowingTestFunction : IJsonFunction
    {
        public string Execute() => throw new InvalidOperationException("Test exception");
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
