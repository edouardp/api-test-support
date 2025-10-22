using PQSoft.HttpFile;
using PQSoft.JsonComparer;
using PQSoft.JsonComparer.AwesomeAssertions;
using PQSoft.JsonComparer.Functions;
using AwesomeAssertions;
using Reqnroll;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PQSoft.ReqNRoll;

[Binding]
public class ApiStepDefinitions
{
    protected HttpClient Client { get; }
    private HttpResponseMessage? _lastResponse;
    private string? _lastBody;
    private Dictionary<string, JsonElement> _variables = [];
    private readonly JsonFunctionRegistry _functionRegistry = new();

    public ApiStepDefinitions(HttpClient client)
    {
        Client = client;
    }

    [Given("the following request")]
    public async Task GivenTheFollowingRequest(string httpRequest)
    {
        httpRequest = SubstituteVariables(httpRequest);
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(httpRequest));
        var parser = new HttpFileParser();
        var request = await parser.ParseAsync(stream).FirstOrDefaultAsync();
        
        var requestMessage = request!.ToHttpRequestMessage();
        _lastResponse = await Client.SendAsync(requestMessage);
        _lastBody = await _lastResponse.Content.ReadAsStringAsync();
    }

    [Given(@"the variable '(.*)' is set to '(.*)'")]
    [Given(@"the variable ""(.*)"" is set to ""(.*)""")]
    public void GivenTheVariableIsSetTo(string name, string value)
    {
        value = ExecuteFunctions(value);
        _variables[name] = JsonDocument.Parse($"\"{value}\"").RootElement;
    }

    [Given(@"the variable '(.*)' is set to (-?\d+)$")]
    [Given(@"the variable ""(.*)"" is set to (-?\d+)$")]
    public void GivenTheVariableIsSetToInt(string name, int value)
    {
        _variables[name] = JsonDocument.Parse(value.ToString()).RootElement;
    }

    [Given(@"the variable '(.*)' is set to (-?\d+\.\d+)$")]
    [Given(@"the variable ""(.*)"" is set to (-?\d+\.\d+)$")]
    public void GivenTheVariableIsSetToDouble(string name, double value)
    {
        _variables[name] = JsonDocument.Parse(value.ToString()).RootElement;
    }

    [Given(@"the variable '(.*)' is set to (true|false)")]
    [Given(@"the variable ""(.*)"" is set to (true|false)")]
    public void GivenTheVariableIsSetToBool(string name, bool value)
    {
        _variables[name] = JsonDocument.Parse(value.ToString().ToLower()).RootElement;
    }

    [Then("the API returns the following response")]
    public async Task ThenTheApiReturnsTheFollowingResponse(string httpResponse)
    {
        httpResponse = SubstituteVariables(httpResponse);
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(httpResponse));
        var expected = await HttpResponseParser.ParseAsync(stream);

        _lastResponse.Should().NotBeNull();
        _lastResponse!.StatusCode.Should().Be(expected.StatusCode);
        _lastResponse.StatusCode.ToString().Should().Be(expected.ReasonPhrase);

        ValidateHeaders(expected.Headers);

        // Check if response is JSON based on Content-Type header or body content
        var contentType = expected.Headers.FirstOrDefault(h => 
            h.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));
        
        var isJson = (contentType != null && contentType.Value.Contains("json", StringComparison.OrdinalIgnoreCase)) ||
                     (contentType == null && expected.Body.TrimStart().StartsWith("{"));
        
        if (isJson)
        {
            // JSON response - use subset matching and token extraction
            var captured = _lastBody!.AsJsonString().Should().ContainSubset(expected.Body);
            _variables = _variables.Concat(captured.ExtractedValues)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value);
        }
        else
        {
            // Non-JSON response - do simple text comparison with token extraction
            var actualBody = _lastBody!;
            var expectedBody = expected.Body;
            
            // Extract tokens from expected body
            var tokenPattern = @"\[\[([A-Z_]+)\]\]";
            var matches = System.Text.RegularExpressions.Regex.Matches(expectedBody, tokenPattern);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var tokenName = match.Groups[1].Value;
                var tokenStart = match.Index;
                
                // Find what comes after the token in expected
                var tokenEnd = match.Index + match.Length;
                var afterToken = tokenEnd < expectedBody.Length ? expectedBody.Substring(tokenEnd) : "";
                
                // Find the next delimiter in actual body
                int nextDelimiterIndex;
                if (string.IsNullOrEmpty(afterToken))
                {
                    // Token is at the end, extract to end of actual body
                    nextDelimiterIndex = actualBody.Length;
                }
                else
                {
                    // Find where the "after token" text appears in actual body
                    var searchStart = Math.Min(tokenStart, actualBody.Length);
                    nextDelimiterIndex = actualBody.IndexOf(afterToken, searchStart, StringComparison.Ordinal);
                    if (nextDelimiterIndex < 0)
                    {
                        nextDelimiterIndex = actualBody.Length;
                    }
                }
                
                if (nextDelimiterIndex >= tokenStart && tokenStart < actualBody.Length)
                {
                    var extractedValue = actualBody.Substring(tokenStart, nextDelimiterIndex - tokenStart);
                    _variables[tokenName] = System.Text.Json.JsonDocument.Parse($"\"{extractedValue}\"").RootElement;
                    
                    // Replace token in expected with extracted value for comparison
                    expectedBody = expectedBody.Replace(match.Value, extractedValue);
                }
            }
            
            // Now compare the bodies
            actualBody.Should().Be(expectedBody);
        }
    }

    [Then(@"the variable '(.*)' is equals to '(.*)'")] 
    [Then(@"the variable ""(.*)"" is equals to ""(.*)""")] 
    public void ThenTheVariableIsEqualsTo(string name, string value)
    {
        if (!_variables.ContainsKey(name))
            throw new VariableNotFoundException(name, _variables.Keys);
        
        _variables[name].ToString().Should().Be(value);
    }

    [Then(@"the variable '(.*)' equals '(.*)'")]
    [Then(@"the variable ""(.*)"" equals ""(.*)""")]
    public void ThenTheVariableEqualsString(string name, string value)
    {
        if (!_variables.ContainsKey(name))
            throw new VariableNotFoundException(name, _variables.Keys);
        
        _variables[name].GetString().Should().Be(value);
    }

    [Then(@"the variable '(.*)' is of type '(.*)'")] 
    [Then(@"the variable ""(.*)"" is of type ""(.*)""")] 
    public void ThenTheVariableIsOfType(string name, string type)
    {
        if (!_variables.ContainsKey(name))
            throw new VariableNotFoundException(name, _variables.Keys);
        
        var kind = _variables[name].ValueKind;
        switch (type.ToLowerInvariant())
        {
            case "string": kind.Should().Be(JsonValueKind.String); break;
            case "number": kind.Should().Be(JsonValueKind.Number); break;
            case "boolean": kind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False); break;
            case "object": kind.Should().Be(JsonValueKind.Object); break;
            case "array": kind.Should().Be(JsonValueKind.Array); break;
            case "null": kind.Should().Be(JsonValueKind.Null); break;
            case "date":
                kind.Should().Be(JsonValueKind.String);
                DateTime.TryParse(_variables[name].GetString(), out _).Should().BeTrue();
                break;
            default: throw new InvalidOperationException($"Unknown type '{type}'");
        }
    }

    [Then(@"the variable '(.*)' matches '(.*)'")] 
    [Then(@"the variable ""(.*)"" matches ""(.*)""")] 
    public void ThenTheVariableMatches(string name, string regex)
    {
        if (!_variables.ContainsKey(name))
            throw new VariableNotFoundException(name, _variables.Keys);
        
        _variables[name].ToString().Should().MatchRegex(regex);
    }

    [Then(@"the variable '(.*)' equals (-?\d+)$")]
    [Then(@"the variable ""(.*)"" equals (-?\d+)$")]
    public void ThenTheVariableEqualsInt(string name, int expected)
    {
        if (!_variables.ContainsKey(name))
            throw new VariableNotFoundException(name, _variables.Keys);
        
        _variables[name].GetInt32().Should().Be(expected);
    }

    [Then(@"the variable '(.*)' equals (-?\d+\.\d+)$")]
    [Then(@"the variable ""(.*)"" equals (-?\d+\.\d+)$")]
    public void ThenTheVariableEqualsDouble(string name, double expected)
    {
        if (!_variables.ContainsKey(name))
            throw new VariableNotFoundException(name, _variables.Keys);
        
        _variables[name].GetDouble().Should().BeApproximately(expected, 0.0001);
    }

    [Then(@"the variable '(.*)' equals (-?[\d.]+) with delta ([\d.]+)")]
    [Then(@"the variable ""(.*)"" equals (-?[\d.]+) with delta ([\d.]+)")]
    public void ThenTheVariableEqualsWithDelta(string name, double expected, double delta)
    {
        if (!_variables.ContainsKey(name))
            throw new VariableNotFoundException(name, _variables.Keys);
        
        _variables[name].GetDouble().Should().BeApproximately(expected, delta);
    }

    [Then(@"the variable '(.*)' equals (true|false)")]
    [Then(@"the variable ""(.*)"" equals (true|false)")]
    public void ThenTheVariableEqualsBool(string name, bool expected)
    {
        if (!_variables.ContainsKey(name))
            throw new VariableNotFoundException(name, _variables.Keys);
        
        _variables[name].GetBoolean().Should().Be(expected);
    }

    private string SubstituteVariables(string text)
    {
        foreach (var kvp in _variables)
        {
            var value = kvp.Value.ValueKind switch
            {
                JsonValueKind.String => kvp.Value.GetString()!,
                JsonValueKind.Number => kvp.Value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                _ => kvp.Value.ToString()
            };
            text = text.Replace("{{" + kvp.Key + "}}", value);
        }
        return text;
    }

    private string ExecuteFunctions(string text)
    {
        var functionPattern = @"\{\{([A-Z_]+)\(\)\}\}";
        return Regex.Replace(text, functionPattern, match =>
        {
            var functionName = match.Groups[1].Value;
            return _functionRegistry.ExecuteFunction(functionName);
        });
    }

    private void ValidateHeaders(IEnumerable<ParsedHeader> expectedHeaders)
    {
        var allHeaders = _lastResponse!.Headers.Concat(_lastResponse.Content.Headers)
            .Select(h => h.Key).ToList();
        
        foreach (var expected in expectedHeaders)
        {
            if (!_lastResponse.Headers.TryGetValues(expected.Name, out var values) &&
                !_lastResponse.Content.Headers.TryGetValues(expected.Name, out values))
                throw new HeaderValidationException(expected.Name, allHeaders, "Header is missing");

            var actual = HttpHeadersParser.ParseHeader($"{expected.Name}: {values.First()}");
            
            if (actual.Value != expected.Value)
                throw new HeaderValidationException(expected.Name, allHeaders, 
                    $"Expected value '{expected.Value}' but got '{actual.Value}'");
            
            if (expected.Parameters.Any())
            {
                foreach (var expectedParam in expected.Parameters)
                {
                    if (!actual.Parameters.ContainsKey(expectedParam.Key))
                        throw new HeaderValidationException(expected.Name, allHeaders, 
                            $"Parameter '{expectedParam.Key}' is missing");
                    
                    if (actual.Parameters[expectedParam.Key] != expectedParam.Value)
                        throw new HeaderValidationException(expected.Name, allHeaders, 
                            $"Parameter '{expectedParam.Key}' expected '{expectedParam.Value}' but got '{actual.Parameters[expectedParam.Key]}'");
                }
            }
        }
    }
}
