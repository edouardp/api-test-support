# PQSoft.JsonComparer

A powerful library for **structural JSON comparison** designed for testing REST APIs,
validating service responses, and comparing JSON documents received as strings or bytes.

## Core Value: Structural JSON Comparison

The primary purpose of JsonComparer is to compare JSON documents structurally,
regardless of formatting, property order, or whitespace differences:

```csharp
// These are structurally identical despite different formatting
string apiResponse = """{"name":"John","age":30,"active":true}""";
string expected = """
{
    "name": "John",
    "age": 30,
    "active": true
}
""";

var comparer = new JsonComparer();
var result = comparer.ExactMatch(expected, apiResponse);
// result.IsMatch = true - structure and values match
```

### Perfect for REST API Testing

When testing REST endpoints, you receive JSON as strings or bytes.
JsonComparer handles this seamlessly:

```csharp
// Test a REST API endpoint
var response = await httpClient.GetAsync("/api/users/123");
string responseJson = await response.Content.ReadAsStringAsync();

string expectedStructure = """
{
    "id": 123,
    "name": "John Doe",
    "email": "john@example.com",
    "status": "active"
}
""";

var result = comparer.ExactMatch(expectedStructure, responseJson);
Assert.True(result.IsMatch);
```

### Subset Matching for Contract Validation

Validate that required fields exist without caring about extra fields:

```csharp
// API might return additional fields - that's OK
string apiResponse = """
{
    "id": 123,
    "name": "John",
    "email": "john@example.com",
    "status": "active",
    "lastLogin": "2024-01-01T10:00:00Z",
    "preferences": {"theme": "dark"}
}
""";

string requiredFields = """
{
    "id": 123,
    "name": "John",
    "status": "active"
}
""";

var result = comparer.SubsetMatch(requiredFields, apiResponse);
// result.IsMatch = true - all required fields present
```

## Extended Value: Dynamic Content Handling

Beyond structural comparison, JsonComparer adds powerful features for handling
dynamic content common in modern APIs:

### Token Extraction for Dynamic Values

APIs generate IDs, timestamps, and tokens. Extract these for validation or subsequent use:

```csharp
// API returns generated values
string apiResponse = """
{
    "userId": "usr_abc123",
    "sessionToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "email": "test@example.com",
    "status": "active"
}
""";

// Extract dynamic values while validating structure
string template = """
{
    "userId": "[[USER_ID]]",
    "sessionToken": "[[SESSION_TOKEN]]",
    "email": "test@example.com",
    "status": "active"
}
""";

var result = comparer.ExactMatch(template, apiResponse);
// Structure validated, dynamic values extracted:
// result.ExtractedValues["USER_ID"] = "usr_abc123"
// result.ExtractedValues["SESSION_TOKEN"] = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Discard Tokens for Presence Testing

Sometimes you only care that a field exists, not its specific value. Use `[[_]]` as a discard token:

```csharp
string apiResponse = """
{
    "userId": "usr_abc123",
    "internalId": "internal_xyz789",
    "debugInfo": {"requestId": "req_456", "processingTime": 150},
    "email": "test@example.com"
}
""";

// Test that fields exist without caring about their values
string template = """
{
    "userId": "[[USER_ID]]",
    "internalId": "[[_]]",
    "debugInfo": "[[_]]",
    "email": "test@example.com"
}
""";

var result = comparer.ExactMatch(template, apiResponse);
// Validates that internalId and debugInfo exist, but doesn't capture their values
// Only USER_ID is captured: result.ExtractedValues["USER_ID"] = "usr_abc123"
// The "_" token is captured but typically ignored: result.ExtractedValues["_"] = "internal_xyz789"
```

### JsonElement Validation of Extracted Values

Extracted tokens are `JsonElement` objects, allowing rich type and value validation:

```csharp
string apiResponse = """
{
    "userId": 12345,
    "score": 98.5,
    "isActive": true,
    "tags": ["premium", "verified"],
    "metadata": {
        "created": "2024-01-01T10:00:00Z",
        "source": "api"
    },
    "nullField": null
}
""";

