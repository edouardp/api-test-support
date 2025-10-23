using System.Globalization;
using PQSoft.HttpFile;
using PQSoft.JsonComparer.AwesomeAssertions;
using PQSoft.JsonComparer.Functions;
using AwesomeAssertions;
using Reqnroll;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PQSoft.ReqNRoll;

[Binding]
public class ApiStepDefinitions(HttpClient client)
{
    private HttpClient Client { get; } = client;
    private HttpResponseMessage? lastResponse;
    private string? lastBody;
    private Dictionary<string, JsonElement> variables = [];
    private readonly JsonFunctionRegistry functionRegistry = new();

    [Given("the following request")]
    public async Task GivenTheFollowingRequest(string httpRequest)
    {
        httpRequest = SubstituteVariables(httpRequest);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(httpRequest));
        var parser = new HttpFileParser();
        var request = await parser.ParseAsync(stream).FirstOrDefaultAsync();

        var requestMessage = request!.ToHttpRequestMessage();
        lastResponse = await Client.SendAsync(requestMessage);
        lastBody = await lastResponse.Content.ReadAsStringAsync();
    }

    [Given(@"the variable '(.*)' is set to '(.*)'")]
    [Given("""
           the variable "(.*)" is set to "(.*)"
           """)]
    public void GivenTheVariableIsSetTo(string name, string value)
    {
        value = ExecuteFunctions(value);
        variables[name] = JsonDocument.Parse($"\"{value}\"").RootElement;
    }

    [Given("""the variable '(.*)' is set to (-?\d+)$""")]
    [Given("""the variable "(.*)" is set to (-?\d+)$""")]
    public void GivenTheVariableIsSetToInt(string name, int value)
    {
        variables[name] = JsonDocument.Parse(value.ToString()).RootElement;
    }

    [Given("""the variable '(.*)' is set to (-?\d+\.\d+)$""")]
    [Given("""the variable "(.*)" is set to (-?\d+\.\d+)$""")]
    public void GivenTheVariableIsSetToDouble(string name, double value)
    {
        variables[name] = JsonDocument.Parse(value.ToString(CultureInfo.InvariantCulture)).RootElement;
    }

    [Given("the variable '(.*)' is set to (true|false)")]
    [Given("""the variable "(.*)" is set to (true|false)""")]
    public void GivenTheVariableIsSetToTrueOrFalse(string name, bool value)
    {
        variables[name] = JsonDocument.Parse(value.ToString().ToLower()).RootElement;
    }

    [Then("the API returns the following response")]
    public async Task ThenTheApiReturnsTheFollowingResponse(string httpResponse)
    {
        httpResponse = SubstituteVariables(httpResponse);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(httpResponse));
        var expected = await HttpResponseParser.ParseAsync(stream);

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(expected.StatusCode);
        lastResponse.StatusCode.ToString().Should().Be(expected.ReasonPhrase);

        ValidateHeaders(expected.Headers);

        // Check if response is JSON based on Content-Type header or body content
        var contentType = expected.Headers.FirstOrDefault(h =>
            h.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));

        var isJson = (contentType != null && contentType.Value.Contains("json", StringComparison.OrdinalIgnoreCase)) ||
                     (contentType == null && expected.Body.TrimStart().StartsWith("{"));

        if (isJson)
        {
            // JSON response - use subset matching and token extraction
            var expectedBodyWithSubstitution = SubstituteVariables(expected.Body);
            var captured = lastBody!.AsJsonString().Should().ContainSubset(expectedBodyWithSubstitution);
            variables = variables.Concat(captured.ExtractedValues)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value);
        }
        else
        {
            // Non-JSON response - do simple text comparison with token extraction
            var actualBody = lastBody!;
            var expectedBody = expected.Body;

            // Extract tokens from expected body
            var tokenPattern = @"\[\[([A-Z_]+)\]\]";
            var matches = Regex.Matches(expectedBody, tokenPattern);

            foreach (Match match in matches)
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
                    variables[tokenName] = JsonDocument.Parse($"\"{extractedValue}\"").RootElement;

                    // Replace token in expected with extracted value for comparison
                    expectedBody = expectedBody.Replace(match.Value, extractedValue);
                }
            }

            // Now compare the bodies
            actualBody.Should().Be(expectedBody);
        }
    }

    [Then("the variable '(.*)' is equals to '(.*)'")]
    [Then("""
          the variable "(.*)" is equals to "(.*)"
          """)]
    public void ThenTheVariableIsEqualsTo(string name, string value)
    {
        if (!variables.TryGetValue(name, out var variable))
            throw new VariableNotFoundException(name, variables.Keys.ToList());

        variable.ToString().Should().Be(value);
    }

    [Then(@"the variable '(.*)' equals '(.*)'")]
    [Then("""
          the variable "(.*)" equals "(.*)"
          """)]
    public void ThenTheVariableEqualsString(string name, string value)
    {
        if (!variables.TryGetValue(name, out JsonElement element))
            throw new VariableNotFoundException(name, variables.Keys.ToList());
        element.GetString().Should().Be(value);
    }

    [Then(@"the variable '(.*)' is of type '(.*)'")]
    [Then("""
          the variable "(.*)" is of type "(.*)"
          """)]
    public void ThenTheVariableIsOfType(string name, string type)
    {
        if (!variables.TryGetValue(name, out var variable))
            throw new VariableNotFoundException(name, variables.Keys.ToList());

        var kind = variable.ValueKind;
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
                DateTime.TryParse(variables[name].GetString(), out _).Should().BeTrue();
                break;
            default: throw new InvalidOperationException($"Unknown type '{type}'");
        }
    }

    [Then(@"the variable '(.*)' matches '(.*)'")]
    [Then("""
          the variable "(.*)" matches "(.*)"
          """)]
    public void ThenTheVariableMatches(string name, string regex)
    {
        if (!variables.TryGetValue(name, out var variable))
            throw new VariableNotFoundException(name, variables.Keys.ToList());

        variable.ToString().Should().MatchRegex(regex);
    }

    [Then(@"the variable '(.*)' equals (-?\d+)$")]
    [Then("""the variable "(.*)" equals (-?\d+)$""")]
    public void ThenTheVariableEqualsInt(string name, int expected)
    {
        if (!variables.ContainsKey(name))
            throw new VariableNotFoundException(name, variables.Keys.ToList());

        variables[name].GetInt32().Should().Be(expected);
    }

    [Then(@"the variable '(.*)' equals (-?\d+\.\d+)$")]
    [Then("""the variable "(.*)" equals (-?\d+\.\d+)$""")]
    public void ThenTheVariableEqualsDouble(string name, double expected)
    {
        if (!variables.TryGetValue(name, out var value))
            throw new VariableNotFoundException(name, variables.Keys.ToList());
        value.GetDouble().Should().BeApproximately(expected, 0.0001);
    }

    [Then(@"the variable '(.*)' equals (-?[\d.]+) with delta ([\d.]+)")]
    [Then("""the variable "(.*)" equals (-?[\d.]+) with delta ([\d.]+)""")]
    public void ThenTheVariableEqualsWithDelta(string name, double expected, double delta)
    {
        if (!variables.TryGetValue(name, out var value))
            throw new VariableNotFoundException(name, variables.Keys.ToList());
        value.GetDouble().Should().BeApproximately(expected, delta);
    }

    [Then(@"the variable '(.*)' equals the variable '(.*)'")]
    [Then("""
          the variable "(.*)" equals the variable "(.*)"
          """)]
    public void ThenTheVariableEqualsTheVariable(string name1, string name2)
    {
        if (!variables.TryGetValue(name1, out var value))
            throw new VariableNotFoundException(name1, variables.Keys.ToList());
        if (!variables.TryGetValue(name2, out var variable))
            throw new VariableNotFoundException(name2, variables.Keys.ToList());
        value.ToString().Should().Be(variable.ToString());
    }

    [Then(@"the variable '(.*)' equals (true|false)")]
    [Then("""the variable "(.*)" equals (true|false)""")]
    public void ThenTheVariableEqualsTrueOrFalse(string name, bool expected)
    {
        if (!variables.TryGetValue(name, out var value))
            throw new VariableNotFoundException(name, variables.Keys.ToList());
        value.GetBoolean().Should().Be(expected);
    }

    private string SubstituteVariables(string text)
    {
        // First substitute {{TOKEN}} patterns (variable substitution)
        foreach (var kvp in variables)
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

        // Then substitute [[TOKEN]] patterns (token extraction patterns)
        foreach (var kvp in variables)
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
            text = text.Replace("[[" + kvp.Key + "]]", value);
        }

        return text;
    }

    private string ExecuteFunctions(string text)
    {
        var functionPattern = @"\{\{([A-Z_]+)\(\)\}\}";
        return Regex.Replace(text, functionPattern, match =>
        {
            var functionName = match.Groups[1].Value;
            return functionRegistry.ExecuteFunction(functionName);
        });
    }

    private void ValidateHeaders(IEnumerable<ParsedHeader> expectedHeaders)
    {
        var allHeaders = lastResponse!.Headers.Concat(lastResponse.Content.Headers)
            .Select(h => h.Key).ToList();

        // Build actual headers for extraction
        var actualHeaders = new List<ParsedHeader>();
        foreach (var header in lastResponse.Headers.Concat(lastResponse.Content.Headers))
        {
            var headerValue = string.Join(", ", header.Value);
            var parsed = HttpHeadersParser.ParseHeader($"{header.Key}: {headerValue}");
            actualHeaders.Add(parsed);
        }

        // Extract variables from headers FIRST
        var parsedHeaders = expectedHeaders as ParsedHeader[] ?? expectedHeaders.ToArray();
        var extractedVariables = HeaderVariableExtractor.ExtractVariables(parsedHeaders, actualHeaders);
        foreach (var kvp in extractedVariables)
        {
            variables[kvp.Key] = JsonDocument.Parse($"\"{kvp.Value}\"").RootElement;
        }

        // Now validate headers with substituted values
        foreach (var expected in parsedHeaders)
        {
            if (!lastResponse.Headers.TryGetValues(expected.Name, out var values) &&
                !lastResponse.Content.Headers.TryGetValues(expected.Name, out values))
                throw new HeaderValidationException(expected.Name, allHeaders, "Header is missing");

            var actual = HttpHeadersParser.ParseHeader($"{expected.Name}: {values.First()}");

            // Substitute variables in expected value for comparison
            var expectedValue = SubstituteVariables(expected.Value);

            if (actual.Value != expectedValue)
                throw new HeaderValidationException(expected.Name, allHeaders,
                    $"Expected value '{expectedValue}' but got '{actual.Value}'");

            if (expected.Parameters.Count == 0) continue;

            foreach (var expectedParam in expected.Parameters)
            {
                if (!actual.Parameters.ContainsKey(expectedParam.Key))
                    throw new HeaderValidationException(expected.Name, allHeaders,
                        $"Parameter '{expectedParam.Key}' is missing");

                var expectedParamValue = SubstituteVariables(expectedParam.Value);
                if (actual.Parameters[expectedParam.Key] != expectedParamValue)
                    throw new HeaderValidationException(expected.Name, allHeaders,
                        $"Parameter '{expectedParam.Key}' expected '{expectedParamValue}' but got '{actual.Parameters[expectedParam.Key]}'");
            }
        }
    }
}
