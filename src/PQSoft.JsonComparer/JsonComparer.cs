using System.Text.Json;
using System.Text.RegularExpressions;

namespace TestSupport;

/// <summary>
/// Provides functionality to compare JSON strings with support for tokenized placeholders and function execution.
/// This class includes methods to perform exact and subset comparisons between JSON structures,
/// allowing specific values to be extracted dynamically via tokens and functions to be executed.
///
/// Token Types:
/// - Extraction tokens: [[TOKEN_NAME]] - Values are extracted from actual JSON
/// - Function tokens: {{FUNCTION_NAME()}} - Functions are executed and replaced with results
/// - Variable tokens: {{VARIABLE_NAME}} - Variables are substituted from provided context
///
/// Features:
/// - Exact match comparison: Ensures the expected and actual JSON structures are identical, 
///   except for tokenized values.
/// - Subset match comparison: Verifies that all elements in the expected JSON exist within 
///   the actual JSON, without requiring a full match.
/// - Token extraction: Captures values corresponding to tokens in the expected JSON for further processing.
/// - Function execution: Executes functions like {{GUID()}}, {{NOW()}}, {{UTCNOW()}} during preprocessing.
/// - Detailed mismatch reporting: Provides structured information on any differences found.
///
/// Example Usage:
/// <code>
///   string expectedJson = """{ "id": "[[JOBID]]", "createdAt": "{{NOW()}}", "status": "complete" }""";
///   string actualJson = """{ "id": "12345", "createdAt": "2024-01-01T10:00:00.000+00:00", "status": "complete" }""";
///
///   bool isMatch = JsonComparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);
///
///   Console.WriteLine(isMatch); // True
///   Console.WriteLine(extractedValues["JOBID"]); // 12345
/// </code>
///
/// </summary>
public static partial class JsonComparer
{
    // Regex to match boxed tokens in expected JSON (e.g. "[[JOBID]]")
    [GeneratedRegex(@"^\[\[(\w+)\]\]$", RegexOptions.Compiled)]
    private static partial Regex TokenRegexGenerator();
    private static readonly Regex TokenRegex = TokenRegexGenerator();  

    // Regex to find tokens that are not already enclosed in quotes.
    // This regex looks for the pattern [[VARIABLE]] that is not immediately preceded or followed by a double quote.
    [GeneratedRegex("(?<!\\\")\\[\\[(\\w+)\\]\\](?!\\\")", RegexOptions.Compiled)]
    private static partial Regex UnquotedTokenRegexGenerator();
    private static readonly Regex UnquotedTokenRegex = UnquotedTokenRegexGenerator();

    // Regex to match function calls in expected JSON (e.g. "{{GUID()}}")
    [GeneratedRegex(@"\{\{(\w+)\(\)\}\}", RegexOptions.Compiled)]
    private static partial Regex FunctionRegexGenerator();
    private static readonly Regex FunctionRegex = FunctionRegexGenerator();

    // Function registry for executing functions during preprocessing
    private static readonly JsonFunctionRegistry FunctionRegistry = new();

    /// <summary>
    /// Compares the two JSON strings for an exact match.
    /// Returns true if they match exactly (except for tokens), false otherwise.
    /// Also extracts any token values (e.g. JOBID) into extractedValues and records mismatch details.
    /// </summary>
    public static bool ExactMatch(string expectedJson, string actualJson,
        out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches)
    {
        return Compare(expectedJson, actualJson, subsetMode: false, out extractedValues, out mismatches);
    }

    /// <summary>
    /// Compares the two JSON strings for an exact match using a custom TimeProvider for time-based functions.
    /// Returns true if they match exactly (except for tokens), false otherwise.
    /// Also extracts any token values (e.g. JOBID) into extractedValues and records mismatch details.
    /// </summary>
    public static bool ExactMatch(string expectedJson, string actualJson, TimeProvider timeProvider,
        out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches)
    {
        return Compare(expectedJson, actualJson, subsetMode: false, timeProvider, out extractedValues, out mismatches);
    }

    /// <summary>
    /// Compares the two JSON strings for a subset match (i.e. expected is a subset of actual).
    /// Returns true if all elements in expected (except tokens) are found in actual.
    /// Also extracts any token values into extractedValues and records mismatch details.
    /// </summary>
    public static bool SubsetMatch(string expectedJson, string actualJson,
        out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches)
    {
        return Compare(expectedJson, actualJson, subsetMode: true, out extractedValues, out mismatches);
    }

