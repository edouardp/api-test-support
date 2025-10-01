# Rationale

## API Testing

When we build REST APIs in C#, we want to test if they behave as we expect. 

ASP.NET gives us some useful tools to push this testing left to the "inner dev loop", the
write, test, debug activity that an engineer performs on her laptop, and forms the most
important flywheel that delivers developer velocity and productivity.

Specifically ASP.NET and the .NET ecosystem give us:

* **TestServer** and **WebApplicationFactory**

      The ability to instantiate the running API and as much of the business/domain later as
      required, and a client to interact with it, all running fast and locally on the engineers
      laptop.

* **XUnit**, **Moq**, **AwesomeAssertions**, **Reqnroll**

      High quality test automation infrastructure, also running fast and locally. These
      are often considered essential for the lowest level, granular unit tests of individual
      methods or classes, but are broadly used across the industry further up the test
      pyramid

      The tests built with thios infrastructure is integrated into engineers IDEs and command-line
      tooling, and can even be run automatically with test runners like NCrunch or dotCovers
      Continuous Testing mode.

      Reqnroll extends the automated tests to Behaviour Driven Design using the industry standard
      cucumber format, which transparently compiles down to unit tests.

* **TestContainers**

      The ability to include other systems and and services directly into your unit tests in a
      transparent and fully managed way using docker containers. 

      A great example is running a database as part of your unit tests, so a call to the API within
      your unit tests can tranverse the code all the way to the database and back again to not only
      validate your API, but also the full vertical slice of you code all the way to the DB.

### Aside - "Unit Tests" vs "Fast Tests"

Traditionally, "unit tests" meant testing a single method or class in isolation, with all dependencies mocked out. While these focused tests are still crucial for code correctness, modern development practices have expanded what we can test quickly.

**Fast Tests** go beyond single-method testing. With modern tooling, you can test much broader scenarios while maintaining the speed and reliability of traditional unit tests:

- **Full API endpoints** using `WebApplicationFactory` - instantiate your entire web app in memory
- **Complete vertical slices** from HTTP request → business logic → database → response
- **Real database interactions** using TestContainers with Docker - often sub-100ms per test
- **Multi-step workflows** that exercise complete user journeys
- **Integration scenarios** that validate multiple components working together

The key insight: **if a test runs locally in milliseconds and gives immediate feedback, it belongs in your inner dev loop** - regardless of how much of the system it exercises.

For example, a "Fast Test" might:

1. Start a PostgreSQL container via TestContainers (~2 seconds startup, cached between tests)
2. Make an HTTP POST to `/api/job` via WebApplicationFactory (in-memory, no network)
3. Insert data through your API into the real database (~50ms)
4. Query the database through another API endpoint (~30ms)
5. Validate the full response with PQSoft libraries (~5ms)

**Total test time: ~85ms after container warmup**. Fast enough to run hundreds of times per hour during development.

This is radically different from slow integration tests that require:

- Deploying to remote environments
- Waiting for build pipelines
- Coordinating shared test databases
- Running tests sequentially to avoid conflicts

**Fast Tests blur the line between "unit" and "integration"** - they test integrated behavior at unit test speed.

I'm going to use the phrase "Unit Tests" throughout this document, but mostly I mean "Fast Tests" utilizing a standard Unit Test framework like xUnit. The tests may exercise a single method, an entire API endpoint, or a full vertical slice through your application - what matters is they're fast enough for the inner dev loop.

## Correct and Complete

In software devlepment, there is an idea of Correct and Complete, where the code does what it is
supposed to do, without any bugs or misbehaviour (the "Correct" part), and it handles all the possible
use cases, not just the happy path, and performs as intended in every one of those scenarios (the
"Complete" part).

For example, a server should be able to handle

* the network changing on the fly - a network interfaced being added or removed
* the disk filling up, so there is no space to write files locally
* inputs being invalid
* deadlocks cancelling a query in the database
* the database being offline, then coming back online
* Handling Leap years properly
* handling 2am happening twice (or never) as Daylight Savings start and ends
* and many, many more

Each "unhappy path" should have a clear strategy for handling the situation

I find that many teams do a good job of correctness, but only a so-so job on completeness.