string template = """
{
    "userId": "[[USER_ID]]",
    "score": "[[SCORE]]",
    "isActive": "[[ACTIVE]]",
    "tags": "[[TAGS]]",
    "metadata": "[[META]]",
    "nullField": "[[NULL_FIELD]]"
}
""";

var result = comparer.ExactMatch(template, apiResponse);

// Validate JSON types
Assert.Equal(JsonValueKind.Number, result.ExtractedValues["USER_ID"].ValueKind);
Assert.Equal(JsonValueKind.Number, result.ExtractedValues["SCORE"].ValueKind);
Assert.Equal(JsonValueKind.True, result.ExtractedValues["ACTIVE"].ValueKind);
Assert.Equal(JsonValueKind.Array, result.ExtractedValues["TAGS"].ValueKind);
Assert.Equal(JsonValueKind.Object, result.ExtractedValues["META"].ValueKind);
Assert.Equal(JsonValueKind.Null, result.ExtractedValues["NULL_FIELD"].ValueKind);

// Extract and validate specific values
Assert.Equal(12345, result.ExtractedValues["USER_ID"].GetInt32());
Assert.Equal(98.5, result.ExtractedValues["SCORE"].GetDouble());
Assert.True(result.ExtractedValues["ACTIVE"].GetBoolean());

// Work with arrays
var tagsArray = result.ExtractedValues["TAGS"].EnumerateArray().ToArray();
Assert.Equal(2, tagsArray.Length);
Assert.Equal("premium", tagsArray[0].GetString());
Assert.Equal("verified", tagsArray[1].GetString());

// Work with objects
var metadata = result.ExtractedValues["META"];
Assert.Equal("2024-01-01T10:00:00Z", metadata.GetProperty("created").GetString());
Assert.Equal("api", metadata.GetProperty("source").GetString());
```

### Advanced JsonElement Validation Patterns

Use JsonElement properties for sophisticated validation:

```csharp
string complexResponse = """
{
    "pagination": {
        "page": 1,
        "pageSize": 20,
        "totalItems": 150,
        "hasNext": true
    },
    "items": [
        {"id": 1, "name": "Item 1", "price": 29.99},
        {"id": 2, "name": "Item 2", "price": 15.50}
    ],
    "summary": {
        "averagePrice": 22.745,
        "categories": ["electronics", "books", "clothing"]
    }
}
""";

string template = """
{
    "pagination": "[[PAGINATION]]",
    "items": "[[ITEMS]]",
    "summary": "[[SUMMARY]]"
}
""";

var result = comparer.ExactMatch(template, complexResponse);

// Validate pagination object structure
var pagination = result.ExtractedValues["PAGINATION"];
Assert.True(pagination.TryGetProperty("page", out var pageElement));
Assert.True(pagination.TryGetProperty("totalItems", out var totalElement));
Assert.Equal(1, pageElement.GetInt32());
Assert.True(totalElement.GetInt32() > 0);

// Validate array contents
var items = result.ExtractedValues["ITEMS"];
Assert.True(items.GetArrayLength() > 0);

foreach (var item in items.EnumerateArray())
{
    Assert.True(item.TryGetProperty("id", out var idElement));
    Assert.True(item.TryGetProperty("price", out var priceElement));
    Assert.Equal(JsonValueKind.Number, idElement.ValueKind);
    Assert.Equal(JsonValueKind.Number, priceElement.ValueKind);
    Assert.True(priceElement.GetDouble() > 0);
}

// Validate nested array in object
var summary = result.ExtractedValues["SUMMARY"];
var categories = summary.GetProperty("categories");
Assert.Equal(JsonValueKind.Array, categories.ValueKind);
Assert.True(categories.GetArrayLength() >= 3);

