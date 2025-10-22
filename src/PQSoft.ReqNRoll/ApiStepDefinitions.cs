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

        var captured = _lastBody!.AsJsonString().Should().ContainSubset(expected.Body);
        _variables = _variables.Concat(captured.ExtractedValues)
            .GroupBy(kvp => kvp.Key)
            .ToDictionary(g => g.Key, g => g.Last().Value);
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
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
