namespace TestSupport;

/// <summary>
/// Function that returns the current UTC date and time.
/// Returns the current DateTime.UtcNow in ISO 8601 format with 'Z' timezone indicator.
/// </summary>
public class UtcNowFunction : IJsonFunction
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of UtcNowFunction with the specified TimeProvider.
    /// </summary>
    /// <param name="timeProvider">The TimeProvider to use for getting current time.</param>
    public UtcNowFunction(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Gets the current UTC date and time and returns it in ISO 8601 format.
    /// </summary>
    /// <returns>Current UTC date and time in format "yyyy-MM-ddTHH:mm:ss.fffZ".</returns>
    public string Execute()
    {
        return _timeProvider.GetUtcNow().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }
}
