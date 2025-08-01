using System.Text.Json;
using PQSoft.JsonComparer;

Console.WriteLine("PQSoft.JsonComparer Sample Application");
Console.WriteLine("=====================================");
Console.WriteLine();

// Example 1: Compare identical JSON objects
Console.WriteLine("Example 1: Compare identical JSON objects");
string json1 = """{"name":"John","age":30,"city":"New York"}""";
string json2 = """{"name":"John","age":30,"city":"New York"}""";

bool result1 = JsonComparer.ExactMatch(json1, json2, out var extractedValues1, out var mismatches1);
PrintResult(result1, extractedValues1, mismatches1);

// Example 2: Compare JSON objects with different values
Console.WriteLine("\nExample 2: Compare JSON objects with different values");
string json3 = """{"name":"John","age":30,"city":"New York"}""";
string json4 = """{"name":"John","age":31,"city":"New York"}""";

bool result2 = JsonComparer.ExactMatch(json3, json4, out var extractedValues2, out var mismatches2);
PrintResult(result2, extractedValues2, mismatches2);

// Example 3: Compare JSON with tokens
Console.WriteLine("\nExample 3: Compare JSON with tokens");
string json5 = """{"id":"[[JOB_ID]]","name":"John","age":30}""";
string json6 = """{"id":"12345","name":"John","age":30}""";

bool result3 = JsonComparer.ExactMatch(json5, json6, out var extractedValues3, out var mismatches3);
PrintResult(result3, extractedValues3, mismatches3);
if (extractedValues3.TryGetValue("JOB_ID", out var jobId))
{
    Console.WriteLine($"Extracted JOB_ID: {jobId.GetRawText()}");
}
else
{
    Console.WriteLine("No JOB_ID extracted");
}

// Example 4: Subset match
Console.WriteLine("\nExample 4: Subset match");
string json7 = """{"name":"John"}""";
string json8 = """{"name":"John","age":30,"city":"New York"}""";

bool result4 = JsonComparer.SubsetMatch(json7, json8, out var extractedValues4, out var mismatches4);
PrintResult(result4, extractedValues4, mismatches4);

// Example 5: Using functions
Console.WriteLine("\nExample 5: Using functions");
string json9 = """{"timestamp":"{{NOW()}}","status":"active"}""";
string json10 = """{"timestamp":"2024-01-01T10:00:00.000+00:00","status":"active"}""";

bool result5 = JsonComparer.ExactMatch(json9, json10, out var extractedValues5, out var mismatches5);
PrintResult(result5, extractedValues5, mismatches5);

void PrintResult(bool areEqual, Dictionary<string, JsonElement> extractedValues, List<string> mismatches)
{
    if (areEqual)
    {
        Console.WriteLine("The JSON documents match.");
    }
    else
    {
        Console.WriteLine("The JSON documents do not match:");
        foreach (var mismatch in mismatches)
        {
            Console.WriteLine($"  {mismatch}");
        }
    }
}