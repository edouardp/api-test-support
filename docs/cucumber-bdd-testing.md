# Cucumber/Reqnroll BDD Testing with PQSoft Libraries

## Overview

Behavior-Driven Development (BDD) using Cucumber/Reqnroll (formerly SpecFlow)
provides a way to write tests in natural language (Gherkin) that are executable
and maintainable. The PQSoft libraries integrate seamlessly with BDD testing to
provide powerful API testing capabilities.

This guide shows how to:

- Write readable Gherkin scenarios for API testing
- Use token extraction (`[[TOKEN]]`) to capture dynamic values
- Reference captured values in subsequent requests (`{{TOKEN}}`)
- Implement step definitions using PQSoft libraries
- Test complete CRUD workflows with multi-step scenarios

## Why Use PQSoft with Cucumber/Reqnroll?

### **Traditional BDD API Testing Challenges**

Without proper tooling, BDD API tests suffer from:

1. **Brittle assertions**: Hard-coded IDs, timestamps, and headers break easily
2. **Verbose step definitions**: Manual JSON parsing and comparison in every
   step
3. **Poor multi-step support**: Difficult to pass data between steps
4. **Header complexity**: Hard to validate HTTP headers semantically
5. **Limited reusability**: Step definitions become test-specific

### **PQSoft Solutions**

| Challenge                        | PQSoft Solution                              |
| -------------------------------- | -------------------------------------------- |
| Dynamic values (IDs, timestamps) | Token extraction: `[[JOBID]]`                |
| Value reuse across steps         | Token substitution: `{{JOBID}}`              |
| JSON comparison                  | Semantic comparison with `JsonComparer`      |
| Header validation                | Semantic header matching with `ParsedHeader` |
| HTTP parsing                     | Direct `.http` format support in Gherkin     |

## Quick Start Example

Here's a complete BDD scenario showing token extraction and reuse:

```gherkin
Feature: Job Management API

  Scenario: Create and retrieve a job

    # Step 1: Create a job and extract the JobID
    Given the following request
    """
    POST /api/job HTTP/1.1
    Content-Type: application/json; charset=utf-8
    Accept: application/json

    {
        "JobType": "Upgrade"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json; charset=utf-8

    {
        "jobId": [[JOBID]]
    }
    """

    # Step 2: Use the captured JobID to get job status
    Given the following request
    """
    GET /api/job/status/{{JOBID}} HTTP/1.1
    Accept: application/json
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json; charset=utf-8

    {
        "jobId": "{{JOBID}}",
        "status": "Pending"
    }
    """
```

**What's happening:**

1. `[[JOBID]]` extracts the `jobId` value from the response (e.g., `"abc-123"`)
2. `{{JOBID}}` substitutes the captured value into subsequent requests
3. Headers are compared semantically (case-insensitive, parameter order
   independent)
