using System.Text.Json;
using Xunit;

namespace PQSoft.JsonComparer.Tests
{
    public class JsonComparerTests
    {
        [Fact]
        public void Compare_IdenticalJsonObjects_ReturnsEqual()
        {
            // Arrange
            var comparer = new JsonComparer();
            var json1 = @"{""name"":""John"",""age"":30,""city"":""New York""}";
            var json2 = @"{""name"":""John"",""age"":30,""city"":""New York""}";

            // Act
            var result = comparer.Compare(json1, json2);

            // Assert
            Assert.True(result.AreEqual);
            Assert.Empty(result.Differences);
        }

        [Fact]
        public void Compare_DifferentValues_ReturnsNotEqual()
        {
            // Arrange
            var comparer = new JsonComparer();
            var json1 = @"{""name"":""John"",""age"":30,""city"":""New York""}";
            var json2 = @"{""name"":""John"",""age"":31,""city"":""New York""}";

            // Act
            var result = comparer.Compare(json1, json2);

            // Assert
            Assert.False(result.AreEqual);
            Assert.Single(result.Differences);
            Assert.Equal("$.age", result.Differences[0].Path);
            Assert.Equal(DifferenceType.ValueMismatch, result.Differences[0].DifferenceType);
        }

        [Fact]
        public void Compare_MissingProperty_ReturnsNotEqual()
        {
            // Arrange
            var comparer = new JsonComparer();
            var json1 = @"{""name"":""John"",""age"":30,""city"":""New York""}";
            var json2 = @"{""name"":""John"",""city"":""New York""}";

            // Act
            var result = comparer.Compare(json1, json2);

            // Assert
            Assert.False(result.AreEqual);
            Assert.Single(result.Differences);
            Assert.Equal("$.age", result.Differences[0].Path);
            Assert.Equal(DifferenceType.MissingProperty, result.Differences[0].DifferenceType);
        }

        [Fact]
        public void Compare_NestedObjects_ReturnsCorrectDifferences()
        {
            // Arrange
            var comparer = new JsonComparer();
            var json1 = @"{""person"":{""name"":""John"",""age"":30,""address"":{""city"":""New York"",""zip"":""10001""}}}";
            var json2 = @"{""person"":{""name"":""John"",""age"":30,""address"":{""city"":""Boston"",""zip"":""10001""}}}";

            // Act
            var result = comparer.Compare(json1, json2);

            // Assert
            Assert.False(result.AreEqual);
            Assert.Single(result.Differences);
            Assert.Equal("$.person.address.city", result.Differences[0].Path);
            Assert.Equal(DifferenceType.ValueMismatch, result.Differences[0].DifferenceType);
        }
    }
}