Mocking frameworking like Moq help here a lot - failure modes that are hard or near impossible to
trigger in a live system (whether in prod or pre-prod) can be invoked by a strategic mock when running
the systems inside your unit tests - like mocking the databae layer to throw a
`MySqlException(ER_LOCK_DEADLOCK)` to ensure the system handles the deadlock correctly, ideally by retrying
the SQL statement or transaction.

## So how do we test an API?

The simplest level of testing is the send a request to the REST API, and validate we get the
correct Reponse, by comparing the result to what we expected.

In psuedocode this would be

```
expectedResponse = new HttpReponse(...)   // specify the expected response we want
request = new HttpRequest("GET", "/api/user/1234")
actualResponse = httpClient.send(request)
Assert.Equal(expectedResponse, actualResponse)
```

e.g.

We send

```
GET /api/user/1234 HTTP/1.1
```

And we get a HTTP Reponse

```
HTTP/1.1 200 OK
Date: Sun, 10 Oct 2010 23:26:07 GMT
Server: Apache/2.2.8 (Ubuntu) mod_ssl/2.2.8 OpenSSL/0.9.8g
Last-Modified: Sun, 26 Sep 2010 22:04:35 GMT
X-Request-ID: 7f3e1c2a
ETag: "45b6-834-49130cc1182c0"
Cache-Control: max-age=3600, no-store
Via: 1.1 varnish
Accept-Ranges: bytes
Content-Length: 12
Connection: close
Content-Type: application/json

{"UserID": 1234, "Username": "joeblogs"}
```

But this is where we have to stop and consider what we mean by the expected response being
equal to the actual response.

### Header Case

The HTTP spec states that we should treat the header "content-type: text/html" to be equivilent to
the header "Content-type: text/html"

### Header Whitespace

The HTTP spec also states that the header "Content-type:  text/html" is equivilent to the header
"Content-type: test/html ". i.e. additional whitespace is ignored

### Cache-Control Header and Parameter Order

Another thing the spec says is that "Cache-Control: max-age=3600, no-store" is equivilent to
"Cache-Control: no-store, max-age=3600".

If your expected response is "Cache-Control: max-age=3600, no-store", but the actual response
is "Cache-Control: no-store, max-age=3600" you want to say that that expected header matches.

But that there are some headers where the order is important, e.g.
"Content-Encoding: sdch, gzip". The order matters, and cannot be reversed.

### Date Header

The `Date` header is simply the date that the response is generated, and, in almost all cases,
is not important for correctness, and, unless special effort is applied to inject a known
time into the REST API server, will change every time the unit test is run.

For headers like this, you may want to ignore it altogether, or perhaps there may be a case
where you want the header to be present, but you don't care about the value

### X-Request-ID Header

There are some headers that may be added the server code, or by middleware, or even
by intermediate proxies. "X-Request-ID" is one of these, and we may want to ignore it
altogether, or simply validate that the header is present, while ignoring the
value.

"Via: 1.1 varnish" is explicitly a proxy header, and may be added by intermediate hops
in the HTTP request when running in the real world.


## JSON Response Body

The JSON body itself can be strucurally equivilent, but not be exactly the same.

### Whitespace

`{"Username":"joeblogs"}` is strucurally equivilent to `{ "Username": "joeblogs" }`
which is also equivilent to 

```
{
  "Username":"joeblogs"
}
```

### Field Order

```
{
  "UserID": 1234,
  "Username": "joeblogs"
}
```

is strucrally equivilent to

```
{
  "Username": "joeblogs",
  "UserID": 1234
}
```

### Numerical Equivilence

1 ≡ 1.0 ≡ 1e0


### Escaped Equivilence

"hello" ≡ "he\u006Clo"


## Selective Field Testing, Field Presence Only

Sometimes, when testing a specific action with an API, we may not care about all of
the feilds, but only the ones that are relevant to the action we are performing.

Another common case is that we want to ensure that a certain field is present, but we don't
care what the value is. E.g. when creating a new entity.

For example a POST call to /api/job, 
where we care that we get a reponse back with the job details, but every call to the API
will give us a unique job ID.

This is a little like the Date: or X-Request-ID headers - we may want to ensure the reponse
has a field, but we know that the value for that field changes every time.



## Down the Rabbit Hole

Sooner or later the response may have values that could be correct or incorrect depending on the
meaning of data. 

