namespace TestSupport;

/// <summary>
/// Registry for managing JSON functions that can be executed during comparison preprocessing.
/// Provides registration, lookup, and execution capabilities for both built-in and custom functions.
/// </summary>
public class JsonFunctionRegistry
{
    private readonly Dictionary<string, IJsonFunction> _functions;

    /// <summary>
    /// Initializes a new instance of the JsonFunctionRegistry with built-in functions registered.
    /// </summary>
    /// <param name="timeProvider">Optional TimeProvider for time-based functions. Uses TimeProvider.System if not provided.</param>
    public JsonFunctionRegistry(TimeProvider? timeProvider = null)
    {
        _functions = new Dictionary<string, IJsonFunction>(StringComparer.OrdinalIgnoreCase);
        RegisterBuiltInFunctions(timeProvider ?? TimeProvider.System);
    }

    /// <summary>
    /// Registers a function with the specified name.
    /// Function names are case-insensitive.
    /// </summary>
    /// <param name="name">The name of the function (without parentheses).</param>
    /// <param name="function">The function implementation.</param>
    /// <exception cref="ArgumentException">Thrown when name is null, empty, or already registered.</exception>
    public void RegisterFunction(string name, IJsonFunction function)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Function name cannot be null or empty.", nameof(name));
        
        if (function == null)
            throw new ArgumentNullException(nameof(function));

        if (_functions.ContainsKey(name))
            throw new ArgumentException($"Function '{name}' is already registered.", nameof(name));

        _functions[name] = function;
    }

    /// <summary>
    /// Attempts to retrieve a function by name.
    /// Function names are case-insensitive.
    /// </summary>
    /// <param name="name">The name of the function to retrieve.</param>
    /// <param name="function">The function if found, null otherwise.</param>
    /// <returns>True if the function was found, false otherwise.</returns>
    public bool TryGetFunction(string name, out IJsonFunction? function)
    {
        return _functions.TryGetValue(name, out function);
    }

    /// <summary>
    /// Gets the names of all registered functions.
    /// </summary>
    /// <returns>An array of registered function names.</returns>
    public string[] GetRegisteredFunctions()
    {
        return _functions.Keys.ToArray();
    }

    /// <summary>
    /// Executes a function by name and returns the result.
    /// </summary>
    /// <param name="name">The name of the function to execute.</param>
    /// <returns>The result of the function execution.</returns>
    /// <exception cref="ArgumentException">Thrown when the function is not found.</exception>
    public string ExecuteFunction(string name)
    {
        if (!TryGetFunction(name, out var function))
            throw new ArgumentException($"Function '{name}' is not registered.", nameof(name));

        try
        {
            return function!.Execute();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error executing function '{name}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Registers all built-in functions.
    /// </summary>
    /// <param name="timeProvider">The TimeProvider to use for time-based functions.</param>
    private void RegisterBuiltInFunctions(TimeProvider timeProvider)
    {
        _functions["GUID"] = new GuidFunction();
        _functions["NOW"] = new NowFunction(timeProvider);
        _functions["UTCNOW"] = new UtcNowFunction(timeProvider);
    }
}
