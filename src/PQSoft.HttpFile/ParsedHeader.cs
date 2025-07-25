using System.Text;

namespace TestSupport.HttpFile;

/// <summary>
/// Represents a parsed HTTP header, including its name, value, and any associated parameters.
/// </summary>
public class ParsedHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParsedHeader"/> class.
    /// </summary>
    /// <param name="name">The name of the header.</param>
    /// <param name="value">The value of the header.</param>
    /// <param name="parameters">A dictionary of additional parameters associated with the header.</param>
    public ParsedHeader(string name, string value, Dictionary<string, string> parameters)
    {
        Name = name;
        Value = value;
        Parameters = parameters ?? [];
    }

    /// <summary>
    /// Gets the name of the header.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the value of the header.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the dictionary of parameters associated with the header.
    /// </summary>
    public Dictionary<string, string> Parameters { get; }

    /// <summary>
    /// Returns a string representation of the parsed header, including its name, value, and parameters.
    /// </summary>
    /// <returns>A formatted string representation of the header.</returns>
    public override string ToString()
    {
        // Converts the header and its parameters into a string format.
        // Example output: "Content-Type: text/html; charset=UTF-8"
        return Parameters.Aggregate(
            new StringBuilder($"{Name}: {Value}"),
            (builder, kvp) => builder.Append($"; {kvp.Key}={kvp.Value}")
        ).ToString();
    }

    /// <summary>
    /// Compares this header with another header semantically, ignoring parameter order, 
    /// whitespace differences, and case sensitivity in header names.
    /// </summary>
    /// <param name="other">The other header to compare with.</param>
    /// <returns>True if the headers are semantically equivalent; otherwise, false.</returns>
    public bool SemanticEquals(ParsedHeader? other)
    {
        if (other == null) return false;
        
        // Compare header names (case-insensitive)
        if (!string.Equals(Name.Trim(), other.Name.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;
        
        // Compare header values (case-sensitive, but trimmed)
        if (!string.Equals(Value.Trim(), other.Value.Trim(), StringComparison.Ordinal))
            return false;
        
        // Compare parameters (ignoring order)
        if (Parameters.Count != other.Parameters.Count)
            return false;
        
        foreach (var kvp in Parameters)
        {
            if (!other.Parameters.TryGetValue(kvp.Key, out string? otherValue))
                return false;
            
            if (!string.Equals(kvp.Value.Trim(), otherValue.Trim(), StringComparison.Ordinal))
                return false;
        }
        
        return true;
    }

    /// <summary>
    /// Compares a collection of headers semantically with another collection, 
    /// ignoring order and using semantic comparison for individual headers.
    /// </summary>
    /// <param name="headers1">First collection of headers.</param>
    /// <param name="headers2">Second collection of headers.</param>
    /// <returns>True if both collections contain semantically equivalent headers; otherwise, false.</returns>
    public static bool SemanticEquals(IEnumerable<ParsedHeader> headers1, IEnumerable<ParsedHeader> headers2)
    {
        var list1 = headers1.ToList();
        var list2 = headers2.ToList();
        
        if (list1.Count != list2.Count)
            return false;
        
        // For each header in list1, find a semantically matching header in list2
        foreach (var header1 in list1)
        {
            if (!list2.Any(header2 => header1.SemanticEquals(header2)))
                return false;
        }
        
        return true;
    }
}
