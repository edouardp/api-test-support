using AwesomeAssertions;

namespace PQSoft.HttpFile.UnitTests;

public class HeaderVariableExtractorTests
{
    [Fact]
    public void ExtractVariables_SingleTokenAtEnd_ExtractsCorrectly()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Location", "/api/users/[[USER_ID]]", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Location", "/api/users/12345", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().ContainKey("USER_ID");
        variables["USER_ID"].Should().Be("12345");
    }

    [Fact]
    public void ExtractVariables_SingleTokenAtBeginning_ExtractsCorrectly()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Authorization", "[[TOKEN_TYPE]] abc123", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Authorization", "Bearer abc123", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().ContainKey("TOKEN_TYPE");
        variables["TOKEN_TYPE"].Should().Be("Bearer");
    }

    [Fact]
    public void ExtractVariables_SingleTokenInMiddle_ExtractsCorrectly()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Content-Type", "application/[[FORMAT]]; charset=utf-8", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Content-Type", "application/json; charset=utf-8", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().ContainKey("FORMAT");
        variables["FORMAT"].Should().Be("json");
    }

    [Fact]
    public void ExtractVariables_EntireValueIsToken_ExtractsCorrectly()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("X-Request-ID", "[[REQUEST_ID]]", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("X-Request-ID", "REQ-12345-ABC", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().ContainKey("REQUEST_ID");
        variables["REQUEST_ID"].Should().Be("REQ-12345-ABC");
    }

    [Fact]
    public void ExtractVariables_MultipleTokensInSameHeader_ExtractsAll()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Custom", "[[PREFIX]]-[[MIDDLE]]-[[SUFFIX]]", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Custom", "ABC-123-XYZ", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().HaveCount(3);
        variables["PREFIX"].Should().Be("ABC");
        variables["MIDDLE"].Should().Be("123");
        variables["SUFFIX"].Should().Be("XYZ");
    }

    [Fact]
    public void ExtractVariables_TokensInMultipleHeaders_ExtractsAll()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Location", "/api/users/[[USER_ID]]", new Dictionary<string, string>()),
            new ParsedHeader("X-Request-ID", "[[REQUEST_ID]]", new Dictionary<string, string>()),
            new ParsedHeader("Authorization", "Bearer [[TOKEN]]", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Location", "/api/users/12345", new Dictionary<string, string>()),
            new ParsedHeader("X-Request-ID", "REQ-ABC-123", new Dictionary<string, string>()),
            new ParsedHeader("Authorization", "Bearer secret-token", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().HaveCount(3);
        variables["USER_ID"].Should().Be("12345");
        variables["REQUEST_ID"].Should().Be("REQ-ABC-123");
        variables["TOKEN"].Should().Be("secret-token");
    }

    [Fact]
    public void ExtractVariables_TokenInHeaderParameter_ExtractsCorrectly()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>
            {
                ["charset"] = "[[CHARSET]]"
            })
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>
            {
                ["charset"] = "utf-8"
            })
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().ContainKey("CHARSET");
        variables["CHARSET"].Should().Be("utf-8");
    }

    [Fact]
    public void ExtractVariables_TokenInParameterValue_ExtractsCorrectly()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Content-Disposition", "attachment", new Dictionary<string, string>
            {
                ["filename"] = "report-[[DATE]].pdf"
            })
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Content-Disposition", "attachment", new Dictionary<string, string>
            {
                ["filename"] = "report-2024-01-15.pdf"
            })
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().ContainKey("DATE");
        variables["DATE"].Should().Be("2024-01-15");
    }

    [Fact]
    public void ExtractVariables_MissingExpectedHeader_SkipsExtraction()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Location", "/api/users/[[USER_ID]]", new Dictionary<string, string>()),
            new ParsedHeader("Missing-Header", "[[MISSING]]", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Location", "/api/users/12345", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().HaveCount(1);
        variables.Should().ContainKey("USER_ID");
        variables.Should().NotContainKey("MISSING");
    }

    [Fact]
    public void ExtractVariables_MissingExpectedParameter_SkipsExtraction()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>
            {
                ["charset"] = "[[CHARSET]]",
                ["missing"] = "[[MISSING]]"
            })
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>
            {
                ["charset"] = "utf-8"
            })
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().HaveCount(1);
        variables.Should().ContainKey("CHARSET");
        variables.Should().NotContainKey("MISSING");
    }

    [Fact]
    public void ExtractVariables_NoTokensInHeaders_ReturnsEmptyDictionary()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>()),
            new ParsedHeader("Authorization", "Bearer token", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>()),
            new ParsedHeader("Authorization", "Bearer token", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().BeEmpty();
    }

    [Fact]
    public void ExtractVariables_EmptyHeaderCollections_ReturnsEmptyDictionary()
    {
        // Arrange
        var expectedHeaders = Array.Empty<ParsedHeader>();
        var actualHeaders = Array.Empty<ParsedHeader>();

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().BeEmpty();
    }

    [Fact]
    public void ExtractVariables_CaseInsensitiveHeaderNames_ExtractsCorrectly()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("content-type", "application/[[FORMAT]]", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().ContainKey("FORMAT");
        variables["FORMAT"].Should().Be("json");
    }

    [Fact]
    public void ExtractVariables_ComplexRealWorldScenario_ExtractsAllVariables()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Location", "https://api.example.com/v[[VERSION]]/users/[[USER_ID]]", new Dictionary<string, string>()),
            new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>
            {
                ["charset"] = "[[CHARSET]]"
            }),
            new ParsedHeader("X-Rate-Limit", "[[LIMIT]]/[[WINDOW]]", new Dictionary<string, string>()),
            new ParsedHeader("Set-Cookie", "session=[[SESSION_ID]]; Path=/; HttpOnly", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Location", "https://api.example.com/v2/users/abc-123-def", new Dictionary<string, string>()),
            new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>
            {
                ["charset"] = "utf-8"
            }),
            new ParsedHeader("X-Rate-Limit", "1000/hour", new Dictionary<string, string>()),
            new ParsedHeader("Set-Cookie", "session=xyz789; Path=/; HttpOnly", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().HaveCount(6);
        variables["VERSION"].Should().Be("2");
        variables["USER_ID"].Should().Be("abc-123-def");
        variables["CHARSET"].Should().Be("utf-8");
        variables["LIMIT"].Should().Be("1000");
        variables["WINDOW"].Should().Be("hour");
        variables["SESSION_ID"].Should().Be("xyz789");
    }

    [Fact]
    public void ExtractVariables_TokenWithSpecialRegexCharacters_ExtractsCorrectly()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Custom", "prefix.[[TOKEN]].suffix", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Custom", "prefix.value-with-dots.and.dashes.suffix", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().ContainKey("TOKEN");
        variables["TOKEN"].Should().Be("value-with-dots.and.dashes");
    }

    [Fact]
    public void ExtractVariables_OverlappingTokenNames_ExtractsLatest()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Header1", "[[TOKEN]]", new Dictionary<string, string>()),
            new ParsedHeader("Header2", "[[TOKEN]]", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Header1", "first-value", new Dictionary<string, string>()),
            new ParsedHeader("Header2", "second-value", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().HaveCount(1);
        variables["TOKEN"].Should().Be("second-value");
    }

    [Fact]
    public void ExtractVariables_InvalidTokenPattern_IgnoresInvalidTokens()
    {
        // Arrange
        var expectedHeaders = new[]
        {
            new ParsedHeader("Custom", "[[VALID_TOKEN]] [INVALID] [[ANOTHER_VALID]]", new Dictionary<string, string>())
        };
        var actualHeaders = new[]
        {
            new ParsedHeader("Custom", "value1 [INVALID] value2", new Dictionary<string, string>())
        };

        // Act
        var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);

        // Assert
        variables.Should().HaveCount(2);
        variables["VALID_TOKEN"].Should().Be("value1");
        variables["ANOTHER_VALID"].Should().Be("value2");
    }
}
