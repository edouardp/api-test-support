using AwesomeAssertions;
using System.Text;

namespace PQSoft.HttpFile.UnitTests;

public class ParsedHeaderSemanticEqualsIntegrationTests
{
    #region Individual Header Integration Tests

    [Fact]
    public async Task SemanticEquals_IdenticalHttpRequests_ShouldReturnTrue()
    {
        // Arrange
        var http1 = """
            POST https://api.example.com/users HTTP/1.1
            Content-Type: application/json; charset=utf-8
            Authorization: Bearer token123

            {"name": "John"}
            """;
        
        var http2 = """
            POST https://api.example.com/users HTTP/1.1
            Content-Type: application/json; charset=utf-8
            Authorization: Bearer token123

            {"name": "John"}
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_HeaderNameCaseInsensitive_ShouldReturnTrue()
    {
        // Arrange
        var http1 = """
            GET https://api.example.com/data HTTP/1.1
            content-type: application/json
            authorization: Bearer token123
            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1
            Content-Type: application/json
            Authorization: Bearer token123
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_HeaderValueCaseSensitive_ShouldReturnFalse()
    {
        // Arrange
        var http1 = """
            GET https://api.example.com/data HTTP/1.1
            Content-Type: application/json
            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1
            Content-Type: Application/JSON
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeFalse();
    }

    [Fact]
    public async Task SemanticEquals_ParameterOrderDifferent_ShouldReturnTrue()
    {
        // Arrange
        var http1 = """
            POST https://api.example.com/upload HTTP/1.1
            Content-Type: multipart/form-data; boundary=----WebKitFormBoundary; charset=utf-8
            """;
        
        var http2 = """
            POST https://api.example.com/upload HTTP/1.1
            Content-Type: multipart/form-data; charset=utf-8; boundary=----WebKitFormBoundary
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_HeaderContinuation_ShouldReturnTrue()
    {
        // Arrange - Simulating header continuation (folded headers)
        var http1 = """
            GET https://api.example.com/data HTTP/1.1
            Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1
            Accept:  text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8 
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_ExtraWhitespace_ShouldReturnTrue()
    {
        // Arrange
        var http1 = """
            POST https://api.example.com/users HTTP/1.1
            Content-Type: application/json ; charset=utf-8 
            Authorization: Bearer token123
            """;
        
        var http2 = """
            POST https://api.example.com/users HTTP/1.1
            Content-Type: application/json; charset=utf-8
            Authorization: Bearer token123
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_DifferentParameterValues_ShouldReturnFalse()
    {
        // Arrange
        var http1 = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/json; charset=utf-8
            """;
        
        var http2 = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/json; charset=iso-8859-1
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeFalse();
    }

    [Fact]
    public async Task SemanticEquals_MissingHeader_ShouldReturnFalse()
    {
        // Arrange
        var http1 = """
            GET https://api.example.com/data HTTP/1.1
            Content-Type: application/json
            Authorization: Bearer token123
            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1
            Content-Type: application/json
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeFalse();
    }

    #endregion

    #region Collection Integration Tests

    [Fact]
    public async Task SemanticEquals_HeaderOrderDifferent_ShouldReturnTrue()
    {
        // Arrange
        var http1 = """
            POST https://api.example.com/users HTTP/1.1
            Content-Type: application/json
            Authorization: Bearer token123
            Accept: application/json
            """;
        
        var http2 = """
            POST https://api.example.com/users HTTP/1.1
            Accept: application/json
            Content-Type: application/json
            Authorization: Bearer token123
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_ComplexMultipartHeaders_ShouldReturnTrue()
    {
        // Arrange
        var http1 = """
            POST https://api.example.com/upload HTTP/1.1
            Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW; charset=utf-8
            Content-Length: 1234
            Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9
            """;
        
        var http2 = """
            POST https://api.example.com/upload HTTP/1.1
            Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9
            Content-Length: 1234
            Content-Type: multipart/form-data; charset=utf-8; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_CustomHeaders_ShouldReturnTrue()
    {
        // Arrange
        var http1 = """
            GET https://api.example.com/data HTTP/1.1
            X-API-Key: abc123def456
            X-Request-ID: req-789xyz
            X-Custom-Header: value1; param1=test; param2=data
            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1
            X-Request-ID: req-789xyz
            X-Custom-Header: value1; param2=data; param1=test
            X-API-Key: abc123def456
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_EmptyHeaders_ShouldReturnTrue()
    {
        // Arrange
        var http1 = """
            GET https://api.example.com/data HTTP/1.1

            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1

            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_DuplicateHeaders_ShouldReturnFalse()
    {
        // Arrange
        var http1 = """
            GET https://api.example.com/data HTTP/1.1
            Accept: application/json
            Accept: text/html
            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1
            Accept: application/json
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeFalse();
    }

    [Fact]
    public async Task SemanticEquals_CookieHeaders_ShouldReturnTrue()
    {
        // Arrange
        var http1 = """
            GET https://api.example.com/data HTTP/1.1
            Cookie: sessionId=abc123; userId=456; theme=dark
            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1
            Cookie: sessionId=abc123; userId=456; theme=dark
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_AuthenticationHeaders_ShouldReturnTrue()
    {
        // Arrange
        var http1 = """
            POST https://api.example.com/secure HTTP/1.1
            Authorization: Basic dXNlcjpwYXNzd29yZA==
            WWW-Authenticate: Basic realm="Protected Area"
            """;
        
        var http2 = """
            POST https://api.example.com/secure HTTP/1.1
            WWW-Authenticate: Basic realm="Protected Area"
            Authorization: Basic dXNlcjpwYXNzd29yZA==
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_CacheControlHeaders_ShouldReturnTrue()
    {
        // Arrange
        var http1 = """
            GET https://api.example.com/data HTTP/1.1
            Cache-Control: no-cache, no-store, must-revalidate
            Pragma: no-cache
            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1
            Pragma: no-cache
            Cache-Control: no-cache, no-store, must-revalidate
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    #endregion

    #region Individual Header Comparison Tests

    [Fact]
    public async Task SemanticEquals_IndividualHeaderComparison_ShouldWork()
    {
        // Arrange
        var http1 = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/json; charset=utf-8; boundary=test
            """;
        
        var http2 = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/json; boundary=test; charset=utf-8
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);
        var header1 = request1.Headers.First();
        var header2 = request2.Headers.First();

        // Assert
        header1.SemanticEquals(header2).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_IndividualHeaderDifferent_ShouldReturnFalse()
    {
        // Arrange
        var http1 = """
            GET https://api.example.com/data HTTP/1.1
            Accept: application/json
            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1
            Accept: text/html
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);
        var header1 = request1.Headers.First();
        var header2 = request2.Headers.First();

        // Assert
        header1.SemanticEquals(header2).Should().BeFalse();
    }

    #endregion

    #region Header Continuation Tests

    [Fact]
    public async Task SemanticEquals_HeaderContinuationVsSingleLine_ShouldReturnTrue()
    {
        // Arrange - Real header continuation (folded header) vs single line
        var http1 = """
            GET https://api.example.com/data HTTP/1.1
            Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
             eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ
            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1
            Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9. eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_MultiLineContinuation_ShouldReturnTrue()
    {
        // Arrange - Multiple continuation lines vs single line
        var http1 = """
            POST https://api.example.com/upload HTTP/1.1
            Content-Type: multipart/form-data;
             boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW;
             charset=utf-8
            """;
        
        var http2 = """
            POST https://api.example.com/upload HTTP/1.1
            Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW; charset=utf-8
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_ContinuationWithTabs_ShouldReturnTrue()
    {
        // Arrange - Continuation with tabs vs spaces
        var http1 = """
            GET https://api.example.com/data HTTP/1.1
            User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64)
            	AppleWebKit/537.36
            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1
            User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_AuthorizationContinuation_ShouldReturnTrue()
    {
        // Arrange - Long authorization header with continuation
        var http1 = """
            POST https://api.example.com/secure HTTP/1.1
            Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
             eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.
             SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
            """;
        
        var http2 = """
            POST https://api.example.com/secure HTTP/1.1
            Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9. eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ. SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticEquals_MultipleContinuationHeaders_ShouldReturnTrue()
    {
        // Arrange - Multiple headers with continuations
        var http1 = """
            GET https://api.example.com/data HTTP/1.1
            X-Custom-Header: value1;
             param1=test;
             param2=data
            Accept-Language: en-US,en;q=0.9,
             fr;q=0.8,de;q=0.7
            """;
        
        var http2 = """
            GET https://api.example.com/data HTTP/1.1
            X-Custom-Header: value1; param1=test; param2=data
            Accept-Language: en-US,en;q=0.9, fr;q=0.8,de;q=0.7
            """;

        // Act
        var request1 = await ParseHttpRequest(http1);
        var request2 = await ParseHttpRequest(http2);

        // Assert
        ParsedHeader.SemanticEquals(request1.Headers, request2.Headers).Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static async Task<ParsedHttpRequest> ParseHttpRequest(string httpContent)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(httpContent));
        return await HttpStreamParser.ParseAsync(stream);
    }

    #endregion
}