    /// <summary>
    /// Compares the two JSON strings for a subset match using a custom TimeProvider for time-based functions.
    /// Returns true if all elements in expected (except tokens) are found in actual.
    /// Also extracts any token values into extractedValues and records mismatch details.
    /// </summary>
    public static bool SubsetMatch(string expectedJson, string actualJson, TimeProvider timeProvider,
        out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches)
    {
        return Compare(expectedJson, actualJson, subsetMode: true, timeProvider, out extractedValues, out mismatches);
    }

    /// <summary>
    /// Parses the JSON strings and calls the recursive CompareElements function.
    /// It pre-processes the expected JSON string by executing functions and wrapping unquoted tokens with quotes.
    /// </summary>
    private static bool Compare(string expectedJson, string actualJson, bool subsetMode,
        out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches)
    {
        return Compare(expectedJson, actualJson, subsetMode, null, out extractedValues, out mismatches);
    }

    /// <summary>
    /// Parses the JSON strings and calls the recursive CompareElements function.
    /// It pre-processes the expected JSON string by executing functions and wrapping unquoted tokens with quotes.
    /// </summary>
    private static bool Compare(string expectedJson, string actualJson, bool subsetMode, TimeProvider? timeProvider,
        out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches)
    {
        extractedValues = [];
        mismatches = [];

        // Step 1: Execute functions in the expected JSON (e.g., {{GUID()}} -> actual GUID)
        expectedJson = ProcessFunctions(expectedJson, timeProvider);

        // Step 2: Pre-process the expected JSON to ensure any token of the form {{VARIABLE}}
        // is wrapped in double quotes if not already.
        expectedJson = UnquotedTokenRegex.Replace(expectedJson, "\"[[$1]]\"");

        using JsonDocument expectedDoc = JsonDocument.Parse(expectedJson);
        using JsonDocument actualDoc = JsonDocument.Parse(actualJson);

        CompareElements(expectedDoc.RootElement, actualDoc.RootElement, "$", subsetMode, extractedValues, mismatches);

        return mismatches.Count == 0;
    }

