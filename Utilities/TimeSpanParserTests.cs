using Xunit;
using WebLynx.Utilities;

namespace WebLynx.Utilities.Tests;

public class TimeSpanParserTests
{
    [Fact]
    public void Parse_EmptyString_ReturnsNull()
    {

        // Arrange
        string text = "";

        // Act
        var result = TimeSpanParser.Parse(text);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_TimeWithoutFractionalSeconds_ReturnsParsedValue()
    {

        // Arrange
        string text = "5";

        // Act
        var result = TimeSpanParser.Parse(text);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), result);
    }

    [Fact]
    public void Parse_TimeWithFractionalSeconds_ReturnsParsedValue()
    {

        // Arrange
        string text = "5.234";

        // Act
        var result = TimeSpanParser.Parse(text);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5.234), result);
    }
    
    [Fact]
    public void Parse_TimeWithMinuteAndFractionalSeconds_ReturnsParsedValue()
    {

        // Arrange
        string text = "1:05.234";

        // Act
        var result = TimeSpanParser.Parse(text);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(65.234), result);
    }

    [Fact]
    public void Parse_NonTimeSpanString_ReturnsNull()
    {

        // Arrange
        string text = "foobar";

        // Act
        var result = TimeSpanParser.Parse(text);

        // Assert
        Assert.Null(result);
    }
}