e.g. does the order of the value matter in this array `[1,2,3]`? Is `[2,1,3]` the same? Almost always
no, but its more grey with something like `[{"StockId": 221313},{"StockId": 621142}]`. at this point
the answer may end up being "it depends".

Sometimes we are working with an ordered list, and sometimes we are not. JSON has no "set" datatype. Some
JSON HTTP Responses might have multiple arrays, some which need to be treated as ordered arrays, and
some that should be treated as unordered sets.

And another valid JSON reponse might be `{"a":1, "a":2}`, which is "valid" JSON text under RFC 8259, but it’s not "well-defined". Treating this consistantly is also hard.

And then there is Unicode normalisation: "é" vs "e\u0301"

## Is Expected a Subset of Actual?

A lot of the time, it can be very valuable to allow check if the expected document is a subset
of the Actual document returned by the API.

e.g.

Expected Response:

```
{
  "Job ID": 1234,
  "Job Status": "RUNNING"
}
```

Request

```
GET /api/job/1234
```

Actual Response:

```
{
  "Trace ID": "6168937b-eeb0-485a-9c16-7160fc7461b5",
  "Job Started": "2025-12-09T16:09:53+00:00",
  "Job ID": 1234,
  "Job Status": "RUNNING"
}
```

If we just want to ensure that the status code is working as expected, then we may want to
only check if the expcted response is a subset of the actual reponse.

## Ignoring values

Building on the subset exampkle above, it may be good to do a subset match, but also have the
ability to ignore the values of certain fields, but check that they are present.

Expected Response:

```
{
  "Job Started": [[IGNORE]],
  "Job ID": 1234,
  "Job Status": "RUNNING"
}
```

Request

```
GET /api/job/1234
```

Actual Response:

```
{
  "Trace ID": "6168937b-eeb0-485a-9c16-7160fc7461b5",
  "Job Started": "2025-12-09T16:09:53+00:00",
  "Job ID": 1234,
  "Job Status": "RUNNING"
}
```

## Can I do all this in C# code in my Unit tests?

You certainly can.

For example, to check if a JSON response matches what you expect:

```csharp
[Fact]
public async Task GetUser_ShouldReturnUserDetails()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/user/1234");
    var actualJson = await response.Content.ReadAsStringAsync();

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Parse both JSON strings
    var expectedDoc = JsonDocument.Parse("""{"UserID": 1234, "Username": "joeblogs"}""");
    var actualDoc = JsonDocument.Parse(actualJson);

    // Compare field by field
    var expectedUserId = expectedDoc.RootElement.GetProperty("UserID").GetInt32();
    var actualUserId = actualDoc.RootElement.GetProperty("UserID").GetInt32();
    Assert.Equal(expectedUserId, actualUserId);

    var expectedUsername = expectedDoc.RootElement.GetProperty("Username").GetString();
    var actualUsername = actualDoc.RootElement.GetProperty("Username").GetString();
    Assert.Equal(expectedUsername, actualUsername);
}
```

This works, but it's verbose and doesn't handle:

- Whitespace differences
- Field order differences
- Nested objects
- Arrays
- Dynamic values (dates, IDs, trace IDs)
- Subset matching

You could write helper methods to handle these cases, but you'd essentially be building a JSON comparison library.

### The Problem Gets Worse

Now consider checking headers with semantic equivalence:

```csharp
// Check Content-Type header (case-insensitive name, parameter order independent)
var contentTypeHeader = response.Headers
    .FirstOrDefault(h => h.Key.Equals("content-type", StringComparison.OrdinalIgnoreCase));

Assert.NotNull(contentTypeHeader);

// Parse header value and parameters
var headerValue = contentTypeHeader.Value.First();
var parts = headerValue.Split(';');
var mainValue = parts[0].Trim();
Assert.Equal("application/json", mainValue);

// Parse parameters
var parameters = parts.Skip(1)
    .Select(p => p.Split('='))
    .ToDictionary(p => p[0].Trim(), p => p[1].Trim());

Assert.Contains("charset", parameters.Keys);
Assert.Equal("utf-8", parameters["charset"]);
```

This is extremely verbose for a simple header check! And you'd need to write this boilerplate for every test.

### And It Gets Even More Complex

What about testing with dynamic values that change on every request?

