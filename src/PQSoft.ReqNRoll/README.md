# PQSoft.ReqNRoll

Write API tests in plain English using Gherkin syntax. No boilerplate, no manual HTTP client setup, no JSON parsing headaches.

## Why Use This?

**Before PQSoft.ReqNRoll:**

```csharp
[Fact]
public async Task CreateJob_ReturnsJobId()
{
    var client = _factory.CreateClient();
    var content = new StringContent("{\"JobType\":\"Upgrade\"}", Encoding.UTF8, "application/json");
    var response = await client.PostAsync("/api/job", content);
    
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var body = await response.Content.ReadAsStringAsync();
    var json = JsonDocument.Parse(body);
    var jobId = json.RootElement.GetProperty("jobId").GetString();
    jobId.Should().NotBeNullOrEmpty();
    
    // Now use that jobId in another request...
    var getResponse = await client.GetAsync($"/api/job/status/{jobId}");
    // More parsing, more assertions...
}
```

**After PQSoft.ReqNRoll:**

```gherkin
Scenario: Create a new job
  Given the following request
  """
  POST /api/job HTTP/1.1
  Content-Type: application/json
  
  {
    "JobType": "Upgrade"
  }
  """
  
  Then the API returns the following response
  """
  HTTP/1.1 201 Created
  Content-Type: application/json
  
  {
    "jobId": [[JOBID]]
  }
  """
  
  Given the following request
  """
  GET /api/job/status/{{JOBID}} HTTP/1.1
  """
  
  Then the API returns the following response
  """
  HTTP/1.1 200 OK
  
  {
    "jobId": "{{JOBID}}",
    "status": "Pending"
  }
  """
```

## Installation

```bash
dotnet add package PQSoft.ReqNRoll
dotnet add package Reqnroll.xUnit  # or Reqnroll.NUnit
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

## Quick Start

### 1. Create a Step Definition Class

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using PQSoft.ReqNRoll;
using Reqnroll;

[Binding]
public class ApiSteps : ApiStepDefinitions
{
    public ApiSteps(WebApplicationFactory<Program> factory) 
        : base(factory.CreateClient()) 
    {
    }
}
```

### 2. Write Your Feature File

```gherkin
Feature: User API

Scenario: Create and retrieve a user
  Given the following request
  """
  POST /api/users HTTP/1.1
  Content-Type: application/json
  
  {
    "name": "Alice",
    "email": "alice@example.com"
  }
  """
  
  Then the API returns the following response
  """
  HTTP/1.1 201 Created
  Content-Type: application/json
  
  {
    "id": [[USER_ID]],
    "name": "Alice",
    "email": "alice@example.com"
  }
  """
```

That's it. No manual HTTP setup, no JSON parsing, no assertion boilerplate.

## Features

### Token Extraction

Extract values from responses and use them in subsequent requests:

```gherkin
# Extract with [[TOKEN_NAME]]
Then the API returns the following response
"""
HTTP/1.1 200 OK

{
  "orderId": [[ORDER_ID]],
  "userId": [[USER_ID]]
}
"""

# Use with {{TOKEN_NAME}}
Given the following request
"""
GET /api/orders/{{ORDER_ID}}/user/{{USER_ID}} HTTP/1.1
"""
```

### Subset Matching

Only specify the fields you care about:

```gherkin
Then the API returns the following response
"""
HTTP/1.1 200 OK

{
  "status": "active"
}
"""
```

This matches even if the actual response has 50 other fields. Perfect for testing specific behaviors without brittle tests.

### Header Validation

Headers are automatically validated:

```gherkin
Then the API returns the following response
"""
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
X-Request-Id: [[REQUEST_ID]]

{ "data": "..." }
"""
```

### Setting Variables

Set variables before making requests, including support for functions:

```gherkin
# Static values
Given the variable 'API_KEY' is set to 'test-key-123'
Given the variable 'MAX_RETRIES' is set to 3
Given the variable 'ENABLED' is set to true

# Dynamic values with functions
Given the variable 'UNIQUE_USER' is set to 'user-{{GUID()}}'
Given the variable 'TIMESTAMP' is set to '{{NOW()}}'
```

Available functions:
- `{{GUID()}}` - Generate a unique GUID (e.g., `12345678-1234-1234-1234-123456789abc`)
- `{{NOW()}}` - Current local time
- `{{UTCNOW()}}` - Current UTC time

This is especially useful for parallel test execution where each test needs unique identifiers.

### Variable Assertions

```gherkin
Then the variable 'USER_ID' is of type 'String'
Then the variable 'CREATED_AT' is of type 'Date'
Then the variable 'COUNT' is of type 'Number'
Then the variable 'IS_ACTIVE' is of type 'Boolean'

Then the variable 'EMAIL' is equals to 'test@example.com'
Then the variable 'ORDER_ID' matches '^ORD-\d{6}$'
```

## Real-World Examples

### Testing Error Responses

