using Xunit;
using WebLynx.Utilities;

namespace WebLynx.Utilities.Tests;

public class FixedWidthParserTests
{
     [Fact]
    public void Parse_ValidString_ReturnsParsedValue()
    {
        // Arrange
        string text = "12345";
        int startIndex = 1;
        int length = 3;
        Func<string, int> converter = s => int.Parse(s);

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter);

        // Assert
        Assert.Equal(234, result);
    }

    [Fact]
    public void Parse_ValidStringWithDefaultValue_ReturnsParsedValue()
    {
        // Arrange
        string text = "Hello World";
        int startIndex = 6;
        int length = 5;
        Func<string, string> converter = s => s.Trim();
        string defaultValue = "default";

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter, defaultValue);

        // Assert
        Assert.Equal("World", result);
    }

    [Fact]
    public void Parse_ValidStringWithDefaultValue_ReturnsTransformedSecondToken()
    {
        // Arrange
        string text = "Hello Sunny World";
        int startIndex = 6;
        int length = 5;
        Func<string, string> converter = s => s.Trim().ToUpper();
        string? defaultValue = null;

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter, defaultValue);

        // Assert
        Assert.Equal("SUNNY", result);
    }

    [Fact]
    public void Parse_ValidStringWithDefaultValueAndTooLongLength_ReturnsTransformedThirdToken()
    {
        // Arrange
        string text = "Hello Sunny World";
        int startIndex = 12;
        int length = 20;
        Func<string, string> converter = s => s.Trim().ToUpper();
        string? defaultValue = null;

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter, defaultValue);

        // Assert
        Assert.Equal("WORLD", result);
    }

    [Fact]
    public void Parse_ValidStringWithDefaultValueAndExactLength_ReturnsTransformedThirdToken()
    {
        // Arrange
        string text = "Hello Sunny World";
        int startIndex = 12;
        int length = 5;
        Func<string, string> converter = s => s.Trim().ToUpper();
        string? defaultValue = null;

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter, defaultValue);

        // Assert
        Assert.Equal("WORLD", result);
    }


    [Fact]
    public void Parse_StringTooShort_ReturnsDefaultValue()
    {
        // Arrange
        string text = "1";
        int startIndex = 2;
        int length = 5;
        Func<string, int> converter = s => int.Parse(s);
        int defaultValue = -1;

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void Parse_StringTooShortNoDefault_ReturnsDefaultOfType()
    {
        // Arrange
        string text = "1";
        int startIndex = 1;
        int length = 5;
        Func<string, int> converter = s => int.Parse(s);

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter);

        // Assert
        Assert.Equal(0, result); // default(int) is 0
    }

    [Fact]
    public void Parse_ExactLengthMatch_ReturnsParsedValue()
    {
        // Arrange
        string text = "12345";
        int startIndex = 0;
        int length = 5;
        Func<string, string> converter = s => s;

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter);

        // Assert
        Assert.Equal("12345", result);
    }

    [Fact]
    public void Parse_LengthExceedsRemainingString_TruncatesToAvailableLength()
    {
        // Arrange
        string text = "12345";
        int startIndex = 2;
        int length = 10; // More than remaining characters
        Func<string, string> converter = s => s;

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter);

        // Assert
        Assert.Equal("345", result); // Only 3 characters remaining from index 2
    }

    [Fact]
    public void Parse_StartIndexAtEndOfString_ReturnsDefaultValue()
    {
        // Arrange
        string text = "12345";
        int startIndex = 5; // After end of the input string
        int length = 1;
        Func<string, int> converter = s => int.Parse(s);
        int defaultValue = 999;

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsDefaultValue()
    {
        // Arrange
        string text = "";
        int startIndex = 0;
        int length = 1;
        Func<string, string> converter = s => s;
        string defaultValue = "empty";

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void Parse_ZeroLength_ReturnsEmptyString()
    {
        // Arrange
        string text = "12345";
        int startIndex = 2;
        int length = 0;
        Func<string, string> converter = s => s;

        // Act && Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => FixedWidthParser.Parse(text, startIndex, length, converter));
    }

    [Fact]
    public void Parse_WithBooleanConverter_ReturnsBooleanValue()
    {
        // Arrange
        string text = "true false";
        int startIndex = 0;
        int length = 4;
        Func<string, bool> converter = s => bool.Parse(s);

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Parse_WithDecimalConverter_ReturnsDecimalValue()
    {
        // Arrange
        string text = "123.456";
        int startIndex = 0;
        int length = 7;
        Func<string, decimal> converter = s => decimal.Parse(s);

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter);

        // Assert
        Assert.Equal(123.456m, result);
    }

    [Fact]
    public void Parse_WithCustomConverter_ReturnsConvertedValue()
    {
        // Arrange
        string text = "  hello  ";
        int startIndex = 0;
        int length = 9;
        Func<string, string> converter = s => s.Trim().ToUpper();

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter);

        // Assert
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void Parse_WithNullableType_ReturnsNullableValue()
    {
        // Arrange
        string text = "123";
        int startIndex = 0;
        int length = 3;
        Func<string, int?> converter = s => int.Parse(s);

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter);

        // Assert
        Assert.Equal(123, result);
    }

    [Fact]
    public void Parse_WithNullableTypeAndDefault_ReturnsDefaultWhenStringTooShort()
    {
        // Arrange
        string text = "1";
        int startIndex = 1;
        int length = 5;
        Func<string, int?> converter = s => int.Parse(s);
        int? defaultValue = null;

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter, defaultValue);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithComplexObjectConverter_ReturnsObject()
    {
        // Arrange
        string text = "John,25";
        int startIndex = 0;
        int length = 7;
        Func<string, Person> converter = s => 
        {
            var parts = s.Split(',');
            return new Person { Name = parts[0], Age = int.Parse(parts[1]) };
        };

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.Name);
        Assert.Equal(25, result.Age);
    }

    [Fact]
    public void Parse_WithComplexObjectConverterAndDefault_ReturnsDefaultWhenStringTooShort()
    {
        // Arrange
        string text = "J";
        int startIndex = 1;
        int length = 10;
        Func<string, Person> converter = s => 
        {
            var parts = s.Split(',');
            return new Person { Name = parts[0], Age = int.Parse(parts[1]) };
        };
        Person defaultValue = new Person { Name = "Unknown", Age = 0 };

        // Act
        var result = FixedWidthParser.Parse(text, startIndex, length, converter, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    // Helper class for testing complex object conversion
    private class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is Person other)
            {
                return Name == other.Name && Age == other.Age;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Age);
        }
    }

    //FixedWidthParser.TrimParse(line, 0, 3)
    [Fact]
    public void TrimParse_ReturnsTrimmedValue()
    {
        // Arrange
        string text = "        ";
        int startIndex = 0;
        int length = 3;

        // Act
        var result = FixedWidthParser.TrimParse(text, startIndex, length);

        // Assert
        Assert.Equal("", result);
    }

}