```csharp
// How do you test this when Job ID changes every time?
var actualJson = """
{
    "JobID": "7a3b2c1d-4e5f-6a7b-8c9d-0e1f2a3b4c5d",
    "Status": "RUNNING",
    "CreatedAt": "2025-01-15T10:30:45.123Z"
}
""";

// You'd have to:
// 1. Parse the JSON
// 2. Extract the JobID value
// 3. Validate it's a valid GUID format
// 4. Check the other fields
// 5. Store the JobID for use in subsequent tests

var doc = JsonDocument.Parse(actualJson);
var jobId = doc.RootElement.GetProperty("JobID").GetString();
Assert.Matches(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", jobId);
Assert.Equal("RUNNING", doc.RootElement.GetProperty("Status").GetString());

// And now what if you need that JobID in the next test?
// You'd need test fixtures, static variables, or complex test orchestration
```

## The Solution: PQSoft API Test Support

This library provides three focused tools that solve these problems elegantly:

### 1. **PQSoft.JsonComparer** - Semantic JSON Comparison

Instead of manual parsing and comparison:

```csharp
var comparer = new JsonComparer();
var expectedJson = """{"UserID": 1234, "Username": "joeblogs"}""";
var actualJson = """{"Username": "joeblogs", "UserID": 1234}"""; // Different order

var result = comparer.ExactMatch(expectedJson, actualJson);
Assert.True(result.IsMatch); // Handles field order, whitespace automatically
```

### 2. **Token Extraction** - Handle Dynamic Values

For values that change (IDs, timestamps, trace IDs):

```csharp
var expectedJson = """
{
    "JobID": "[[JOB_ID]]",
    "Status": "RUNNING",
    "CreatedAt": "{{NOW()}}"
}
""";

var actualJson = """
{
    "JobID": "7a3b2c1d-4e5f-6a7b-8c9d-0e1f2a3b4c5d",
    "Status": "RUNNING",
    "CreatedAt": "2025-01-15T10:30:45.123Z"
}
""";

var comparer = new JsonComparer();
var (isMatch, extractedValues, mismatches) = comparer.ExactMatch(expectedJson, actualJson);

Assert.True(isMatch);

// Use the extracted JobID in subsequent tests
string jobId = extractedValues["JOB_ID"].GetString();
var statusResponse = await client.GetAsync($"/api/job/{jobId}/status");
```

**Tokens** (`[[TOKEN_NAME]]`) extract dynamic values for later use.
**Functions** (`{{NOW()}}`, `{{GUID()}}`, `{{UTCNOW()}}`) validate timestamps and generated values.

### 3. **Subset Matching** - Test What Matters

When you only care about specific fields:

```csharp
var expectedJson = """
{
    "JobID": 1234,
    "Status": "RUNNING"
}
""";

var actualJson = """
{
    "TraceID": "6168937b-eeb0-485a-9c16-7160fc7461b5",
    "JobStarted": "2025-12-09T16:09:53+00:00",
    "JobID": 1234,
    "Status": "RUNNING",
    "WorkerNode": "node-7",
    "Priority": "HIGH"
}
""";

var result = comparer.SubsetMatch(expectedJson, actualJson);
Assert.True(result.IsMatch); // Only checks fields in expected
```

### 4. **PQSoft.HttpFile** - Parse HTTP Requests/Responses

Parse `.http` files (like VS Code uses) directly in tests:

```csharp
var rawHttp = """
    POST https://api.example.com/jobs HTTP/1.1
    Content-Type: application/json
    Authorization: Bearer token123

    {"jobType": "processing", "priority": "high"}
    """;

using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
var request = await HttpStreamParser.ParseAsync(stream);

// Convert to HttpRequestMessage and send
var httpRequest = request.ToHttpRequestMessage();
var response = await client.SendAsync(httpRequest);
```

### 5. **Semantic Header Comparison**

Headers are compared according to HTTP specification rules:

```csharp
var header1 = new ParsedHeader("Content-Type", "application/json",
    new() { { "charset", "utf-8" }, { "boundary", "abc" } });

var header2 = new ParsedHeader("content-type", "application/json",
    new() { { "boundary", "abc" }, { "charset", "utf-8" } }); // Different case, order

Assert.True(header1.SemanticEquals(header2));
// ✅ Names are case-insensitive (HTTP spec)
// ✅ Parameter order doesn't matter (HTTP spec)
// ✅ Whitespace is normalized (HTTP spec)
```

