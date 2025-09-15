using AwesomeAssertions;
using System.Text;

namespace PQSoft.HttpFile.UnitTests;

public class HttpFileParserTests
{
    [Fact]
    public async Task ParseAsync_Should_Parse_Single_Request_Without_Separator()
    {
        // Arrange
        const string rawRequest = """
            GET /api/users HTTP/1.1
            Host: example.com
            Accept: application/json
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var results = new List<ParsedHttpRequest>();
        await foreach (var request in HttpFileParser.ParseAsync(stream))
        {
            results.Add(request);
        }

        // Assert
        results.Should().HaveCount(1);
        var result = results[0];
        result.Method.Should().Be(HttpMethod.Get);
        result.Url.Should().Be("/api/users");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Headers.Should().ContainSingle(header => header.Name == "Accept")
            .Which.Value.Should().Be("application/json");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Parse_Multiple_Requests_Separated_By_Hash()
    {
        // Arrange
        const string rawRequests = """
            GET /api/users HTTP/1.1
            Host: example.com
            Accept: application/json

            ###

            POST /api/users HTTP/1.1
            Host: example.com
            Content-Type: application/json

            {"name": "John"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequests));

        // Act
        var results = new List<ParsedHttpRequest>();
        await foreach (var request in HttpFileParser.ParseAsync(stream))
        {
            results.Add(request);
        }

        // Assert
        results.Should().HaveCount(2);

        // First request
        var first = results[0];
        first.Method.Should().Be(HttpMethod.Get);
        first.Url.Should().Be("/api/users");
        first.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        first.Headers.Should().ContainSingle(header => header.Name == "Accept")
            .Which.Value.Should().Be("application/json");
        first.Body.Should().BeEmpty();

