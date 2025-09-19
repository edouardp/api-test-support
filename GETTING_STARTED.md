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

// Create a comparer instance
var comparer = new JsonComparer();

// Compare two JSON strings for exact match
string expectedJson = @"{""name"":""John"",""age"":30}";
string actualJson = @"{""name"":""John"",""age"":31}";

bool isMatch = comparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);

if (isMatch)
{
    Console.WriteLine("The JSON documents match exactly.");
}
else
{
    Console.WriteLine("The JSON documents are different:");
    foreach (var mismatch in mismatches)
    {
        Console.WriteLine(mismatch);
    }
}

// You can also use static methods
bool staticMatch = JsonComparer.ExactMatch(expectedJson, actualJson, out var values, out var errors);
```

## For Contributors

### Setting Up the Development Environment

1. Clone the repository:
   ```bash
   git clone https://github.com/edouardp/api-test-support.git
   cd api-test-support
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
- `src/PQSoft.JsonComparer.UnitTests/`: Contains unit tests
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

1. Add support for more complex array comparison strategies
2. Improve performance for large JSON documents
3. Add support for JSON Schema validation
4. Add support for JSON Patch generation
