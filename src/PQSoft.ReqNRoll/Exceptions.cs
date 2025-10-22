namespace PQSoft.ReqNRoll;

public class HeaderValidationException : Exception
{
    public string HeaderName { get; }
    public IEnumerable<string> ActualHeaders { get; }

    public HeaderValidationException(string headerName, IEnumerable<string> actualHeaders, string message)
        : base($"Header validation failed for '{headerName}': {message}\nActual headers: {string.Join(", ", actualHeaders)}")
    {
        HeaderName = headerName;
        ActualHeaders = actualHeaders;
    }
}

public class ResponseValidationException : Exception
{
    public string ExpectedBody { get; }
    public string ActualBody { get; }

    public ResponseValidationException(string message, string expectedBody, string actualBody)
        : base($"{message}\n\nExpected:\n{expectedBody}\n\nActual:\n{actualBody}")
    {
        ExpectedBody = expectedBody;
        ActualBody = actualBody;
    }
}

public class VariableNotFoundException : Exception
{
    public string VariableName { get; }
    public IEnumerable<string> AvailableVariables { get; }

    public VariableNotFoundException(string variableName, IEnumerable<string> availableVariables)
        : base($"Variable '{variableName}' not found. Available variables: {string.Join(", ", availableVariables)}")
    {
        VariableName = variableName;
        AvailableVariables = availableVariables;
    }
}