// Validate all categories are strings
foreach (var category in categories.EnumerateArray())
{
    Assert.Equal(JsonValueKind.String, category.ValueKind);
    Assert.False(string.IsNullOrEmpty(category.GetString()));
}
```

### Business Logic Validation with Extracted Values

Combine structural validation with business rule validation:

```csharp
string orderResponse = """
{
    "orderId": "ord_12345",
    "customerId": "cust_67890",
    "items": [
        {"productId": "prod_1", "quantity": 2, "unitPrice": 25.00, "total": 50.00},
        {"productId": "prod_2", "quantity": 1, "unitPrice": 15.99, "total": 15.99}
    ],
    "subtotal": 65.99,
    "tax": 5.28,
    "total": 71.27,
    "status": "confirmed"
}
""";

string template = """
{
    "orderId": "[[ORDER_ID]]",
    "customerId": "[[CUSTOMER_ID]]",
    "items": "[[ITEMS]]",
    "subtotal": "[[SUBTOTAL]]",
    "tax": "[[TAX]]",
    "total": "[[TOTAL]]",
    "status": "confirmed"
}
""";

var result = comparer.ExactMatch(template, orderResponse);

// Extract values for business logic validation
var items = result.ExtractedValues["ITEMS"];
var subtotal = result.ExtractedValues["SUBTOTAL"].GetDouble();
var tax = result.ExtractedValues["TAX"].GetDouble();
var total = result.ExtractedValues["TOTAL"].GetDouble();

// Validate business rules
double calculatedSubtotal = 0;
foreach (var item in items.EnumerateArray())
{
    var quantity = item.GetProperty("quantity").GetInt32();
    var unitPrice = item.GetProperty("unitPrice").GetDouble();
    var itemTotal = item.GetProperty("total").GetDouble();

    // Validate item total calculation
    Assert.Equal(quantity * unitPrice, itemTotal, 2);
    calculatedSubtotal += itemTotal;
}

// Validate order totals
Assert.Equal(calculatedSubtotal, subtotal, 2);
Assert.Equal(subtotal + tax, total, 2);

// Validate tax rate (assuming 8% tax)
var expectedTax = Math.Round(subtotal * 0.08, 2);
Assert.Equal(expectedTax, tax, 2);

// Extract IDs for subsequent API calls
var orderId = result.ExtractedValues["ORDER_ID"].GetString();
var customerId = result.ExtractedValues["CUSTOMER_ID"].GetString();
```

### Function-Based Validation

Validate generated values like GUIDs and timestamps without hardcoding specific values:

```csharp
string eventPayload = """
{
    "eventId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2024-01-01T10:00:00.000Z",
    "eventType": "UserCreated"
}
""";

string expectedPattern = """
{
    "eventId": "{{GUID()}}",
    "timestamp": "{{UTCNOW()}}",
    "eventType": "UserCreated"
}
""";

var result = comparer.ExactMatch(expectedPattern, eventPayload);
// Validates GUID format and timestamp format without exact value matching
```

### Variable Substitution for Test Templates

Create reusable test templates with environment-specific values:

```csharp
var testConfig = new Dictionary<string, object>
{
    ["BASE_URL"] = "https://api.staging.example.com",
    ["API_VERSION"] = "v2"
};

string expectedResponse = """
{
    "endpoint": "{{BASE_URL}}/{{API_VERSION}}/users",
    "version": "{{API_VERSION}}"
}
""";

string actualResponse = """
{
    "endpoint": "https://api.staging.example.com/v2/users",
    "version": "v2"
}
""";

var result = comparer.ExactMatch(expectedResponse, actualResponse, testConfig);
// Variables substituted before structural comparison
```

## Why Use JsonComparer?

### The Problem with Traditional JSON Testing

Traditional approaches fail with real-world API responses:

```csharp
// ❌ Brittle - breaks with formatting differences, property order, or dynamic values
string expected = """{"id":"12345","createdAt":"2024-01-01T10:00:00Z"}""";
string actual = """  {  "createdAt": "2024-01-01T10:05:30Z",  "id": "67890"  }  """;
Assert.Equal(expected, actual); // FAILS due to formatting, order, and dynamic values
```

### The JsonComparer Solution

```csharp
// ✅ Robust - handles formatting, order, and dynamic values
string expected = """{"id": "[[USER_ID]]", "createdAt": "{{UTCNOW()}}"}""";
string actual = """  {  "createdAt": "2024-01-01T10:05:30Z",  "id": "67890"  }  """;

