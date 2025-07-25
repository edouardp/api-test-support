namespace TestSupport;

/// <summary>
/// Function that returns the current local date and time.
/// Returns the current DateTime.Now in ISO 8601 format with timezone offset.
/// </summary>
public class NowFunction : IJsonFunction
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of NowFunction with the specified TimeProvider.
    /// </summary>
    /// <param name="timeProvider">The TimeProvider to use for getting current time.</param>
    public NowFunction(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Gets the current local date and time and returns it in ISO 8601 format.
    /// </summary>
    /// <returns>Current local date and time in format "yyyy-MM-ddTHH:mm:ss.fffK".</returns>
    public string Execute()
    {
        return _timeProvider.GetLocalNow().ToString("yyyy-MM-ddTHH:mm:ss.fffK");
    }
}
