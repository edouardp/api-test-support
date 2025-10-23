namespace PQSoft.ReqNRoll;

public class HeaderValidationException(string headerName, IList<string> actualHeaders, string message)
    : Exception(
        $"Header validation failed for '{headerName}': {message}\nActual headers: {string.Join(", ", actualHeaders)}")
{
    public string HeaderName { get; } = headerName;
    public IList<string> ActualHeaders { get; } = actualHeaders;
}

public class ResponseValidationException(string message, string expectedBody, string actualBody)
    : Exception($"{message}\n\nExpected:\n{expectedBody}\n\nActual:\n{actualBody}")
{
    public string ExpectedBody { get; } = expectedBody;
    public string ActualBody { get; } = actualBody;
}

public class VariableNotFoundException(string variableName, IList<string> availableVariables) : Exception(
    $"Variable '{variableName}' not found. Available variables: {string.Join(", ", availableVariables)}")
{
    public string VariableName { get; } = variableName;
    public IList<string> AvailableVariables { get; } = availableVariables;
}
