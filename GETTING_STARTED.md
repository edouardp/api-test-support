# Getting Started with PQSoft.JsonComparer

This document provides instructions for getting started with the PQSoft.JsonComparer library, both as a user and as a contributor.

## For Users

### Installation

Once the package is published to NuGet, you can install it using the following command:

```bash
dotnet add package PQSoft.JsonComparer
```

### Basic Usage

```csharp
using PQSoft.JsonComparer;

// Create a comparer with default options
var comparer = new JsonComparer();

// Compare two JSON strings
string json1 = @"{""name"":""John"",""age"":30}";
string json2 = @"{""name"":""John"",""age"":31}";

var result = comparer.Compare(json1, json2);

if (result.AreEqual)
{
    Console.WriteLine("The JSON documents are equal.");
}
else
{
    Console.WriteLine("The JSON documents are different:");
    foreach (var diff in result.Differences)
    {
        Console.WriteLine($"Path: {diff.Path}");
        Console.WriteLine($"Value 1: {diff.Value1}");
        Console.WriteLine($"Value 2: {diff.Value2}");
        Console.WriteLine($"Difference Type: {diff.DifferenceType}");
    }
}
```

### Advanced Usage

You can customize the comparison behavior using `JsonComparerOptions`:

```csharp
var options = new JsonComparerOptions
{
    IgnoreCase = true,
    IgnoreArrayOrder = true,
    ExcludePaths = new List<string> { "$.metadata" }
};
var customComparer = new JsonComparer(options);
```

## For Contributors

### Setting Up the Development Environment

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/PQSoft.JsonComparer.git
   cd PQSoft.JsonComparer
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

4. Run tests:
   ```bash
   dotnet test
   ```

### Project Structure

- `src/PQSoft.JsonComparer/`: Contains the main library code
- `tests/PQSoft.JsonComparer.Tests/`: Contains unit tests
- `samples/PQSoft.JsonComparer.Sample/`: Contains a sample application

### Making Changes

1. Create a new branch for your feature or bug fix
2. Make your changes
3. Add or update tests as necessary
4. Ensure all tests pass
5. Submit a pull request

## Publishing to NuGet

To publish the package to NuGet:

1. Create a NuGet API key at https://www.nuget.org/
2. Add the API key as a GitHub secret named `NUGET_API_KEY`
3. Create a GitHub release to trigger the publish workflow

## Known Issues

- There are currently warnings about vulnerabilities in the System.Text.Json package. These are known issues that will be addressed in future updates.

## Next Steps

1. Complete the implementation of the `IgnoreWhitespace` option
2. Add support for more complex array comparison strategies
3. Improve performance for large JSON documents
4. Add support for JSON Schema validation
5. Add support for JSON Patch generation