var result = comparer.ExactMatch(expected, actual);
// result.IsMatch = true - structure matches, dynamic values extracted
```

## Core Features

- **Token Extraction**: `[[TOKEN_NAME]]` - Extract dynamic values for later use
- **Function Execution**: `{{GUID()}}`, `{{NOW()}}`, `{{UTCNOW()}}` - Validate generated values
- **Exact Matching**: Strict structural and value comparison
- **Subset Matching**: Verify required fields exist (ignores extra fields)
- **Variable Substitution**: Template-based JSON with variable replacement
- **Custom Functions**: Extend with your own validation functions
- **TimeProvider Support**: Deterministic time testing
- **Detailed Error Reporting**: Precise mismatch information

## Installation

```bash
dotnet add package PQSoft.JsonComparer
```

## Token Extraction Examples

### Basic Token Extraction
Extract dynamic values from API responses for use in subsequent tests:

```csharp
// API returns user with generated ID
string apiResponse = """
{
    "user": {
        "id": "usr_abc123",
        "email": "test@example.com",
        "status": "active"
    },
    "sessionToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
""";

string expectedStructure = """
{
    "user": {
        "id": "[[USER_ID]]",
        "email": "test@example.com",
        "status": "active"
    },
    "sessionToken": "[[SESSION_TOKEN]]"
}
""";

var comparer = new JsonComparer();
var result = comparer.ExactMatch(expectedStructure, apiResponse);

// Extract values for subsequent API calls
string userId = result.ExtractedValues["USER_ID"].GetString();     // "usr_abc123"
string token = result.ExtractedValues["SESSION_TOKEN"].GetString(); // "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Multi-Level Token Extraction
Handle complex nested structures with multiple dynamic values:

```csharp
string orderResponse = """
{
    "order": {
        "id": "ord_789",
        "items": [
            {"productId": "prod_123", "quantity": 2, "price": 29.99},
            {"productId": "prod_456", "quantity": 1, "price": 15.50}
        ],
        "total": 75.48,
        "customer": {
            "id": "cust_999",
            "shippingAddress": {
                "id": "addr_555"
            }
        }
    },
    "paymentId": "pay_777"
}
""";

string template = """
{
    "order": {
        "id": "[[ORDER_ID]]",
        "items": [
            {"productId": "[[PRODUCT1_ID]]", "quantity": 2, "price": "[[PRICE1]]"},
            {"productId": "[[PRODUCT2_ID]]", "quantity": 1, "price": "[[PRICE2]]"}
        ],
        "total": "[[TOTAL]]",
        "customer": {
            "id": "[[CUSTOMER_ID]]",
            "shippingAddress": {
                "id": "[[ADDRESS_ID]]"
            }
        }
    },
    "paymentId": "[[PAYMENT_ID]]"
}
""";

var result = comparer.ExactMatch(template, orderResponse);
// Extracts: ORDER_ID, PRODUCT1_ID, PRICE1, PRODUCT2_ID, PRICE2, TOTAL, CUSTOMER_ID, ADDRESS_ID, PAYMENT_ID
```

### Array Token Extraction
Extract values from JSON arrays:

```csharp
string usersResponse = """
{
    "users": [
        {"id": "user_1", "name": "Alice", "role": "admin"},
        {"id": "user_2", "name": "Bob", "role": "user"},
        {"id": "user_3", "name": "Charlie", "role": "user"}
    ],
    "totalCount": 3
}
""";

string pattern = """
{
    "users": [
        {"id": "[[ADMIN_ID]]", "name": "Alice", "role": "admin"},
        {"id": "[[USER1_ID]]", "name": "Bob", "role": "user"},
        {"id": "[[USER2_ID]]", "name": "Charlie", "role": "user"}
    ],
    "totalCount": "[[TOTAL]]"
}
""";

var result = comparer.ExactMatch(pattern, usersResponse);
// Extract specific user IDs for role-based testing
```