        // Second request
        var second = results[1];
        second.Method.Should().Be(HttpMethod.Post);
        second.Url.Should().Be("/api/users");
        second.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        second.Headers.Should().ContainSingle(header => header.Name == "Content-Type")
            .Which.Value.Should().Be("application/json");
        second.Body.Should().Be("{\"name\": \"John\"}");
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Empty_Lines_Around_Separator()
    {
        // Arrange
        const string rawRequests = """
            GET /first HTTP/1.1
            Host: example.com

            ###



            POST /second HTTP/1.1
            Host: example.com

            ###

            PUT /third HTTP/1.1
            Host: example.com
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequests));

        // Act
        var results = new List<ParsedHttpRequest>();
        await foreach (var request in HttpFileParser.ParseAsync(stream))
        {
            results.Add(request);
        }

        // Assert
        results.Should().HaveCount(3);
        results[0].Method.Should().Be(HttpMethod.Get);
        results[0].Url.Should().Be("/first");
        results[1].Method.Should().Be(HttpMethod.Post);
        results[1].Url.Should().Be("/second");
        results[2].Method.Should().Be(HttpMethod.Put);
        results[2].Url.Should().Be("/third");
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Leading_And_Trailing_Separators()
    {
        // Arrange
        const string rawRequests = """
            ###
            GET /first HTTP/1.1
            Host: example.com

            ###
            POST /second HTTP/1.1
            Host: example.com
            ###
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequests));

        // Act
        var results = new List<ParsedHttpRequest>();
        await foreach (var request in HttpFileParser.ParseAsync(stream))
        {
            results.Add(request);
        }

        // Assert
        results.Should().HaveCount(2);
        results[0].Method.Should().Be(HttpMethod.Get);
        results[0].Url.Should().Be("/first");
        results[1].Method.Should().Be(HttpMethod.Post);
        results[1].Url.Should().Be("/second");
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Consecutive_Separators()
    {
        // Arrange
        const string rawRequests = """
            GET /first HTTP/1.1
            Host: example.com

            ###
            ###
            ###
            POST /second HTTP/1.1
            Host: example.com
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequests));

        // Act
        var results = new List<ParsedHttpRequest>();
        await foreach (var request in HttpFileParser.ParseAsync(stream))
        {
            results.Add(request);
        }

        // Assert
        results.Should().HaveCount(2);
        results[0].Method.Should().Be(HttpMethod.Get);
        results[0].Url.Should().Be("/first");
        results[1].Method.Should().Be(HttpMethod.Post);
        results[1].Url.Should().Be("/second");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Stream_Is_Null()
    {
        // Act
        Func<Task> act = async () =>
        {
            await foreach (var _ in HttpFileParser.ParseAsync(null!))
            {
                // This should not be reached
            }
        };

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("HTTP stream cannot be null. (Parameter 'httpStream')");
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Request_With_Body()
    {
        // Arrange
        const string rawRequests = """
            POST /api/users HTTP/1.1
            Host: example.com
            Content-Type: application/json

            {"name": "John", "age": 30}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequests));

        // Act
        var results = new List<ParsedHttpRequest>();
        await foreach (var request in HttpFileParser.ParseAsync(stream))
        {
            results.Add(request);
        }

        // Assert
        results.Should().HaveCount(1);
        var result = results[0];
        result.Method.Should().Be(HttpMethod.Post);
        result.Url.Should().Be("/api/users");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Type")
            .Which.Value.Should().Be("application/json");
        result.Body.Should().Be("{\"name\": \"John\", \"age\": 30}");
    }

    [Fact]
    public async Task ParseFileAsync_Should_Parse_File_With_Multiple_Requests()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        try
        {
            const string fileContent = """
                GET /first HTTP/1.1
                Host: example.com

                ###

                POST /second HTTP/1.1
                Host: example.com
                """;
            await File.WriteAllTextAsync(tempFilePath, fileContent, Encoding.UTF8);

            // Act
            var results = new List<ParsedHttpRequest>();
            await foreach (var request in HttpFileParser.ParseFileAsync(tempFilePath))
            {
                results.Add(request);
            }

            // Assert
            results.Should().HaveCount(2);
            results[0].Method.Should().Be(HttpMethod.Get);
            results[0].Url.Should().Be("/first");
            results[1].Method.Should().Be(HttpMethod.Post);
            results[1].Url.Should().Be("/second");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public async Task ParseFileAsync_Should_ThrowFileNotFoundException_When_File_Not_Exists()
    {
        // Arrange
        var nonExistentPath = "/path/that/does/not/exist.http";

        // Act
        Func<Task> act = async () =>
        {
            await foreach (var _ in HttpFileParser.ParseFileAsync(nonExistentPath))
            {
                // This should not be reached
            }
        };

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage($"The file '{nonExistentPath}' was not found.*")
            .Where(ex => ex.FileName == nonExistentPath);
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Invalid_Request_Format()
    {
        // Arrange
        const string rawRequests = """
            INVALID_REQUEST_LINE

            ###

            GET /valid HTTP/1.1
            Host: example.com
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequests));

        // Act
        var results = new List<ParsedHttpRequest>();
        var act = async () =>
        {
            await foreach (var request in HttpFileParser.ParseAsync(stream))
            {
                results.Add(request);
            }
        };

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid request line format: 'INVALID_REQUEST_LINE'. Expected: METHOD URL VERSION");
        // The first request should fail, so no results should be collected
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Cancel_When_CancellationToken_Is_Used()
    {
        // Arrange
        const string rawRequests = """
            GET /first HTTP/1.1
            Host: example.com

            ###

            GET /second HTTP/1.1
            Host: example.com

            ###

            GET /third HTTP/1.1
            Host: example.com
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequests));
        var cts = new CancellationTokenSource();

        // Act
        var results = new List<ParsedHttpRequest>();
        var task = Task.Run(async () =>
        {
            await foreach (var request in HttpFileParser.ParseAsync(stream, cts.Token))
            {
                results.Add(request);
                // Cancel after processing the first request
                if (results.Count == 1)
                {
                    await cts.CancelAsync();
                }
            }
        }, cts.Token);

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
        results.Should().HaveCount(1); // Only the first request should have been processed
        results[0].Method.Should().Be(HttpMethod.Get);
        results[0].Url.Should().Be("/first");
    }

    [Fact]
    public async Task ParseFileAsync_Should_Cancel_When_CancellationToken_Is_Used()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        try
        {
            const string fileContent = """
                GET /first HTTP/1.1
                Host: example.com

                ###

                GET /second HTTP/1.1
                Host: example.com
                """;
            await File.WriteAllTextAsync(tempFilePath, fileContent, Encoding.UTF8);

            var cts = new CancellationTokenSource();

            // Act
            var results = new List<ParsedHttpRequest>();
            var task = Task.Run(async () =>
            {
                await foreach (var request in HttpFileParser.ParseFileAsync(tempFilePath, cts.Token))
                {
                    results.Add(request);
                    // Cancel after the first request
                    if (results.Count == 1)
                    {
                        await cts.CancelAsync();
                    }
                }
            }, cts.Token);

            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
            results.Should().HaveCount(1); // Only the first request should have been processed
            results[0].Method.Should().Be(HttpMethod.Get);
            results[0].Url.Should().Be("/first");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}
