using PQSoft.JsonComparer;

Console.WriteLine("PQSoft.JsonComparer Sample Application");
Console.WriteLine("=====================================");
Console.WriteLine();

// Create a comparer with default options
var defaultComparer = new JsonComparer();

// Create a comparer with custom options
var customOptions = new JsonComparerOptions
{
    IgnoreCase = true,
    IgnoreArrayOrder = true,
    ExcludePaths = new List<string> { "$.metadata" }
};
var customComparer = new JsonComparer(customOptions);

// Example 1: Compare identical JSON objects
Console.WriteLine("Example 1: Compare identical JSON objects");
string json1 = @"{""name"":""John"",""age"":30,""city"":""New York""}";
string json2 = @"{""name"":""John"",""age"":30,""city"":""New York""}";

var result1 = defaultComparer.Compare(json1, json2);
PrintResult(result1);

// Example 2: Compare JSON objects with different values
Console.WriteLine("\nExample 2: Compare JSON objects with different values");
string json3 = @"{""name"":""John"",""age"":30,""city"":""New York""}";
string json4 = @"{""name"":""John"",""age"":31,""city"":""New York""}";

var result2 = defaultComparer.Compare(json3, json4);
PrintResult(result2);

// Example 3: Compare JSON objects with case differences using case-insensitive comparison
Console.WriteLine("\nExample 3: Compare JSON objects with case differences using case-insensitive comparison");
string json5 = @"{""name"":""John"",""city"":""New York""}";
string json6 = @"{""name"":""JOHN"",""city"":""New York""}";

var result3 = defaultComparer.Compare(json5, json6);
Console.WriteLine("Default comparer (case-sensitive):");
PrintResult(result3);

var result4 = customComparer.Compare(json5, json6);
Console.WriteLine("Custom comparer (case-insensitive):");
PrintResult(result4);

// Example 4: Compare JSON arrays with different order
Console.WriteLine("\nExample 4: Compare JSON arrays with different order");
string json7 = @"{""colors"":[""red"",""green"",""blue""]}";
string json8 = @"{""colors"":[""blue"",""red"",""green""]}";

var result5 = defaultComparer.Compare(json7, json8);
Console.WriteLine("Default comparer (respect array order):");
PrintResult(result5);

var result6 = customComparer.Compare(json7, json8);
Console.WriteLine("Custom comparer (ignore array order):");
PrintResult(result6);

// Example 5: Compare JSON with excluded paths
Console.WriteLine("\nExample 5: Compare JSON with excluded paths");
string json9 = @"{""name"":""John"",""metadata"":{""lastUpdated"":""2025-07-22""}}";
string json10 = @"{""name"":""John"",""metadata"":{""lastUpdated"":""2025-07-23""}}";

var result7 = defaultComparer.Compare(json9, json10);
Console.WriteLine("Default comparer (include all paths):");
PrintResult(result7);

var result8 = customComparer.Compare(json9, json10);
Console.WriteLine("Custom comparer (exclude $.metadata path):");
PrintResult(result8);

void PrintResult(ComparisonResult result)
{
    if (result.AreEqual)
    {
        Console.WriteLine("The JSON documents are equal.");
    }
    else
    {
        Console.WriteLine("The JSON documents are different:");
        foreach (var diff in result.Differences)
        {
            Console.WriteLine($"  Path: {diff.Path}");
            Console.WriteLine($"  Value 1: {diff.Value1}");
            Console.WriteLine($"  Value 2: {diff.Value2}");
            Console.WriteLine($"  Difference Type: {diff.DifferenceType}");
            Console.WriteLine();
        }
    }
}