## Function Execution Examples

### Time-Based Validation
Validate timestamps without hardcoding specific times:

```csharp
string eventPayload = """
{
    "eventId": "550e8400-e29b-41d4-a716-446655440000",
    "eventType": "UserRegistered",
    "timestamp": "2024-01-01T10:00:00.000Z",
    "localTime": "2024-01-01T05:00:00.000-05:00",
    "data": {
        "userId": "user_123"
    }
}
""";

string expectedPattern = """
{
    "eventId": "{{GUID()}}",
    "eventType": "UserRegistered",
    "timestamp": "{{UTCNOW()}}",
    "localTime": "{{NOW()}}",
    "data": {
        "userId": "[[USER_ID]]"
    }
}
""";

var comparer = new JsonComparer();
var result = comparer.ExactMatch(expectedPattern, eventPayload);
// Validates GUID format, UTC timestamp, local timestamp, and extracts USER_ID
```

### Deterministic Time Testing with TimeProvider
Control time for predictable tests:

```csharp
// Set up controlled time
var fixedTime = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(-5));
var fakeTimeProvider = new FakeTimeProvider(fixedTime);

string actualResponse = """
{
    "processedAt": "2024-06-15T19:30:00.000Z",
    "localProcessedAt": "2024-06-15T14:30:00.000-05:00",
    "batchId": "batch_456"
}
""";

string expectedTemplate = """
{
    "processedAt": "{{UTCNOW()}}",
    "localProcessedAt": "{{NOW()}}",
    "batchId": "[[BATCH_ID]]"
}
""";

var comparer = new JsonComparer(fakeTimeProvider);
var result = comparer.ExactMatch(expectedTemplate, actualResponse);
// Uses controlled time for deterministic testing
```

### Mixed Functions and Tokens
Combine function validation with token extraction:

```csharp
string apiResponse = """
{
    "requestId": "req_abc123",
    "correlationId": "550e8400-e29b-41d4-a716-446655440000",
    "processedAt": "2024-01-01T10:00:00.000Z",
    "result": {
        "userId": "user_789",
        "status": "success"
    }
}
""";

string template = """
{
    "requestId": "[[REQUEST_ID]]",
    "correlationId": "{{GUID()}}",
    "processedAt": "{{UTCNOW()}}",
    "result": {
        "userId": "[[USER_ID]]",
        "status": "success"
    }
}
""";

var result = comparer.ExactMatch(template, apiResponse);
// Validates GUID and timestamp formats while extracting REQUEST_ID and USER_ID
```

## Subset Matching Examples

### API Contract Validation
Ensure required fields exist without caring about extra fields:

```csharp
// API might return extra fields in the future
string apiResponse = """
{
    "id": 123,
    "name": "John Doe",
    "email": "john@example.com",
    "status": "active",
    "createdAt": "2024-01-01T10:00:00Z",
    "lastLoginAt": "2024-01-15T08:30:00Z",
    "preferences": {
        "theme": "dark",
        "notifications": true
    },
    "metadata": {
        "source": "web",
        "version": "1.2.3"
    }
}
""";

// Only validate the fields we care about
string requiredFields = """
{
    "id": "[[USER_ID]]",
    "name": "John Doe",
    "email": "john@example.com",
    "status": "active"
}
""";

var result = comparer.SubsetMatch(requiredFields, apiResponse);
// Passes even with extra fields in response
```

### Database Record Validation
Validate core fields while ignoring audit fields:

```csharp
string dbRecord = """
{
    "id": 456,
    "productName": "Widget Pro",
    "price": 29.99,
    "category": "electronics",
    "inStock": true,
    "createdAt": "2024-01-01T10:00:00Z",
    "updatedAt": "2024-01-15T14:30:00Z",
    "createdBy": "admin",
    "version": 3
}
""";

string coreFields = """
{
    "productName": "Widget Pro",
    "price": "[[PRICE]]",
    "category": "electronics",
    "inStock": true
}
""";

var result = comparer.SubsetMatch(coreFields, dbRecord);
// Ignores audit fields, extracts price for further validation
```