    /// <summary>
    /// Recursively compares expected and actual JsonElements.
    /// When a moustache token is encountered in expected (e.g. "{{JOBID}}"),
    /// the actual value is extracted into extractedValues and no further comparison is done at that node.
    /// </summary>
    private static void CompareElements(JsonElement expected, JsonElement actual, string jsonPath, bool subsetMode,
        Dictionary<string, JsonElement> extractedValues, List<string> mismatches)
    {
        // Check for token match when expected is a string
        if (expected.ValueKind == JsonValueKind.String)
        {
            string? expectedStr = expected.GetString();
            if (!string.IsNullOrEmpty(expectedStr))
            {
                var match = TokenRegex.Match(expectedStr);
                if (match.Success)
                {
                    // It's a token, extract the actual value (of any type) and return.
                    string tokenName = match.Groups[1].Value;
                    extractedValues[tokenName] = actual.Clone();
                    return;
                }
            }
        }

        // Check that both nodes are of the same JSON type.
        if (expected.ValueKind != actual.ValueKind)
        {
            if ((expected.ValueKind == JsonValueKind.True && actual.ValueKind == JsonValueKind.False) ||
                (expected.ValueKind == JsonValueKind.False && actual.ValueKind == JsonValueKind.True))
            {
                mismatches.Add($"{jsonPath}: Boolean mismatch. Expected {expected.GetBoolean()}, got {actual.GetBoolean()}.");
            }
            else
            {
                mismatches.Add($"{jsonPath}: Type mismatch. Expected {expected.ValueKind}, got {actual.ValueKind}.");
            }
            return;
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                // For objects, each expected property must exist in actual.
                foreach (JsonProperty prop in expected.EnumerateObject())
                {
                    if (!actual.TryGetProperty(prop.Name, out JsonElement actualProp))
                    {
                        mismatches.Add($"{jsonPath}: Missing property '{prop.Name}'.");
                    }
                    else
                    {
                        CompareElements(prop.Value, actualProp, $"{jsonPath}.{prop.Name}", subsetMode, extractedValues, mismatches);
                    }
                }
                // For an exact match, check that actual does not have extra properties.
                if (!subsetMode)
                {
                    foreach (JsonProperty prop in actual.EnumerateObject())
                    {
                        if (!expected.TryGetProperty(prop.Name, out _))
                        {
                            mismatches.Add($"{jsonPath}: Extra property '{prop.Name}' found in actual JSON.");
                        }
                    }
                }
                break;

            case JsonValueKind.Array:
                // For arrays, the expected array must be a prefix of the actual array in subset mode
                // or exactly equal in length for an exact match.
                
                //JsonElement.ArrayEnumerator expectedEnum = expected.EnumerateArray();
                //JsonElement.ArrayEnumerator actualEnum = actual.EnumerateArray();

                List<JsonElement> expectedList = new(expected.EnumerateArray());
                List<JsonElement> actualList = new(actual.EnumerateArray());

                if (!subsetMode && expectedList.Count != actualList.Count)
                {
                    mismatches.Add($"{jsonPath}: Array length mismatch. Expected {expectedList.Count}, got {actualList.Count}.");
                    return;
                }
                if (subsetMode && expectedList.Count > actualList.Count)
                {
                    mismatches.Add($"{jsonPath}: Array length mismatch in subset mode. Expected array with at most {actualList.Count} elements, but expected has {expectedList.Count} elements.");
                    return;
                }
                // Compare each element in expected.
                for (int i = 0; i < expectedList.Count; i++)
                {
                    CompareElements(expectedList[i], actualList[i], $"{jsonPath}[{i}]", subsetMode, extractedValues, mismatches);
                }
                break;

            case JsonValueKind.String:
                // Already handled token case above.
                string? expectedValue = expected.GetString();
                string? actualValue = actual.GetString();
                if (expectedValue != actualValue)
                {
                    mismatches.Add($"{jsonPath}: String mismatch. Expected \"{expectedValue}\", got \"{actualValue}\".");
                }
                break;

            case JsonValueKind.Number:
                // Compare using raw text for numbers
                if (expected.GetRawText() != actual.GetRawText())
                {
                    mismatches.Add($"{jsonPath}: Number mismatch. Expected {expected.GetRawText()}, got {actual.GetRawText()}.");
                }
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                // Both values are the same (either both True or both False)
                break;

            case JsonValueKind.Null:
                // Both are null, so they match.
                break;

            default:
                mismatches.Add($"{jsonPath}: Unsupported JSON value kind: {expected.ValueKind}.");
                break;
        }
    }

    /// <summary>
    /// Processes function calls in the JSON string by executing them and replacing with results.
    /// Functions are identified by the pattern {{FUNCTION_NAME()}} and are executed using the function registry.
    /// </summary>
    /// <param name="json">The JSON string containing function calls to process.</param>
    /// <param name="timeProvider">Optional TimeProvider for time-based functions.</param>
    /// <returns>The JSON string with function calls replaced by their execution results.</returns>
    private static string ProcessFunctions(string json, TimeProvider? timeProvider = null)
    {
        var registry = timeProvider != null ? new JsonFunctionRegistry(timeProvider) : FunctionRegistry;
        
        return FunctionRegex.Replace(json, match =>
        {
            string functionName = match.Groups[1].Value;
            try
            {
                string result = registry.ExecuteFunction(functionName);
                // Return the raw result since the function call is already within quotes in the JSON
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to execute function '{functionName}': {ex.Message}", ex);
            }
        });
    }

    /// <summary>
    /// Registers a custom function that can be used in JSON comparisons.
    /// This allows extending the built-in function set with custom implementations.
    /// </summary>
    /// <param name="name">The name of the function (without parentheses).</param>
    /// <param name="function">The function implementation.</param>
    /// <exception cref="ArgumentException">Thrown when name is invalid or already registered.</exception>
    public static void RegisterFunction(string name, IJsonFunction function)
    {
        FunctionRegistry.RegisterFunction(name, function);
    }

    /// <summary>
    /// Gets the names of all registered functions.
    /// </summary>
    /// <returns>An array of registered function names.</returns>
    public static string[] GetRegisteredFunctions()
    {
        return FunctionRegistry.GetRegisteredFunctions();
    }
}

