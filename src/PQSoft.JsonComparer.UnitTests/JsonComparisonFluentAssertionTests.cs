using System.Text.Json;
using FluentAssertions;
using TestSupport.Json;

namespace TestSupport.Json.UnitTests;

public class JsonComparisonFluentAssertionTests
{
    [Fact]
    public void BeEquivalentToJson_ShouldPass_ForMatchingJson()
    {
        // Arrange
        string expected = """
            {
                "name": "Alice",
                "age": 30,
                "active": true
            }
            """;

        string actual = """
            {
                "name": "Alice",
                "age": 30,
                "active": true
            }
            """;

        // Act and Assert
        actual.AsJsonString().Should().FullyMatch(expected, "because the JSON response should match the expected structure");
    }

    [Fact]
    public void BeEquivalentToJson_ShouldFail_ForNonMatchingJson()
    {
        // Arrange
        string expected = """
            {
                "name": "Alice",
                "age": 30,
                "active": true
            }
            """;

        // Actual JSON has a mismatch in the "name" field.
        string actual = """
            {
                "name": "Bob",
                "age": 30,
                "active": true
            }
            """;

        // Act
        Action act = () => actual.AsJsonString().Should().FullyMatch(expected);

        // Assert
        act.Should().Throw<Exception>()
           .Where(e => e.Message.Contains("String mismatch") && e.Message.Contains("$.name"));
    }

    [Fact]
    public void ContainSubsetJson_ShouldPass_WhenActualContainsSubset()
    {
        // Arrange: expected JSON is a subset of actual JSON.
        string expected = """ {"name": "Alice"} """;

        string actual = """
            {
                "name": "Alice",
                "age": 30,
                "active": true
            }
            """;

        // Act and Assert
        actual.AsJsonString().Should().ContainSubset(expected, "because the actual JSON contains all expected properties");
    }

    [Fact]
    public void ContainSubsetJson_ShouldFail_WhenSubsetMismatchOccurs()
    {
        // Arrange: expected JSON contains a property value that does not match.
        string expected = """
            {
                "name": "Alice",
                "age": 30
            }
            """;

        string actual = """
            {
                "name": "Alice",
                "age": 25,
                "active": true
            }
            """;

        // Act
        Action act = () => actual.AsJsonString().Should().ContainSubset(expected);

        // Assert
        act.Should().Throw<Exception>()
           .Where(e => e.Message.Contains("$.age") && e.Message.Contains("Number mismatch"));
    }

    public class JsonComparisonWithExtractionTests
    {
        [Fact]
        public void BeEquivalentToJson_ShouldExtractTokenValues()
        {
            // Arrange
            // Expected JSON with a token. The token is wrapped in quotes.
            string expected = """
                {
                    "JobID": [[JOBID]],
                    "status": "pending"
                }
                """;
            // Actual JSON returns a number for the JobID.
            string actual = """
                {
                    "JobID": 12345,
                    "status": "pending"
                }
                """;

            // Act
            var assertions = actual.AsJsonString().Should().FullyMatch(expected, "because we need to extract the JobID");

            // Well this is a nasty hack ... but it works
            var extractedValues = assertions.And.ExtractedValues;

            // Assert
            // Access the extracted values through the ExtractedValues property.
            extractedValues.Should().ContainKey("JOBID");
            var jobIdElement = extractedValues["JOBID"];
            jobIdElement.ValueKind.Should().Be(JsonValueKind.Number);
            jobIdElement.GetRawText().Should().Be("12345");
        }
    }
}