4. JSON is compared structurally (field order doesn't matter)

## Token System

The PQSoft libraries support two token types:

### **1. Extraction Tokens: `[[TOKEN_NAME]]`**

Used in **expected responses** to capture dynamic values:

```gherkin
Then the API returns the following response
"""
HTTP/1.1 201 Created

{
    "jobId": [[JOBID]],
    "traceId": [[TRACE_ID]],
    "createdAt": "{{NOW()}}"
}
"""
```

- `[[JOBID]]` captures the actual `jobId` value from the response
- `[[TRACE_ID]]` captures the `traceId` value
- Empty tokens `[[]]` are discard tokens (validates presence, doesn't store)

### **2. Substitution Tokens: `{{TOKEN_NAME}}`**

Used in **requests and expected responses** to reference captured values:

```gherkin
Given the following request
"""
GET /api/job/{{JOBID}} HTTP/1.1
"""
```

The `{{JOBID}}` is replaced with the actual value captured earlier.

### **3. Function Tokens: `{{FUNCTION()}}`**

Execute functions during comparison:

| Function       | Description                      | Example Output                         |
| -------------- | -------------------------------- | -------------------------------------- |
| `{{NOW()}}`    | Current local time with timezone | `2025-01-15T10:30:45.123+13:00`        |
| `{{UTCNOW()}}` | Current UTC time                 | `2025-01-15T10:30:45.123Z`             |
| `{{GUID()}}`   | Generate a new GUID              | `7a3b2c1d-4e5f-6a7b-8c9d-0e1f2a3b4c5d` |

```gherkin
Then the API returns the following response
"""
{
    "timestamp": "{{NOW()}}",
    "requestId": [[REQUEST_ID]]
}
"""
```

## Complete CRUD Workflow Example

Here's a comprehensive example testing a full lifecycle:

```gherkin
Feature: Complete Job Lifecycle

  Scenario: Create, Read, Update, and Delete a job

    # CREATE
    Given the following request
    """
    POST /api/job HTTP/1.1
    Content-Type: application/json; charset=utf-8

    {
        "JobType": "DataMigration",
        "Priority": "High"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json; charset=utf-8

    {
        "jobId": [[JOBID]],
        "jobType": "DataMigration",
        "priority": "High",
        "status": "Pending",
        "createdAt": "{{NOW()}}"
    }
    """

    # READ (immediately after creation)
    Given the following request
    """
    GET /api/job/{{JOBID}} HTTP/1.1
    Accept: application/json
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json; charset=utf-8

    {
        "jobId": "{{JOBID}}",
        "jobType": "DataMigration",
        "priority": "High",
        "status": "Pending"
    }
    """

    # UPDATE
    Given the following request
    """
    PUT /api/job/{{JOBID}} HTTP/1.1
    Content-Type: application/json; charset=utf-8

    {
        "priority": "Critical",
        "status": "Running"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json; charset=utf-8

    {
        "jobId": "{{JOBID}}",
        "priority": "Critical",
        "status": "Running",
        "updatedAt": "{{NOW()}}"
    }
    """

    # READ (after update)
    Given the following request
    """
    GET /api/job/{{JOBID}} HTTP/1.1
    Accept: application/json
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json; charset=utf-8

    {
        "jobId": "{{JOBID}}",
        "priority": "Critical",
        "status": "Running"
    }
    """

    # DELETE
    Given the following request
    """
    DELETE /api/job/{{JOBID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 No Content
    """

    # READ (after deletion - should fail)
    Given the following request
    """
    GET /api/job/{{JOBID}} HTTP/1.1
    Accept: application/json
    """

    Then the API returns the following response
    """
    HTTP/1.1 404 NotFound
    Content-Type: application/problem+json; charset=utf-8

    {
        "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        "title": "Job Not Found",
        "status": 404,
        "detail": "The job with ID {{JOBID}} was not found."
    }
    """
```

## Step Definition Implementation

Here's how to implement the step definitions using PQSoft libraries:

### **Setup: Test Context**

First, create a shared context to store extracted tokens and HTTP state:

```csharp
using PQSoft.HttpFile;
using PQSoft.JsonComparer;
using System.Text.Json;

public class ApiTestContext
{
    public HttpClient Client { get; set; } = null!;
    public JsonComparer JsonComparer { get; } = new();
    public Dictionary<string, JsonElement> ExtractedTokens { get; } = new();
    public HttpResponseMessage? LastResponse { get; set; }

    // Replace tokens in text with extracted values
    public string SubstituteTokens(string text)
    {
        var result = text;
        foreach (var (key, value) in ExtractedTokens)
        {
            var token = $"{{{{{key}}}}}";
            if (result.Contains(token))
            {
                result = result.Replace(token, value.GetString() ?? value.GetRawText());
            }
        }
        return result;
    }
}
```

### **Step Definitions**

```csharp
using Reqnroll;
using PQSoft.HttpFile;
using System.Text;
using System.Net.Http.Json;

[Binding]
public class ApiSteps
{
    private readonly ApiTestContext _context;

    public ApiSteps(ApiTestContext context)
    {
        _context = context;
    }

    [Given(@"the following request")]
    public async Task GivenTheFollowingRequest(string rawHttpRequest)
    {
        // Substitute any tokens from previous steps
        var substituted = _context.SubstituteTokens(rawHttpRequest);

        // Parse the HTTP request
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(substituted));
        var parsedRequest = await HttpStreamParser.ParseAsync(stream);

        // Convert to HttpRequestMessage and send
        var httpRequest = parsedRequest.ToHttpRequestMessage();
        _context.LastResponse = await _context.Client.SendAsync(httpRequest);
    }

    [Then(@"the API returns the following response")]
    public async Task ThenTheApiReturnsTheFollowingResponse(string rawHttpResponse)
    {
        // Substitute tokens in expected response
        var expectedWithTokens = _context.SubstituteTokens(rawHttpResponse);

        // Parse expected response
        using var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedWithTokens));
        var expectedResponse = await HttpResponseParser.ParseAsync(expectedStream);

        // Build actual response for comparison
        var actualResponseText = $"""
            HTTP/1.1 {(int)_context.LastResponse!.StatusCode} {_context.LastResponse.ReasonPhrase}
            {string.Join("\n", _context.LastResponse.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}
            {string.Join("\n", _context.LastResponse.Content.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}

            {await _context.LastResponse.Content.ReadAsStringAsync()}
            """;

        using var actualStream = new MemoryStream(Encoding.UTF8.GetBytes(actualResponseText));
        var actualResponse = await HttpResponseParser.ParseAsync(actualStream);

        // 1. Compare status codes
        Assert.Equal(expectedResponse.StatusCode, actualResponse.StatusCode);

        // 2. Compare headers semantically (subset match - actual can have more)
        foreach (var expectedHeader in expectedResponse.Headers)
        {
            var actualHeader = actualResponse.Headers
                .FirstOrDefault(h => h.Name.Equals(expectedHeader.Name, StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(actualHeader);
            Assert.True(expectedHeader.SemanticEquals(actualHeader),
                $"Header '{expectedHeader.Name}' mismatch. Expected: '{expectedHeader}', Actual: '{actualHeader}'");
        }

        // 3. Compare JSON bodies with token extraction
        if (!string.IsNullOrWhiteSpace(expectedResponse.Body))
        {
            var result = _context.JsonComparer.ExactMatch(
                expectedResponse.Body,
                actualResponse.Body);

            // Store extracted tokens for use in subsequent steps
            foreach (var (key, value) in result.ExtractedValues)
            {
                _context.ExtractedTokens[key] = value;
            }

            // Assert match
            if (!result.IsMatch)
            {
                var mismatchDetails = string.Join("\n", result.Mismatches);
                Assert.Fail($"Response body mismatch:\n{mismatchDetails}");
            }
        }
    }
}
```

### **Initialization: WebApplicationFactory**

```csharp
[Binding]
public class Hooks
{
    private static WebApplicationFactory<Program>? _factory;

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        _factory?.Dispose();
    }

    [BeforeScenario]
    public void BeforeScenario(ApiTestContext context)
    {
        context.Client = _factory!.CreateClient();
    }
}
```

## Advanced Scenarios

### **Testing Error Cases**

```gherkin
Scenario: Invalid input returns validation error

  Given the following request
  """
  POST /api/job HTTP/1.1
  Content-Type: application/json; charset=utf-8

  {
      "JobType": ""
  }
  """

  Then the API returns the following response
  """
  HTTP/1.1 400 BadRequest
  Content-Type: application/problem+json; charset=utf-8

  {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Job Type Invalid",
      "status": 400,
      "detail": "The Job Type '' is invalid."
  }
  """
```

### **Testing Resource Not Found**

```gherkin
Scenario: Get non-existent job returns 404

  Given the following request
  """
  GET /api/job/status/_DOES_NOT_EXIST_ HTTP/1.1
  Accept: application/json
  """

  Then the API returns the following response
  """
  HTTP/1.1 404 NotFound
  Content-Type: application/problem+json; charset=utf-8

  {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
      "title": "Job Not Found",
      "status": 404,
      "detail": "The job with ID _DOES_NOT_EXIST_ was not found."
  }
  """
```

### **Subset Matching for Flexible Assertions**

When you only care about specific fields:

```gherkin
Scenario: Job response contains required fields

  Given the following request
  """
  GET /api/job/{{JOBID}} HTTP/1.1
  """

  Then the API returns the following response (subset)
  """
  HTTP/1.1 200 OK

  {
      "jobId": "{{JOBID}}",
      "status": "Running"
  }
  """
```

Implement with a separate step definition using `SubsetMatch`:

```csharp
[Then(@"the API returns the following response \(subset\)")]
public async Task ThenTheApiReturnsSubsetResponse(string rawHttpResponse)
{
    // Similar to exact match, but use SubsetMatch instead of ExactMatch
    var result = _context.JsonComparer.SubsetMatch(
        expectedResponse.Body,
        actualResponse.Body);

    // ... rest of implementation
}
```

### **Multiple Token Extraction**

```gherkin
Scenario: Extract multiple values from response

  Given the following request
  """
  POST /api/batch HTTP/1.1
  Content-Type: application/json

  {
      "jobs": ["job1", "job2", "job3"]
  }
  """

  Then the API returns the following response
  """
  HTTP/1.1 201 Created

  {
      "batchId": [[BATCH_ID]],
      "traceId": [[TRACE_ID]],
      "jobIds": [
          [[JOB_ID_1]],
          [[JOB_ID_2]],
          [[JOB_ID_3]]
      ],
      "createdAt": "{{NOW()}}"
  }
  """

  # Use any of the extracted values
  Given the following request
  """
  GET /api/batch/{{BATCH_ID}}/job/{{JOB_ID_1}} HTTP/1.1
  """
```

### **Discard Tokens (Validate Presence Only)**

Use empty tokens `[[]]` when you want to validate a field exists but don't care
about the value:

```gherkin
Then the API returns the following response
"""
{
    "jobId": [[JOBID]],
    "createdAt": [[]],
    "lastModified": [[]],
    "etag": [[]]
}
"""
```

## Best Practices

### **1. Use Descriptive Token Names**

```gherkin
# ✅ Good - Clear intent
"userId": [[USER_ID]]
"orderId": [[ORDER_ID]]

# ❌ Bad - Ambiguous
"id": [[ID1]]
"id": [[ID2]]
```

### **2. Test Unhappy Paths**

```gherkin
Scenario Outline: Various invalid inputs
  Given the following request
  """
  POST /api/job HTTP/1.1
  Content-Type: application/json

  {
      "JobType": "<jobType>"
  }
  """

  Then the API returns the following response
  """
  HTTP/1.1 400 BadRequest

  {
      "detail": "<errorMessage>"
  }
  """

  Examples:
    | jobType | errorMessage |
    |         | The Job Type '' is invalid. |
    | NULL    | The Job Type 'NULL' is invalid. |
    | $$$$    | The Job Type '$$$$' is invalid. |
```

### **3. Use Semantic Header Matching**

Headers are automatically compared semantically:

- Names are case-insensitive: `Content-Type` == `content-type`
- Parameters are order-independent: `charset=utf-8; boundary=abc` ==
  `boundary=abc; charset=utf-8`
- Whitespace is normalized

```gherkin
# All these match semantically:
Content-Type: application/json; charset=utf-8
content-type: application/json; charset=utf-8
Content-Type: application/json;charset=utf-8
```

### **4. Organize by Feature/Resource**

```
features/
  ├── Jobs.feature          # Job management scenarios
  ├── Users.feature         # User management scenarios
  ├── Billing.feature       # Billing scenarios
  └── Authentication.feature # Auth scenarios
```

### **5. Use Background for Common Setup**

```gherkin
Feature: Job Management

  Background:
    # Create a user for all job tests
    Given the following request
    """
    POST /api/user HTTP/1.1
    Content-Type: application/json

    {
        "username": "testuser"
    }
    """
    Then the API returns the following response
    """
    HTTP/1.1 201 Created

    {
        "userId": [[USER_ID]]
    }
    """

  Scenario: Create job as authenticated user
    # USER_ID is available from Background
    Given the following request
    """
    POST /api/user/{{USER_ID}}/job HTTP/1.1
    ...
```

## Testing with Time-Sensitive Data

For deterministic timestamp testing, inject a fake `TimeProvider`:

```csharp
public class Hooks
{
    [BeforeScenario("@FixedTime")]
    public void SetupFixedTime(ApiTestContext context)
    {
        var fixedTime = new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);

        context.JsonComparer = new JsonComparer(fakeTimeProvider);
    }
}
```

Then use in scenarios:

```gherkin
@FixedTime
Scenario: Create job with fixed timestamp
  Given the following request
  """
  POST /api/job HTTP/1.1
  ...
  """

  Then the API returns the following response
  """
  {
      "createdAt": "2025-01-15T10:00:00.000Z"
  }
  """
```

## Conclusion

The PQSoft libraries transform Cucumber/Reqnroll BDD testing by:

1. **Enabling natural language HTTP/JSON in Gherkin**: Write `.http` format
   directly in scenarios
2. **Token extraction and substitution**: Handle dynamic values elegantly
3. **Semantic comparison**: Follow HTTP/JSON specs automatically
4. **Multi-step workflows**: Pass data between steps without complex plumbing
5. **Readable scenarios**: Business stakeholders can understand API behavior

The result: **executable specifications that test real API behavior with minimal
code**.