### Microservices Integration Testing
Validate service contracts while allowing implementation flexibility:

```csharp
string serviceResponse = """
{
    "status": "success",
    "data": {
        "resourceId": "res_123",
        "type": "document",
        "permissions": ["read", "write"],
        "owner": {
            "id": "user_456",
            "name": "Alice"
        }
    },
    "metadata": {
        "processingTime": 150,
        "server": "api-01",
        "requestId": "req_789"
    },
    "links": {
        "self": "/api/resources/res_123",
        "owner": "/api/users/user_456"
    }
}
""";

string contractRequirements = """
{
    "status": "success",
    "data": {
        "resourceId": "[[RESOURCE_ID]]",
        "type": "document",
        "owner": {
            "id": "[[OWNER_ID]]"
        }
    }
}
""";

var result = comparer.SubsetMatch(contractRequirements, serviceResponse);
// Validates contract compliance while allowing extra implementation details
```

## Variable Substitution Examples

### Environment-Specific Testing
Use variables for different environments:

```csharp
var testConfig = new Dictionary<string, object>
{
    ["BASE_URL"] = "https://api.staging.example.com",
    ["API_VERSION"] = "v2",
    ["TENANT_ID"] = "tenant_staging_123"
};

string expectedResponse = """
{
    "apiEndpoint": "{{BASE_URL}}/{{API_VERSION}}/tenants/{{TENANT_ID}}",
    "version": "{{API_VERSION}}",
    "tenantId": "{{TENANT_ID}}",
    "environment": "staging"
}
""";

string actualResponse = """
{
    "apiEndpoint": "https://api.staging.example.com/v2/tenants/tenant_staging_123",
    "version": "v2",
    "tenantId": "tenant_staging_123",
    "environment": "staging"
}
""";

var result = comparer.ExactMatch(expectedResponse, actualResponse, testConfig);
// Variables are substituted before comparison
```

### Test Data Templates
Create reusable test templates:

```csharp
var userTestData = new Dictionary<string, object>
{
    ["USER_EMAIL"] = "test.user@example.com",
    ["USER_ROLE"] = "premium",
    ["SUBSCRIPTION_TIER"] = "gold",
    ["MAX_API_CALLS"] = 10000
};

string userProfileTemplate = """
{
    "profile": {
        "email": "{{USER_EMAIL}}",
        "role": "{{USER_ROLE}}",
        "subscription": {
            "tier": "{{SUBSCRIPTION_TIER}}",
            "limits": {
                "apiCalls": {{MAX_API_CALLS}}
            }
        }
    },
    "userId": "[[GENERATED_USER_ID]]",
    "createdAt": "{{UTCNOW()}}"
}
""";

// Use template with different test data sets
var result = comparer.ExactMatch(userProfileTemplate, actualUserResponse, userTestData);
```

## Custom Functions

### Register Domain-Specific Functions
Add custom validation functions for your domain:

```csharp
// Register custom functions
JsonComparer.RegisterFunction("ORDER_NUMBER", () => $"ORD-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}");
JsonComparer.RegisterFunction("SKU_CODE", () => $"SKU-{Guid.NewGuid().ToString("N")[..8].ToUpper()}");
JsonComparer.RegisterFunction("PRICE_FORMAT", () => $"{Random.Shared.NextDouble() * 100:F2}");

string orderTemplate = """
{
    "orderNumber": "{{ORDER_NUMBER()}}",
    "items": [
        {
            "sku": "{{SKU_CODE()}}",
            "price": "{{PRICE_FORMAT()}}"
        }
    ],
    "customerId": "[[CUSTOMER_ID]]"
}
""";

string actualOrder = """
{
    "orderNumber": "ORD-20240101-1234",
    "items": [
        {
            "sku": "SKU-A1B2C3D4",
            "price": "29.99"
        }
    ],
    "customerId": "cust_789"
}
""";

var result = comparer.ExactMatch(orderTemplate, actualOrder);
// Custom functions validate format while extracting CUSTOMER_ID
```

