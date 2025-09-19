using System.Diagnostics.CodeAnalysis;
using PQSoft.JsonComparer.Functions.BuiltInFunctions;

namespace PQSoft.JsonComparer.Functions;

/// <summary>
/// Registry for managing JSON functions that can be executed during comparison preprocessing.
/// Provides registration, lookup, and execution capabilities for both built-in and custom functions.
/// </summary>
public class JsonFunctionRegistry
{
    private readonly Dictionary<string, IJsonFunction> functions;

    /// <summary>
    /// Initializes a new instance of the JsonFunctionRegistry with built-in functions registered.
    /// </summary>
    /// <param name="timeProvider">Optional TimeProvider for time-based functions. Uses TimeProvider.System if not provided.</param>
    public JsonFunctionRegistry(TimeProvider? timeProvider = null)
    {
        functions = new Dictionary<string, IJsonFunction>(StringComparer.OrdinalIgnoreCase);
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

        ArgumentNullException.ThrowIfNull(function);

        if (!functions.TryAdd(name, function))
            throw new ArgumentException($"Function '{name}' is already registered.", nameof(name));
    }

    /// <summary>
    /// Attempts to retrieve a function by name.
    /// Function names are case-insensitive.
    /// </summary>
    /// <param name="name">The name of the function to retrieve.</param>
    /// <param name="function">The function if found, null otherwise.</param>
    /// <returns>True if the function was found, false otherwise.</returns>
    public bool TryGetFunction(string name, [NotNullWhen(true)] out IJsonFunction? function)
    {
        return functions.TryGetValue(name, out function);
    }

    /// <summary>
    /// Gets the names of all registered functions.
    /// </summary>
    /// <returns>An array of registered function names.</returns>
    public string[] GetRegisteredFunctions()
    {
        return functions.Keys.ToArray();
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
            return function.Execute();
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
        functions["GUID"] = new GuidFunction();
        functions["NOW"] = new NowFunction(timeProvider);
        functions["UTCNOW"] = new UtcNowFunction(timeProvider);
    }
}
