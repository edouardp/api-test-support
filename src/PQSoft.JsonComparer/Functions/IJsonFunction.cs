namespace TestSupport;

/// <summary>
/// Represents a function that can be executed during JSON comparison preprocessing.
/// Functions are invoked when tokens matching the pattern {{FUNCTION_NAME()}} are encountered.
/// </summary>
public interface IJsonFunction
{
    /// <summary>
    /// Executes the function and returns the result as a string.
    /// The result will replace the function token in the JSON during preprocessing.
    /// </summary>
    /// <returns>The string result of the function execution.</returns>
    string Execute();
}
