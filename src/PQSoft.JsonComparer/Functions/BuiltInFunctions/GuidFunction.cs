namespace TestSupport;

/// <summary>
/// Function that generates a new GUID.
/// Returns a new GUID in standard string format (e.g., "12345678-1234-1234-1234-123456789abc").
/// </summary>
public class GuidFunction : IJsonFunction
{
    /// <summary>
    /// Generates a new GUID and returns it as a string.
    /// </summary>
    /// <returns>A new GUID in standard string format.</returns>
    public string Execute()
    {
        return Guid.NewGuid().ToString();
    }
}
