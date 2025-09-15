using System.Text;

namespace PQSoft.HttpFile.UnitTests;

public class HttpLineSplitterTests
{
    [Fact]
    public async Task HttpLineSplitter_Should_Split_On_Line_Separator()
    {
        // Arrange
        const string content = """
                               GET /api/users HTTP/1.1
                               Host: example.com

                               ###

                               POST /api/users HTTP/1.1
                               Host: example.com
                               Content-Type: application/json

                               {"name": "John"}

                               ###

                               DELETE /api/users/1 HTTP/1.1
                               Host: example.com
                               """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var splitter = new HttpLineSplitter(stream);

        // Act
        var results = new List<string>();
        await foreach (var segmentStream in splitter)
        {
            using var reader = new StreamReader(segmentStream, Encoding.UTF8);
            var segmentText = await reader.ReadToEndAsync();
            results.Add(segmentText.Trim());
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.StartsWith("GET /api/users HTTP/1.1", results[0]);
        Assert.StartsWith("POST /api/users HTTP/1.1", results[1]);
        Assert.StartsWith("DELETE /api/users/1 HTTP/1.1", results[2]);
    }

    [Fact]
    public async Task HttpLineSplitter_Should_Handle_Separator_With_Comments()
    {
        // Arrange
        const string content = """
                               GET /api/test HTTP/1.1
                               Host: example.com

                               ### This is a comment

                               POST /api/test HTTP/1.1
                               Host: example.com
                               """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var splitter = new HttpLineSplitter(stream);

        // Act
        var results = new List<string>();
        await foreach (var segmentStream in splitter)
        {
            using var reader = new StreamReader(segmentStream, Encoding.UTF8);
            var segmentText = await reader.ReadToEndAsync();
            results.Add(segmentText.Trim());
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.StartsWith("GET /api/test HTTP/1.1", results[0]);
        Assert.StartsWith("POST /api/test HTTP/1.1", results[1]);
    }

    [Fact]
    public async Task HttpLineSplitter_Should_Skip_Empty_Sections()
    {
        // Arrange
        const string content = """
                               ###

                               GET /api/test HTTP/1.1
                               Host: example.com

                               ###

                               ###

                               POST /api/test HTTP/1.1
                               Host: example.com

                               ###
                               """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var splitter = new HttpLineSplitter(stream);

        // Act
        var results = new List<string>();
        await foreach (var segmentStream in splitter)
        {
            using var reader = new StreamReader(segmentStream, Encoding.UTF8);
            var segmentText = await reader.ReadToEndAsync();
            results.Add(segmentText.Trim());
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.StartsWith("GET /api/test HTTP/1.1", results[0]);
        Assert.StartsWith("POST /api/test HTTP/1.1", results[1]);
    }

    [Fact]
    public async Task HttpLineSplitter_Should_Handle_No_Separators()
    {
        // Arrange
        const string content = """
                               GET /api/test HTTP/1.1
                               Host: example.com
                               Content-Type: application/json

                               {"test": "data"}
                               """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var splitter = new HttpLineSplitter(stream);

        // Act
        var results = new List<string>();
        await foreach (var segmentStream in splitter)
        {
            using var reader = new StreamReader(segmentStream, Encoding.UTF8);
            var segmentText = await reader.ReadToEndAsync();
            results.Add(segmentText.Trim());
        }

        // Assert
        Assert.Single(results);
        Assert.StartsWith("GET /api/test HTTP/1.1", results[0]);
        Assert.Contains("application/json", results[0]);
    }

    [Fact]
    public async Task HttpLineSplitter_Should_Handle_Empty_Input()
    {
        // Arrange
        var stream = new MemoryStream();
        var splitter = new HttpLineSplitter(stream);

        // Act
        var results = new List<string>();
        await foreach (var segmentStream in splitter)
        {
            using var reader = new StreamReader(segmentStream, Encoding.UTF8);
            var segmentText = await reader.ReadToEndAsync();
            results.Add(segmentText);
        }

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void HttpLineSplitter_Should_Throw_When_Stream_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HttpLineSplitter(null!));
    }

    [Fact]
    public async Task HttpLineSplitter_Should_Cancel_When_CancellationToken_Is_Used()
    {
        // Arrange
        const string content = """
                               GET /api/test1 HTTP/1.1
                               Host: example.com

                               ###

                               GET /api/test2 HTTP/1.1
                               Host: example.com

                               ###

                               GET /api/test3 HTTP/1.1
                               Host: example.com
                               """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var cts = new CancellationTokenSource();
        var splitter = new HttpLineSplitter(stream);

        // Act
        var results = new List<string>();
        var task = Task.Run(async () =>
        {
            await foreach (var segmentStream in splitter.WithCancellation(cts.Token))
            {
                using var reader = new StreamReader(segmentStream, Encoding.UTF8);
                var segmentText = await reader.ReadToEndAsync(cts.Token);
                results.Add(segmentText);

                // Cancel after the first segment
                if (results.Count == 1)
                {
                    await cts.CancelAsync();
                }
            }
        }, cts.Token);

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
        Assert.Single(results); // Only the first segment should have been processed
    }

    [Fact]
    public async Task HttpLineSplitter_Should_Handle_Custom_Separator()
    {
        // Arrange
        const string content = """
                               GET /api/test1 HTTP/1.1
                               Host: example.com

                               ---

                               GET /api/test2 HTTP/1.1
                               Host: example.com
                               """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var splitter = new HttpLineSplitter(stream, "---");

        // Act
        var results = new List<string>();
        await foreach (var segmentStream in splitter)
        {
            using var reader = new StreamReader(segmentStream, Encoding.UTF8);
            var segmentText = await reader.ReadToEndAsync();
            results.Add(segmentText.Trim());
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.StartsWith("GET /api/test1 HTTP/1.1", results[0]);
        Assert.StartsWith("GET /api/test2 HTTP/1.1", results[1]);
    }
}