### 6. **FluentAssertions Integration**

Expressive, readable test assertions:

```csharp
var actualJson = """{"JobID": "abc123", "Status": "RUNNING"}""";
var expectedJson = """{"JobID": "[[JOB_ID]]", "Status": "RUNNING"}""";

var assertions = actualJson.AsJsonString().Should();
assertions.FullyMatch(expectedJson);

// Access extracted values fluently
string jobId = assertions.ExtractedValues["JOB_ID"].GetString();
jobId.Should().NotBeNullOrEmpty();
```

## Design Principles

The library was built on these principles:

### 1. **Fast Inner Dev Loop**
Tests run locally in milliseconds using `TestServer`, `WebApplicationFactory`, and standard xUnit infrastructure.

### 2. **Semantic Correctness**
Comparisons follow HTTP and JSON specifications precisely:

- Header names are case-insensitive
- JSON field order doesn't matter
- Whitespace is normalized
- Parameter order is ignored where appropriate

### 3. **Handle Real-World Testing Needs**
- **Dynamic values**: Extract and reuse generated IDs, timestamps, trace IDs
- **Subset matching**: Test only what matters for each scenario
- **Flexible assertions**: Exact match when needed, subset when appropriate

### 4. **Developer Experience**
- Readable test code using FluentAssertions style
- Clear error messages with detailed mismatches
- Natural syntax that expresses intent

### 5. **Supports "Correct and Complete" Testing**
As discussed earlier, software should be both correct (bug-free) and complete (handles all scenarios).
This library enables complete test coverage without sacrificing correctness:

- Mock dynamic time with `TimeProvider` for deterministic timestamp testing
- Extract unpredictable values with tokens to test dynamic scenarios
- Use subset matching for focused assertions without brittle full-response checks

## Example: Complete API Test Workflow

Here's a realistic end-to-end test demonstrating all features:

```csharp
[Fact]
public async Task CompleteJobWorkflow_ShouldHandleFullLifecycle()
{
    var client = _factory.CreateClient();
    var comparer = new JsonComparer();

    // Step 1: Create a job (extract the generated JobID)
    var createResponse = await client.PostAsync("/api/jobs",
        JsonContent.Create(new { jobType = "processing", priority = "high" }));

    var createJson = await createResponse.Content.ReadAsStringAsync();
    var expectedCreate = """
    {
        "JobID": "[[JOB_ID]]",
        "Status": "PENDING",
        "CreatedAt": "{{NOW()}}"
    }
    """;

    var createAssertions = createJson.AsJsonString().Should();
    createAssertions.ContainSubset(expectedCreate); // Ignores extra fields

    string jobId = createAssertions.ExtractedValues["JOB_ID"].GetString();

    // Step 2: Poll job status (use extracted JobID)
    var statusResponse = await client.GetAsync($"/api/jobs/{jobId}");
    var statusJson = await statusResponse.Content.ReadAsStringAsync();

    var expectedStatus = """
    {
        "JobID": "[[]]",
        "Status": "RUNNING"
    }
    """;

    statusJson.AsJsonString().Should().ContainSubset(expectedStatus);

    // Step 3: Validate headers semantically
    var contentType = statusResponse.Headers
        .GetValues("Content-Type").First();
    var parsedHeader = HttpHeadersParser.ParseHeader($"Content-Type: {contentType}");

    var expectedHeader = new ParsedHeader("content-type", "application/json",
        new() { { "charset", "utf-8" } });

    Assert.True(parsedHeader.SemanticEquals(expectedHeader));
}
```

## Conclusion

Testing APIs correctly and completely requires handling:

- HTTP specification semantics (case-insensitivity, parameter order, whitespace)
- JSON structural equivalence (field order, whitespace, numerical formats)
- Dynamic values that change per request
- Subset matching for focused testing
- Value extraction for multi-step workflows

**PQSoft API Test Support** provides focused, composable tools that solve these challenges while maintaining fast inner dev loop performance and readable test code.

The result: **more comprehensive test coverage, fewer brittle tests, and faster developer velocity**.
