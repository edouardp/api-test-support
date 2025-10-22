using System.Text.RegularExpressions;

namespace PQSoft.HttpFile;

/// <summary>
/// Extracts variables from HTTP headers using token patterns like [[VAR_NAME]].
/// </summary>
public static class HeaderVariableExtractor
{
    private static readonly Regex TokenPattern = new(@"\[\[([A-Z_]+)\]\]", RegexOptions.Compiled);

    /// <summary>
    /// Extracts variables from headers by matching expected patterns against actual values.
    /// </summary>
    /// <param name="expectedHeaders">Headers with token patterns like [[VAR_NAME]].</param>
    /// <param name="actualHeaders">Actual headers with real values.</param>
    /// <returns>Dictionary of extracted variable names and values.</returns>
    public static Dictionary<string, string> ExtractVariables(
        IEnumerable<ParsedHeader> expectedHeaders,
        IEnumerable<ParsedHeader> actualHeaders)
    {
        var variables = new Dictionary<string, string>();
        var actualHeadersDict = actualHeaders.ToDictionary(h => h.Name, h => h, StringComparer.OrdinalIgnoreCase);

        foreach (var expected in expectedHeaders)
        {
            if (!actualHeadersDict.TryGetValue(expected.Name, out var actual))
                continue;

            // Extract from header value
            ExtractFromValue(expected.Value, actual.Value, variables);

            // Extract from header parameters
            foreach (var expectedParam in expected.Parameters)
            {
                if (actual.Parameters.TryGetValue(expectedParam.Key, out var actualParamValue))
                {
                    ExtractFromValue(expectedParam.Value, actualParamValue, variables);
                }
            }
        }

        return variables;
    }

    private static void ExtractFromValue(string expectedValue, string actualValue, Dictionary<string, string> variables)
    {
        var matches = TokenPattern.Matches(expectedValue);
        if (matches.Count == 0) return;

        // Build regex pattern by replacing tokens with capture groups
        var pattern = expectedValue;
        var tokenNames = new List<string>();
        
        foreach (Match match in matches)
        {
            tokenNames.Add(match.Groups[1].Value);
        }
        
        // Replace tokens with capture groups from right to left to preserve indices
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var match = matches[i];
            
            // Determine if this is the last token (use greedy) or not (use non-greedy)
            var isLastToken = i == matches.Count - 1;
            var captureGroup = isLastToken ? "(.+)" : "(.+?)";
            
            pattern = pattern.Substring(0, match.Index) + captureGroup + pattern.Substring(match.Index + match.Length);
        }
        
        // Escape the pattern except for our capture groups
        var escapedPattern = Regex.Escape(pattern);
        // Restore capture groups
        escapedPattern = escapedPattern.Replace(@"\(\.\+\?\)", "(.+?)").Replace(@"\(\.\+\)", "(.+)");
        
        var extractionMatch = Regex.Match(actualValue, escapedPattern);
        
        if (extractionMatch.Success && extractionMatch.Groups.Count > tokenNames.Count)
        {
            for (int i = 0; i < tokenNames.Count; i++)
            {
                variables[tokenNames[i]] = extractionMatch.Groups[i + 1].Value;
            }
        }
    }
}
