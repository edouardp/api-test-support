using PQSoft.HttpFile;
using PQSoft.JsonComparer;
using PQSoft.JsonComparer.AwesomeAssertions;
using AwesomeAssertions;
using Reqnroll;
using System.Text;
using System.Text.Json;

namespace PQSoft.ReqNRoll;

[Binding]
public class ApiStepDefinitions
{
    protected HttpClient Client { get; }
    private HttpResponseMessage? _lastResponse;
    private string? _lastBody;
    private Dictionary<string, JsonElement> _variables = [];

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
    public void ThenTheVariableIsEqualsTo(string name, string value) =>
        _variables[name].ToString().Should().Be(value);

    [Then(@"the variable '(.*)' is of type '(.*)'")]
    public void ThenTheVariableIsOfType(string name, string type)
    {
        var kind = _variables[name].ValueKind;
        switch (type)
        {
            case "String": kind.Should().Be(JsonValueKind.String); break;
            case "Number": kind.Should().Be(JsonValueKind.Number); break;
            case "Boolean": kind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False); break;
            case "Object": kind.Should().Be(JsonValueKind.Object); break;
            case "Array": kind.Should().Be(JsonValueKind.Array); break;
            case "Null": kind.Should().Be(JsonValueKind.Null); break;
            case "Date":
                kind.Should().Be(JsonValueKind.String);
                DateTime.TryParse(_variables[name].GetString(), out _).Should().BeTrue();
                break;
            default: throw new InvalidOperationException($"Unknown type '{type}'");
        }
    }

    [Then(@"the variable '(.*)' matches '(.*)'")]
    public void ThenTheVariableMatches(string name, string regex) =>
        _variables[name].ToString().Should().MatchRegex(regex);

    private string SubstituteVariables(string text)
    {
        foreach (var kvp in _variables)
            text = text.Replace("{{" + kvp.Key + "}}", kvp.Value.ToString());
        return text;
    }

    private void ValidateHeaders(IEnumerable<ParsedHeader> expectedHeaders)
    {
        foreach (var expected in expectedHeaders)
        {
            if (!_lastResponse!.Headers.TryGetValues(expected.Name, out var values) &&
                !_lastResponse.Content.Headers.TryGetValues(expected.Name, out values))
                throw new InvalidOperationException($"Header '{expected.Name}' is missing.");

            var actual = HttpHeadersParser.ParseHeader($"{expected.Name}: {values.First()}");
            
            // Always compare name and value
            actual.Name.Should().Be(expected.Name);
            actual.Value.Should().Be(expected.Value);
            
            // If expected has parameters, validate them (strict mode)
            if (expected.Parameters.Any())
            {
                foreach (var expectedParam in expected.Parameters)
                {
                    actual.Parameters.Should().ContainKey(expectedParam.Key,
                        $"header '{expected.Name}' should have parameter '{expectedParam.Key}'");
                    actual.Parameters[expectedParam.Key].Should().Be(expectedParam.Value,
                        $"header '{expected.Name}' parameter '{expectedParam.Key}' should be '{expectedParam.Value}'");
                }
            }
            // Otherwise, ignore actual parameters (flexible mode for common cases)
        }
    }
}
