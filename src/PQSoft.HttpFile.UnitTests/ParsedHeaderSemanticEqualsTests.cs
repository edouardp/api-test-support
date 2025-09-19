using AwesomeAssertions;

namespace PQSoft.HttpFile.UnitTests;

public class ParsedHeaderSemanticEqualsTests
{
    #region Individual Header SemanticEquals Tests

    [Fact]
    public void SemanticEquals_IdenticalHeaders_ShouldReturnTrue()
    {
        // Arrange
        var header1 = new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string> { { "charset", "utf-8" } });
        var header2 = new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string> { { "charset", "utf-8" } });

        // Act & Assert
        InvokeSemanticEquals(header1, header2).Should().BeTrue();
    }

    [Fact]
    public void SemanticEquals_HeaderNameCaseInsensitive_ShouldReturnTrue()
    {
        // Arrange
        var header1 = new ParsedHeader("content-type", "application/json", new Dictionary<string, string>());
        var header2 = new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>());

        // Act & Assert
        InvokeSemanticEquals(header1, header2).Should().BeTrue();
    }

    [Fact]
    public void SemanticEquals_HeaderValueCaseSensitive_ShouldReturnFalse()
    {
        // Arrange
        var header1 = new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>());
        var header2 = new ParsedHeader("Content-Type", "Application/JSON", new Dictionary<string, string>());

        // Act & Assert
        InvokeSemanticEquals(header1, header2).Should().BeFalse();
    }

    [Fact]
    public void SemanticEquals_WhitespaceInNameAndValue_ShouldReturnTrue()
    {
        // Arrange
        var header1 = new ParsedHeader(" Content-Type ", " application/json ", new Dictionary<string, string>());
        var header2 = new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>());

        // Act & Assert
        InvokeSemanticEquals(header1, header2).Should().BeTrue();
    }

    [Fact]
    public void SemanticEquals_ParameterOrderDifferent_ShouldReturnTrue()
    {
        // Arrange
        var header1 = new ParsedHeader("Content-Type", "text/html", new Dictionary<string, string> 
        { 
            { "charset", "utf-8" }, 
            { "boundary", "abc123" } 
        });
        var header2 = new ParsedHeader("Content-Type", "text/html", new Dictionary<string, string> 
        { 
            { "boundary", "abc123" }, 
            { "charset", "utf-8" } 
        });

        // Act & Assert
        InvokeSemanticEquals(header1, header2).Should().BeTrue();
    }

    [Fact]
    public void SemanticEquals_ParameterValueWhitespace_ShouldReturnTrue()
    {
        // Arrange
        var header1 = new ParsedHeader("Content-Type", "text/html", new Dictionary<string, string> 
        { 
            { "charset", " utf-8 " } 
        });
        var header2 = new ParsedHeader("Content-Type", "text/html", new Dictionary<string, string> 
        { 
            { "charset", "utf-8" } 
        });

        // Act & Assert
        InvokeSemanticEquals(header1, header2).Should().BeTrue();
    }

    [Fact]
    public void SemanticEquals_DifferentParameterCount_ShouldReturnFalse()
    {
        // Arrange
        var header1 = new ParsedHeader("Content-Type", "text/html", new Dictionary<string, string> 
        { 
            { "charset", "utf-8" } 
        });
        var header2 = new ParsedHeader("Content-Type", "text/html", new Dictionary<string, string> 
        { 
            { "charset", "utf-8" }, 
            { "boundary", "abc123" } 
        });

        // Act & Assert
        InvokeSemanticEquals(header1, header2).Should().BeFalse();
    }

    [Fact]
    public void SemanticEquals_MissingParameter_ShouldReturnFalse()
    {
        // Arrange
        var header1 = new ParsedHeader("Content-Type", "text/html", new Dictionary<string, string> 
        { 
            { "charset", "utf-8" } 
        });
        var header2 = new ParsedHeader("Content-Type", "text/html", new Dictionary<string, string> 
        { 
            { "boundary", "abc123" } 
        });

        // Act & Assert
        InvokeSemanticEquals(header1, header2).Should().BeFalse();
    }

    [Fact]
    public void SemanticEquals_DifferentParameterValue_ShouldReturnFalse()
    {
        // Arrange
        var header1 = new ParsedHeader("Content-Type", "text/html", new Dictionary<string, string> 
        { 
            { "charset", "utf-8" } 
        });
        var header2 = new ParsedHeader("Content-Type", "text/html", new Dictionary<string, string> 
        { 
            { "charset", "iso-8859-1" } 
        });

        // Act & Assert
        InvokeSemanticEquals(header1, header2).Should().BeFalse();
    }

    [Fact]
    public void SemanticEquals_NullHeader_ShouldReturnFalse()
    {
        // Arrange
        var header1 = new ParsedHeader("Content-Type", "application/json", new Dictionary<string, string>());

        // Act & Assert
        InvokeSemanticEquals(header1, null).Should().BeFalse();
    }

    [Fact]
    public void SemanticEquals_EmptyParameters_ShouldReturnTrue()
    {
        // Arrange
        var header1 = new ParsedHeader("Authorization", "Bearer token123", new Dictionary<string, string>());
        var header2 = new ParsedHeader("Authorization", "Bearer token123", new Dictionary<string, string>());

        // Act & Assert
        InvokeSemanticEquals(header1, header2).Should().BeTrue();
    }

    #endregion

    #region Collection SemanticEquals Tests

    [Fact]
    public void SemanticEquals_IdenticalCollections_ShouldReturnTrue()
    {
        // Arrange
        var headers1 = new List<ParsedHeader>
        {
            new("Content-Type", "application/json", new Dictionary<string, string> { { "charset", "utf-8" } }),
            new("Authorization", "Bearer token", new Dictionary<string, string>())
        };
        var headers2 = new List<ParsedHeader>
        {
            new("Content-Type", "application/json", new Dictionary<string, string> { { "charset", "utf-8" } }),
            new("Authorization", "Bearer token", new Dictionary<string, string>())
        };

        // Act & Assert
        ParsedHeader.SemanticEquals(headers1, headers2).Should().BeTrue();
    }

    [Fact]
    public void SemanticEquals_DifferentOrder_ShouldReturnTrue()
    {
        // Arrange
        var headers1 = new List<ParsedHeader>
        {
            new("Content-Type", "application/json", new Dictionary<string, string>()),
            new("Authorization", "Bearer token", new Dictionary<string, string>())
        };
        var headers2 = new List<ParsedHeader>
        {
            new("Authorization", "Bearer token", new Dictionary<string, string>()),
            new("Content-Type", "application/json", new Dictionary<string, string>())
        };

        // Act & Assert
        ParsedHeader.SemanticEquals(headers1, headers2).Should().BeTrue();
    }

    [Fact]
    public void SemanticEquals_DifferentCount_ShouldReturnFalse()
    {
        // Arrange
        var headers1 = new List<ParsedHeader>
        {
            new("Content-Type", "application/json", new Dictionary<string, string>())
        };
        var headers2 = new List<ParsedHeader>
        {
            new("Content-Type", "application/json", new Dictionary<string, string>()),
            new("Authorization", "Bearer token", new Dictionary<string, string>())
        };

        // Act & Assert
        ParsedHeader.SemanticEquals(headers1, headers2).Should().BeFalse();
    }

    [Fact]
    public void SemanticEquals_EmptyCollections_ShouldReturnTrue()
    {
        // Arrange
        var headers1 = new List<ParsedHeader>();
        var headers2 = new List<ParsedHeader>();

        // Act & Assert
        ParsedHeader.SemanticEquals(headers1, headers2).Should().BeTrue();
    }

    [Fact]
    public void SemanticEquals_OneEmptyCollection_ShouldReturnFalse()
    {
        // Arrange
        var headers1 = new List<ParsedHeader>();
        var headers2 = new List<ParsedHeader>
        {
            new("Content-Type", "application/json", new Dictionary<string, string>())
        };

        // Act & Assert
        ParsedHeader.SemanticEquals(headers1, headers2).Should().BeFalse();
    }

    [Fact]
    public void SemanticEquals_CaseInsensitiveHeaderNames_ShouldReturnTrue()
    {
        // Arrange
        var headers1 = new List<ParsedHeader>
        {
            new("content-type", "application/json", new Dictionary<string, string>()),
            new("AUTHORIZATION", "Bearer token", new Dictionary<string, string>())
        };
        var headers2 = new List<ParsedHeader>
        {
            new("Content-Type", "application/json", new Dictionary<string, string>()),
            new("authorization", "Bearer token", new Dictionary<string, string>())
        };

        // Act & Assert
        ParsedHeader.SemanticEquals(headers1, headers2).Should().BeTrue();
    }

    [Fact]
    public void SemanticEquals_HeaderContinuationEquivalent_ShouldReturnTrue()
    {
        // Arrange - Simulating header continuation (folded header vs single line)
        var headers1 = new List<ParsedHeader>
        {
            new("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8", new Dictionary<string, string>())
        };
        var headers2 = new List<ParsedHeader>
        {
            new("Accept", " text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8 ", new Dictionary<string, string>())
        };

        // Act & Assert
        ParsedHeader.SemanticEquals(headers1, headers2).Should().BeTrue();
    }

    [Fact]
    public void SemanticEquals_ComplexParameterComparison_ShouldReturnTrue()
    {
        // Arrange
        var headers1 = new List<ParsedHeader>
        {
            new("Content-Type", "multipart/form-data", new Dictionary<string, string> 
            { 
                { "boundary", "----WebKitFormBoundary7MA4YWxkTrZu0gW" },
                { "charset", "utf-8" }
            })
        };
        var headers2 = new List<ParsedHeader>
        {
            new("Content-Type", "multipart/form-data", new Dictionary<string, string> 
            { 
                { "charset", " utf-8 " },
                { "boundary", "----WebKitFormBoundary7MA4YWxkTrZu0gW" }
            })
        };

        // Act & Assert
        ParsedHeader.SemanticEquals(headers1, headers2).Should().BeTrue();
    }

    [Fact]
    public void SemanticEquals_DuplicateHeaders_ShouldReturnFalse()
    {
        // Arrange
        var headers1 = new List<ParsedHeader>
        {
            new("Content-Type", "application/json", new Dictionary<string, string>()),
            new("Content-Type", "application/json", new Dictionary<string, string>())
        };
        var headers2 = new List<ParsedHeader>
        {
            new("Content-Type", "application/json", new Dictionary<string, string>())
        };

        // Act & Assert
        ParsedHeader.SemanticEquals(headers1, headers2).Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to invoke the private SemanticEquals method using reflection
    /// </summary>
    private static bool InvokeSemanticEquals(ParsedHeader header1, ParsedHeader? header2)
    {
        var method = typeof(ParsedHeader).GetMethod("SemanticEquals", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (bool)method!.Invoke(header1, new object?[] { header2 })!;
    }

    #endregion
}