```gherkin
Scenario: Invalid input returns 400
  Given the following request
  """
  POST /api/job HTTP/1.1
  Content-Type: application/json
  
  {
    "JobType": ""
  }
  """
  
  Then the API returns the following response
  """
  HTTP/1.1 400 BadRequest
  Content-Type: application/problem+json
  
  {
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    "title": "Job Type Invalid",
    "status": 400,
    "detail": "The Job Type '' is invalid."
  }
  """
```

### Testing 404s

```gherkin
Scenario: Non-existent resource returns 404
  Given the following request
  """
  GET /api/job/status/DOES_NOT_EXIST HTTP/1.1
  """
  
  Then the API returns the following response
  """
  HTTP/1.1 404 NotFound
  Content-Type: application/problem+json
  
  {
    "title": "Job Not Found",
    "status": 404
  }
  """
```

### Multi-Step Workflows

```gherkin
Scenario: Complete order workflow
  # Create user
  Given the following request
  """
  POST /api/users HTTP/1.1
  Content-Type: application/json
  
  { "name": "Bob" }
  """
  Then the API returns the following response
  """
  HTTP/1.1 201 Created
  { "id": [[USER_ID]] }
  """
  
  # Create order
  Given the following request
  """
  POST /api/orders HTTP/1.1
  Content-Type: application/json
  
  { "userId": "{{USER_ID}}", "item": "Widget" }
  """
  Then the API returns the following response
  """
  HTTP/1.1 201 Created
  { "orderId": [[ORDER_ID]], "status": "pending" }
  """
  
  # Process order
  Given the following request
  """
  POST /api/orders/{{ORDER_ID}}/process HTTP/1.1
  """
  Then the API returns the following response
  """
  HTTP/1.1 200 OK
  { "orderId": "{{ORDER_ID}}", "status": "completed" }
  """
  
  # Verify final state
  Given the following request
  """
  GET /api/users/{{USER_ID}}/orders HTTP/1.1
  """
  Then the API returns the following response
  """
  HTTP/1.1 200 OK
  {
    "orders": [
      { "orderId": "{{ORDER_ID}}", "status": "completed" }
    ]
  }
  """
```

### Testing with Authentication

```gherkin
Scenario: Authenticated request
  Given the following request
  """
  GET /api/profile HTTP/1.1
  Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
  """
  
  Then the API returns the following response
  """
  HTTP/1.1 200 OK
  
  {
    "username": "alice",
    "role": "admin"
  }
  """
```

### Parallel Test Execution

Use functions to generate unique identifiers for parallel test runs:

```gherkin
Scenario: Create user with unique identifier
  Given the variable 'UNIQUE_USER' is set to 'user-{{GUID()}}'
  
  Given the following request
  """
  POST /api/users HTTP/1.1
  Content-Type: application/json
  
  {
    "username": "{{UNIQUE_USER}}",
    "email": "{{UNIQUE_USER}}@example.com"
  }
  """
  
  Then the API returns the following response
  """
  HTTP/1.1 201 Created
  
  {
    "id": [[USER_ID]],
    "username": "{{UNIQUE_USER}}"
  }
  """
```

## Advanced Usage

### Custom HttpClient Configuration

```csharp
[Binding]
public class ApiSteps : ApiStepDefinitions
{
    public ApiSteps(WebApplicationFactory<Program> factory) 
        : base(CreateConfiguredClient(factory)) 
    {
    }
    
    private static HttpClient CreateConfiguredClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Version", "2.0");
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }
}
```

### Adding Custom Steps

Extend the base class with your own steps:

```csharp
[Binding]
public class ApiSteps : ApiStepDefinitions
{
    private readonly IDatabase _database;
    
    public ApiSteps(WebApplicationFactory<Program> factory, IDatabase database) 
        : base(factory.CreateClient()) 
    {
        _database = database;
    }
    
    [Given(@"the database contains a user with email '(.*)'")]
    public async Task GivenDatabaseContainsUser(string email)
    {
        await _database.InsertUser(new User { Email = email });
    }
    
    [Then(@"the database should contain (\d+) orders")]
    public async Task ThenDatabaseShouldContainOrders(int count)
    {
        var orders = await _database.GetOrders();
        orders.Count.Should().Be(count);
    }
}
```

## What's Included

This package provides pre-built Reqnroll step definitions:

- `Given the following request` - Send an HTTP request
- `Then the API returns the following response` - Validate response with subset matching
- `Given the variable '{name}' is set to '{value}'` - Set a variable (supports functions like `{{GUID()}}`)
- `Then the variable '{name}' is equals to '{value}'` - Assert extracted variable value
- `Then the variable '{name}' is of type '{type}'` - Assert variable type (String, Number, Boolean, Date, Object, Array, Null)
- `Then the variable '{name}' matches '{regex}'` - Assert variable matches regex pattern

## Dependencies

Built on top of:

- [PQSoft.HttpFile](https://www.nuget.org/packages/PQSoft.HttpFile) - HTTP request/response parsing
- [PQSoft.JsonComparer](https://www.nuget.org/packages/PQSoft.JsonComparer) - Smart JSON comparison with token extraction
- [Reqnroll](https://reqnroll.net/) - BDD test framework

## License

MIT
