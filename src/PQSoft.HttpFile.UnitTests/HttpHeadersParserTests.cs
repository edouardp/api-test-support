using FluentAssertions;

namespace TestSupport.HttpFile.UnitTests;

public class HttpHeadersParserTests
{
    [Fact]
    public void ParseHeader_Should_Parse_ContentTypeHeader_With_Parameters()
    {
        // Arrange
        string rawHeader = "Content-Type: application/json; charset=utf-8";

        // Act
        var result = HttpHeadersParser.ParseHeader(rawHeader);

        // Assert
        result.Name.Should().Be("Content-Type");
        result.Value.Should().Be("application/json");
        result.Parameters.Should().ContainKey("charset").WhoseValue.Should().Be("utf-8");
    }

    [Fact]
    public void ParseHeader_Should_Parse_CustomHeader_With_Parameters()
    {
        // Arrange
        string rawHeader = "Custom-Header: xyz; something=blah";

        // Act
        var result = HttpHeadersParser.ParseHeader(rawHeader);

        // Assert
        result.Name.Should().Be("Custom-Header");
        result.Value.Should().Be("xyz");
        result.Parameters.Should().ContainKey("something").WhoseValue.Should().Be("blah");
    }

    [Fact]
    public void ParseHeader_Should_Handle_Headers_Without_Parameters()
    {
        // Arrange
        string rawHeader = "Simple-Header: value";

        // Act
        var result = HttpHeadersParser.ParseHeader(rawHeader);

        // Assert
        result.Name.Should().Be("Simple-Header");
        result.Value.Should().Be("value");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void ParseHeader_Should_ThrowException_When_Header_Is_Invalid()
    {
        // Arrange
        string rawHeader = "InvalidHeaderWithoutColon";

        // Act
        Action act = () => HttpHeadersParser.ParseHeader(rawHeader);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Invalid header format, missing ':' separator.");
    }

    [Fact]
    public void ParseHeader_Should_Handle_Empty_Parameter_Value()
    {
        // Arrange
        string rawHeader = "Custom-Header: value; parameter=";

        // Act
        var result = HttpHeadersParser.ParseHeader(rawHeader);

        // Assert
        result.Name.Should().Be("Custom-Header");
        result.Value.Should().Be("value");
        result.Parameters.Should().ContainKey("parameter").WhoseValue.Should().Be("");
    }

    [Fact]
    public void ToString_Should_Return_Correct_Format_For_Header_Without_Parameters()
    {
        // Arrange: A header with no parameters
        var header = new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>());

        // Act
        var result = header.ToString();

        // Assert
        result.Should().Be("Content-Type: application/json");
    }

    [Fact]
    public void ToString_Should_Return_Correct_Format_For_Header_With_Parameters()
    {
        // Arrange: A header with parameters
        var parameters = new Dictionary<string, string>
        {
            { "charset", "utf-8" },
            { "boundary", "12345" }
        };
        var header = new ParsedHeader("Content-Type", "multipart/form-data", parameters);

        // Act
        var result = header.ToString();

        // Assert
        result.Should().Be("Content-Type: multipart/form-data; charset=utf-8; boundary=12345");
    }

    [Fact]
    public void ToString_Should_Return_Correct_Format_For_Header_With_Single_Parameter()
    {
        // Arrange: A header with a single parameter
        var parameters = new Dictionary<string, string>
        {
            { "charset", "utf-8" }
        };
        var header = new ParsedHeader("Content-Type", "text/html", parameters);

        // Act
        var result = header.ToString();

        // Assert
        result.Should().Be("Content-Type: text/html; charset=utf-8");
    }

    [Fact]
    public void ToString_Should_Handle_Empty_Header_Value()
    {
        // Arrange: A header with an empty value
        var header = new ParsedHeader("Content-Type", "", []);

        // Act
        var result = header.ToString();

        // Assert
        result.Should().Be("Content-Type: ");
    }
}