### Parameterized Custom Functions
Create functions that accept parameters:

```csharp
JsonComparer.RegisterFunction("DATE_FORMAT", (format) => DateTime.Now.ToString(format));
JsonComparer.RegisterFunction("RANDOM_STRING", (length) =>
    new string(Enumerable.Range(0, int.Parse(length))
        .Select(_ => (char)Random.Shared.Next('A', 'Z' + 1))
        .ToArray()));

string template = """
{
    "reportDate": "{{DATE_FORMAT('yyyy-MM-dd')}}",
    "reportId": "{{RANDOM_STRING('8')}}",
    "data": "[[REPORT_DATA]]"
}
""";
```

## Error Handling and Debugging

### Detailed Mismatch Information
Get precise information about what doesn't match:

```csharp
string expected = """
{
    "user": {
        "name": "John",
        "age": 30,
        "preferences": {
            "theme": "dark"
        }
    },
    "status": "active"
}
""";

string actual = """
{
    "user": {
        "name": "Jane",
        "age": 25,
        "preferences": {
            "theme": "light"
        }
    },
    "status": "inactive"
}
""";

var result = comparer.ExactMatch(expected, actual);

if (!result.IsMatch)
{
    foreach (var mismatch in result.Mismatches)
    {
        Console.WriteLine($"Path: {mismatch.Path}");
        Console.WriteLine($"Expected: {mismatch.Expected}");
        Console.WriteLine($"Actual: {mismatch.Actual}");
        Console.WriteLine($"Type: {mismatch.Type}");
        Console.WriteLine("---");
    }
}

// Output:
// Path: $.user.name
// Expected: John
// Actual: Jane
// Type: ValueMismatch
// ---
// Path: $.user.age
// Expected: 30
// Actual: 25
// Type: ValueMismatch
// ---
// Path: $.user.preferences.theme
// Expected: dark
// Actual: light
// Type: ValueMismatch
// ---
// Path: $.status
// Expected: active
// Actual: inactive
// Type: ValueMismatch
```

### Validation with Exception Details
Handle validation errors gracefully:

```csharp
try
{
    string malformedJson = """{ "name": "John", "age": }"""; // Invalid JSON
    var result = comparer.ExactMatch(expectedJson, malformedJson);
}
catch (JsonException ex)
{
    Console.WriteLine($"JSON parsing error: {ex.Message}");
    // Handle malformed JSON appropriately
}
```

## Real-World Use Cases

### 1. API Integration Testing
```csharp
// Test user registration endpoint
var registrationData = new { email = "test@example.com", password = "secure123" };
var response = await httpClient.PostAsJsonAsync("/api/register", registrationData);
var responseJson = await response.Content.ReadAsStringAsync();

var expectedStructure = """
{
    "user": {
        "id": "[[USER_ID]]",
        "email": "test@example.com",
        "createdAt": "{{UTCNOW()}}",
        "status": "pending_verification"
    },
    "verificationToken": "[[VERIFICATION_TOKEN]]"
}
""";

var result = comparer.ExactMatch(expectedStructure, responseJson);
Assert.True(result.IsMatch);

// Use extracted values in follow-up tests
var userId = result.ExtractedValues["USER_ID"].GetString();
var verificationToken = result.ExtractedValues["VERIFICATION_TOKEN"].GetString();
```

### 2. Event-Driven Architecture Testing
```csharp
// Validate published events
string publishedEvent = await eventStore.GetLastEventAsync("UserRegistered");

var expectedEventStructure = """
{
    "eventId": "{{GUID()}}",
    "eventType": "UserRegistered",
    "aggregateId": "[[USER_ID]]",
    "timestamp": "{{UTCNOW()}}",
    "version": 1,
    "data": {
        "email": "test@example.com",
        "registrationSource": "web"
    }
}
""";

var result = comparer.ExactMatch(expectedEventStructure, publishedEvent);
Assert.True(result.IsMatch);
```

### 3. Database Integration Testing
```csharp
// Validate database state after operation
var dbUser = await userRepository.GetByEmailAsync("test@example.com");
var userJson = JsonSerializer.Serialize(dbUser);

var expectedDbState = """
{
    "id": "[[DB_USER_ID]]",
    "email": "test@example.com",
    "passwordHash": "[[PASSWORD_HASH]]",
    "createdAt": "{{UTCNOW()}}",
    "updatedAt": "{{UTCNOW()}}",
    "isActive": true,
    "emailVerified": false
}
""";

var result = comparer.ExactMatch(expectedDbState, userJson);
Assert.True(result.IsMatch);
```

### 4. Configuration Validation
```csharp
// Validate application configuration
string configJson = await configService.GetConfigurationAsync();

var requiredConfigStructure = """
{
    "database": {
        "connectionString": "[[DB_CONNECTION]]",
        "timeout": "[[DB_TIMEOUT]]"
    },
    "api": {
        "baseUrl": "[[API_BASE_URL]]",
        "version": "v1"
    },
    "features": {
        "enableNewFeature": "[[FEATURE_FLAG]]"
    }
}
""";

var result = comparer.SubsetMatch(requiredConfigStructure, configJson);
Assert.True(result.IsMatch);

// Validate extracted configuration values
Assert.NotEmpty(result.ExtractedValues["DB_CONNECTION"].GetString());
Assert.True(int.Parse(result.ExtractedValues["DB_TIMEOUT"].GetString()) > 0);
```

## Advanced Patterns

### Chain Multiple Comparisons
```csharp
// Multi-step API testing workflow
var createResult = comparer.ExactMatch(createUserTemplate, createResponse);
var userId = createResult.ExtractedValues["USER_ID"].GetString();

var getUserResponse = await httpClient.GetAsync($"/api/users/{userId}");
var getUserJson = await getUserResponse.Content.ReadAsStringAsync();

var getUserTemplate = """
{
    "id": "[[SAME_USER_ID]]",
    "email": "test@example.com",
    "status": "active",
    "profile": {
        "createdAt": "{{UTCNOW()}}"
    }
}
""";

var getResult = comparer.ExactMatch(getUserTemplate, getUserJson);
Assert.Equal(userId, getResult.ExtractedValues["SAME_USER_ID"].GetString());
```

### Conditional Validation
```csharp
// Different validation based on response type
var baseTemplate = """
{
    "status": "[[STATUS]]",
    "timestamp": "{{UTCNOW()}}"
}
""";

var result = comparer.SubsetMatch(baseTemplate, apiResponse);
var status = result.ExtractedValues["STATUS"].GetString();

if (status == "success")
{
    var successTemplate = """
    {
        "status": "success",
        "data": {
            "id": "[[RESOURCE_ID]]"
        }
    }
    """;
    var successResult = comparer.SubsetMatch(successTemplate, apiResponse);
    Assert.True(successResult.IsMatch);
}
else if (status == "error")
{
    var errorTemplate = """
    {
        "status": "error",
        "error": {
            "code": "[[ERROR_CODE]]",
            "message": "[[ERROR_MESSAGE]]"
        }
    }
    """;
    var errorResult = comparer.SubsetMatch(errorTemplate, apiResponse);
    Assert.True(errorResult.IsMatch);
}
```

## Performance Considerations

JsonComparer is optimized for testing scenarios:
- Efficient JSON parsing using System.Text.Json
- Minimal memory allocation for token extraction
- Fast path for exact matches without tokens or functions
- Lazy evaluation of functions only when needed

For high-volume scenarios, consider:
- Reusing JsonComparer instances
- Pre-compiling templates with variables
- Using subset matching when full validation isn't needed

## Dependencies

- System.Text.Json (built-in .NET)
- No external dependencies for core functionality
