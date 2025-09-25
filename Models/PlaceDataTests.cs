using Xunit;
using WebLynx.Models;

namespace WebLynx.Models.Tests;

public class PlaceDataTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullString_SetsEmptyString()
    {
        // Act
        var placeData = new PlaceData(null!);

        // Assert
        Assert.Equal(string.Empty, placeData.PlaceText);
    }

    [Fact]
    public void Constructor_WithEmptyString_SetsEmptyString()
    {
        // Act
        var placeData = new PlaceData(string.Empty);

        // Assert
        Assert.Equal(string.Empty, placeData.PlaceText);
    }

    [Fact]
    public void Constructor_WithValidString_SetsString()
    {
        // Act
        var placeData = new PlaceData("ABC");

        // Assert
        Assert.Equal("ABC", placeData.PlaceText);
    }

    [Fact]
    public void Constructor_WithStringWithWhitespace_TrimsString()
    {
        // Act
        var placeData = new PlaceData("  ABC  ");

        // Assert
        Assert.Equal("ABC", placeData.PlaceText);
    }

    [Fact]
    public void Constructor_WithStringWithLeadingWhitespace_TrimsString()
    {
        // Act
        var placeData = new PlaceData("  ABC");

        // Assert
        Assert.Equal("ABC", placeData.PlaceText);
    }

    [Fact]
    public void Constructor_WithStringWithTrailingWhitespace_TrimsString()
    {
        // Act
        var placeData = new PlaceData("ABC  ");

        // Assert
        Assert.Equal("ABC", placeData.PlaceText);
    }

    [Fact]
    public void Constructor_WithWhitespaceOnlyString_BecomesEmpty()
    {
        // Act
        var placeData = new PlaceData("   ");

        // Assert
        Assert.Equal(string.Empty, placeData.PlaceText);
    }

    [Fact]
    public void Constructor_WithTabAndSpaceWhitespace_BecomesEmpty()
    {
        // Act
        var placeData = new PlaceData("\t \n\r ");

        // Assert
        Assert.Equal(string.Empty, placeData.PlaceText);
    }

    [Fact]
    public void DefaultConstructor_SetsEmptyString()
    {
        // Act
        var placeData = new PlaceData();

        // Assert
        Assert.Equal(string.Empty, placeData.PlaceText);
    }

    #endregion

    #region HasPlaceData Property Tests

    [Fact]
    public void HasPlaceData_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var placeData = new PlaceData("");

        // Act & Assert
        Assert.False(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithNullString_ReturnsFalse()
    {
        // Arrange
        var placeData = new PlaceData(null);

        // Act & Assert
        Assert.False(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithWhitespaceOnly_ReturnsFalse()
    {
        // Arrange
        var placeData = new PlaceData("   ");

        // Act & Assert
        Assert.False(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithDefaultConstructor_ReturnsFalse()
    {
        // Arrange
        var placeData = new PlaceData();

        // Act & Assert
        Assert.False(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithValidInteger_ReturnsTrue()
    {
        // Arrange
        var placeData = new PlaceData("1");

        // Act & Assert
        Assert.True(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithValidString_ReturnsTrue()
    {
        // Arrange
        var placeData = new PlaceData("ABC");

        // Act & Assert
        Assert.True(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithIntegerAndWhitespace_ReturnsTrue()
    {
        // Arrange
        var placeData = new PlaceData("  1  ");

        // Act & Assert
        Assert.True(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithStringAndWhitespace_ReturnsTrue()
    {
        // Arrange
        var placeData = new PlaceData("  ABC  ");

        // Act & Assert
        Assert.True(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithZeroValue_ReturnsTrue()
    {
        // Arrange
        var placeData = new PlaceData("0");

        // Act & Assert
        Assert.True(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithNegativeValue_ReturnsTrue()
    {
        // Arrange
        var placeData = new PlaceData("-1");

        // Act & Assert
        Assert.True(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithDecimalValue_ReturnsTrue()
    {
        // Arrange
        var placeData = new PlaceData("1.5");

        // Act & Assert
        Assert.True(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithSpecialStartCode_ReturnsTrue()
    {
        // Arrange
        var placeData = new PlaceData("DNS");

        // Act & Assert
        Assert.True(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithSingleCharacter_ReturnsTrue()
    {
        // Arrange
        var placeData = new PlaceData("A");

        // Act & Assert
        Assert.True(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithLongString_ReturnsTrue()
    {
        // Arrange
        var placeData = new PlaceData("ThisIsALongString");

        // Act & Assert
        Assert.True(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithTabAndNewlineWhitespace_ReturnsFalse()
    {
        // Arrange
        var placeData = new PlaceData("\t\n\r ");

        // Act & Assert
        Assert.False(placeData.HasPlaceData);
    }

    [Fact]
    public void HasPlaceData_WithMixedWhitespaceAndContent_ReturnsTrue()
    {
        // Arrange
        var placeData = new PlaceData("\t\n ABC \r\n");

        // Act & Assert
        Assert.True(placeData.HasPlaceData);
    }

    #endregion

    #region Sorting Tests - Integer Values

    [Fact]
    public void CompareTo_IntegerValues_SortsByNumericValue()
    {
        // Arrange
        var place1 = new PlaceData("1");
        var place2 = new PlaceData("2");
        var place10 = new PlaceData("10");
        var place3 = new PlaceData("3");

        // Act & Assert
        Assert.True(place1.CompareTo(place2) < 0);
        Assert.True(place2.CompareTo(place3) < 0);
        Assert.True(place3.CompareTo(place10) < 0);
        Assert.True(place1.CompareTo(place10) < 0);
    }

    [Fact]
    public void CompareTo_IntegerValues_EqualValuesReturnZero()
    {
        // Arrange
        var place1a = new PlaceData("1");
        var place1b = new PlaceData("1");

        // Act & Assert
        Assert.Equal(0, place1a.CompareTo(place1b));
    }

    [Fact]
    public void CompareTo_IntegerValues_ReverseComparison()
    {
        // Arrange
        var place1 = new PlaceData("1");
        var place2 = new PlaceData("2");

        // Act & Assert
        Assert.True(place2.CompareTo(place1) > 0);
    }

    #endregion

    #region Sorting Tests - Empty Strings

    [Fact]
    public void CompareTo_EmptyStrings_AreEqual()
    {
        // Arrange
        var empty1 = new PlaceData("");
        var empty2 = new PlaceData("");

        // Act & Assert
        Assert.Equal(0, empty1.CompareTo(empty2));
    }

    [Fact]
    public void CompareTo_EmptyStrings_WithDefaultConstructor()
    {
        // Arrange
        var empty1 = new PlaceData("");
        var empty2 = new PlaceData();

        // Act & Assert
        Assert.Equal(0, empty1.CompareTo(empty2));
    }

    #endregion

    #region Sorting Tests - Non-Numeric Strings

    [Fact]
    public void CompareTo_NonNumericStrings_SortsAlphabetically()
    {
        // Arrange
        var placeA = new PlaceData("ABC");
        var placeB = new PlaceData("BCD");
        var placeC = new PlaceData("CDE");

        // Act & Assert
        Assert.True(placeA.CompareTo(placeB) < 0);
        Assert.True(placeB.CompareTo(placeC) < 0);
        Assert.True(placeA.CompareTo(placeC) < 0);
    }

    [Fact]
    public void CompareTo_NonNumericStrings_EqualStringsReturnZero()
    {
        // Arrange
        var placeA1 = new PlaceData("ABC");
        var placeA2 = new PlaceData("ABC");

        // Act & Assert
        Assert.Equal(0, placeA1.CompareTo(placeA2));
    }

    [Fact]
    public void CompareTo_NonNumericStrings_CaseSensitive()
    {
        // Arrange
        var placeLower = new PlaceData("abc");
        var placeUpper = new PlaceData("ABC");

        // Act & Assert
        Assert.True(placeLower.CompareTo(placeUpper) > 0); // 'a' > 'A' in ASCII
    }

    #endregion

    #region Sorting Tests - Cross-Category Comparisons

    [Fact]
    public void CompareTo_IntegerVsEmpty_IntegerComesFirst()
    {
        // Arrange
        var integerPlace = new PlaceData("1");
        var emptyPlace = new PlaceData("");

        // Act & Assert
        Assert.True(integerPlace.CompareTo(emptyPlace) < 0);
        Assert.True(emptyPlace.CompareTo(integerPlace) > 0);
    }

    [Fact]
    public void CompareTo_IntegerVsNonNumeric_IntegerComesFirst()
    {
        // Arrange
        var integerPlace = new PlaceData("1");
        var stringPlace = new PlaceData("ABC");

        // Act & Assert
        Assert.True(integerPlace.CompareTo(stringPlace) < 0);
        Assert.True(stringPlace.CompareTo(integerPlace) > 0);
    }

    [Fact]
    public void CompareTo_EmptyVsNonNumeric_EmptyComesFirst()
    {
        // Arrange
        var emptyPlace = new PlaceData("");
        var stringPlace = new PlaceData("ABC");

        // Act & Assert
        Assert.True(emptyPlace.CompareTo(stringPlace) < 0);
        Assert.True(stringPlace.CompareTo(emptyPlace) > 0);
    }

    #endregion

    #region Sorting Tests - Edge Cases

    [Fact]
    public void CompareTo_WithNull_ReturnsPositive()
    {
        // Arrange
        var place = new PlaceData("1");

        // Act & Assert
        Assert.True(place.CompareTo(null) > 0);
    }

    [Fact]
    public void CompareTo_ZeroValue_NotTreatedAsInteger()
    {
        // Arrange
        var zeroPlace = new PlaceData("0");
        var integerPlace = new PlaceData("1");
        var emptyPlace = new PlaceData("");

        // Act & Assert
        // Zero should be treated as non-numeric since we only accept integers > 0
        Assert.True(integerPlace.CompareTo(zeroPlace) < 0); // Integer comes before non-numeric
        Assert.True(zeroPlace.CompareTo(emptyPlace) > 0);   // Non-numeric comes after empty
    }

    [Fact]
    public void CompareTo_NegativeValue_NotTreatedAsInteger()
    {
        // Arrange
        var negativePlace = new PlaceData("-1");
        var integerPlace = new PlaceData("1");
        var emptyPlace = new PlaceData("");

        // Act & Assert
        // Negative should be treated as non-numeric since we only accept integers > 0
        Assert.True(integerPlace.CompareTo(negativePlace) < 0); // Integer comes before non-numeric
        Assert.True(negativePlace.CompareTo(emptyPlace) > 0);   // Non-numeric comes after empty
    }

    [Fact]
    public void CompareTo_DecimalValue_NotTreatedAsInteger()
    {
        // Arrange
        var decimalPlace = new PlaceData("1.5");
        var integerPlace = new PlaceData("1");
        var emptyPlace = new PlaceData("");

        // Act & Assert
        // Decimal should be treated as non-numeric since we only accept integers > 0
        Assert.True(integerPlace.CompareTo(decimalPlace) < 0); // Integer comes before non-numeric
        Assert.True(decimalPlace.CompareTo(emptyPlace) > 0);   // Non-numeric comes after empty
    }

    [Fact]
    public void CompareTo_WhitespaceString_TreatedAsEmptyAfterTrimming()
    {
        // Arrange
        var whitespacePlace = new PlaceData("   ");
        var emptyPlace = new PlaceData("");
        var stringPlace = new PlaceData("ABC");

        // Act & Assert
        // Whitespace should be treated as empty after trimming
        Assert.Equal(0, emptyPlace.CompareTo(whitespacePlace)); // Both are empty after trimming
        Assert.True(whitespacePlace.CompareTo(stringPlace) < 0); // Empty comes before non-numeric
    }

    [Fact]
    public void CompareTo_IntegerWithWhitespace_TreatedAsInteger()
    {
        // Arrange
        var integerWithWhitespace = new PlaceData("  1  ");
        var integerPlace = new PlaceData("2");
        var emptyPlace = new PlaceData("");

        // Act & Assert
        // Integer with whitespace should be treated as integer after trimming
        Assert.True(integerWithWhitespace.CompareTo(integerPlace) < 0); // 1 < 2
        Assert.True(integerWithWhitespace.CompareTo(emptyPlace) < 0);   // Integer comes before empty
    }

    [Fact]
    public void CompareTo_StringWithWhitespace_TreatedAsString()
    {
        // Arrange
        var stringWithWhitespace = new PlaceData("  ABC  ");
        var stringPlace = new PlaceData("BCD");
        var emptyPlace = new PlaceData("");

        // Act & Assert
        // String with whitespace should be treated as string after trimming
        Assert.True(stringWithWhitespace.CompareTo(stringPlace) < 0); // "ABC" < "BCD"
        Assert.True(stringWithWhitespace.CompareTo(emptyPlace) > 0);  // String comes after empty
    }

    #endregion

    #region Integration Tests - List Sorting

    [Fact]
    public void ListSort_ComplexScenario_SortsCorrectly()
    {
        // Arrange
        var places = new List<PlaceData>
        {
            new PlaceData("ABC"),    // Non-numeric
            new PlaceData("3"),      // Integer
            new PlaceData(""),       // Empty
            new PlaceData("1"),      // Integer
            new PlaceData("XYZ"),    // Non-numeric
            new PlaceData("2"),      // Integer
            new PlaceData("DEF"),    // Non-numeric
            new PlaceData(""),       // Empty
            new PlaceData("10")      // Integer
        };

        // Act
        places.Sort();

        // Assert
        var expectedOrder = new[]
        {
            "1", "2", "3", "10",    // Integers first (sorted numerically)
            "", "",                 // Empty strings second
            "ABC", "DEF", "XYZ"     // Non-numeric strings last (sorted alphabetically)
        };

        var actualOrder = places.Select(p => p.PlaceText).ToArray();
        Assert.Equal(expectedOrder, actualOrder);
    }

    [Fact]
    public void ListSort_WithSpecialStartCodes_SortsCorrectly()
    {
        // Arrange
        var places = new List<PlaceData>
        {
            new PlaceData("DNS"),    // Did Not Start
            new PlaceData("2"),      // Integer
            new PlaceData("DNF"),    // Did Not Finish
            new PlaceData("1"),      // Integer
            new PlaceData(""),       // Empty
            new PlaceData("DSQ"),    // Disqualified
            new PlaceData("3")       // Integer
        };

        // Act
        places.Sort();

        // Assert
        var expectedOrder = new[]
        {
            "1", "2", "3",          // Integers first
            "",                     // Empty string
            "DNF", "DNS", "DSQ"     // Special codes last (alphabetically)
        };

        var actualOrder = places.Select(p => p.PlaceText).ToArray();
        Assert.Equal(expectedOrder, actualOrder);
    }

    [Fact]
    public void ListSort_WithWhitespaceValues_SortsCorrectlyAfterTrimming()
    {
        // Arrange
        var places = new List<PlaceData>
        {
            new PlaceData("  ABC  "),  // String with whitespace
            new PlaceData("  2  "),    // Integer with whitespace
            new PlaceData("   "),      // Whitespace only (becomes empty)
            new PlaceData("1"),        // Integer
            new PlaceData("  XYZ  "),  // String with whitespace
            new PlaceData(""),         // Empty
            new PlaceData("  3  ")     // Integer with whitespace
        };

        // Act
        places.Sort();

        // Assert
        var expectedOrder = new[]
        {
            "1", "2", "3",          // Integers first (after trimming)
            "", "",                 // Empty strings second (including trimmed whitespace)
            "ABC", "XYZ"            // Non-numeric strings last (after trimming)
        };

        var actualOrder = places.Select(p => p.PlaceText).ToArray();
        Assert.Equal(expectedOrder, actualOrder);
    }

    #endregion

    #region LINQ OrderBy Tests

    [Fact]
    public void OrderBy_WorksCorrectly()
    {
        // Arrange
        var places = new List<PlaceData>
        {
            new PlaceData("B"),
            new PlaceData("2"),
            new PlaceData("A"),
            new PlaceData("1"),
            new PlaceData("")
        };

        // Act
        var sorted = places.OrderBy(p => p).ToList();

        // Assert
        var expectedOrder = new[] { "1", "2", "", "A", "B" };
        var actualOrder = sorted.Select(p => p.PlaceText).ToArray();
        Assert.Equal(expectedOrder, actualOrder);
    }

    [Fact]
    public void OrderByDescending_WorksCorrectly()
    {
        // Arrange
        var places = new List<PlaceData>
        {
            new PlaceData("B"),
            new PlaceData("2"),
            new PlaceData("A"),
            new PlaceData("1"),
            new PlaceData("")
        };

        // Act
        var sorted = places.OrderByDescending(p => p).ToList();

        // Assert
        var expectedOrder = new[] { "B", "A", "", "2", "1" };
        var actualOrder = sorted.Select(p => p.PlaceText).ToArray();
        Assert.Equal(expectedOrder, actualOrder);
    }

    #endregion

    #region Array Sort Tests

    [Fact]
    public void ArraySort_WorksCorrectly()
    {
        // Arrange
        var places = new[]
        {
            new PlaceData("C"),
            new PlaceData("2"),
            new PlaceData("A"),
            new PlaceData("1"),
            new PlaceData("")
        };

        // Act
        Array.Sort(places);

        // Assert
        var expectedOrder = new[] { "1", "2", "", "A", "C" };
        var actualOrder = places.Select(p => p.PlaceText).ToArray();
        Assert.Equal(expectedOrder, actualOrder);
    }

    #endregion

    #region Consistency Tests

    [Fact]
    public void CompareTo_IsConsistent()
    {
        // Arrange
        var place1 = new PlaceData("1");
        var place2 = new PlaceData("2");
        var place3 = new PlaceData("ABC");

        // Act & Assert
        // Test transitivity: if A < B and B < C, then A < C
        Assert.True(place1.CompareTo(place2) < 0);
        Assert.True(place2.CompareTo(place3) < 0);
        Assert.True(place1.CompareTo(place3) < 0);

        // Test symmetry: if A < B, then B > A
        Assert.True(place1.CompareTo(place2) < 0);
        Assert.True(place2.CompareTo(place1) > 0);

        // Test reflexivity: A == A
        Assert.Equal(0, place1.CompareTo(place1));
    }

    [Fact]
    public void CompareTo_MultipleCalls_ReturnsSameResult()
    {
        // Arrange
        var place1 = new PlaceData("1");
        var place2 = new PlaceData("2");

        // Act
        var result1 = place1.CompareTo(place2);
        var result2 = place1.CompareTo(place2);
        var result3 = place1.CompareTo(place2);

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    [Fact]
    public void ListSort_ComplexPlaceDataScenario_SortsCorrectly()
    {
        // Arrange - Test the exact scenario described by user
        var places = new List<PlaceData>
        {
            new PlaceData("DNF"),    // String place - should be at end
            new PlaceData("2"),      // Integer place - should be at top
            new PlaceData(""),       // No place data - should be in middle
            new PlaceData("1"),      // Integer place - should be at top
            new PlaceData("DNS"),    // String place - should be at end
            new PlaceData(""),       // No place data - should be in middle
            new PlaceData("3")       // Integer place - should be at top
        };

        // Act
        places.Sort();

        // Assert
        // Expected order: 1, 2, 3, "", "", DNF, DNS
        var expectedOrder = new[]
        {
            "1", "2", "3",          // Integers first (sorted by value)
            "", "",                 // Empty strings second (all equal)
            "DNF", "DNS"            // Non-numeric strings last (alphabetically)
        };

        var actualOrder = places.Select(p => p.PlaceText).ToArray();
        Assert.Equal(expectedOrder, actualOrder);
    }

    [Fact]
    public void RacerSorting_WithLaneNumbers_SortsCorrectly()
    {
        // Arrange - Test racers with different place data and lane numbers
        var racers = new List<Racer>
        {
            new Racer { Lane = 3, Place = new PlaceData("DNF") },  // String place
            new Racer { Lane = 1, Place = new PlaceData("2") },    // Integer place
            new Racer { Lane = 5, Place = new PlaceData("") },     // No place data
            new Racer { Lane = 2, Place = new PlaceData("1") },    // Integer place
            new Racer { Lane = 4, Place = new PlaceData("DNS") },  // String place
            new Racer { Lane = 6, Place = new PlaceData("") },     // No place data
            new Racer { Lane = 7, Place = new PlaceData("3") }     // Integer place
        };

        // Act - Sort by place (using TemplateService logic)
        var sortedRacers = racers.OrderBy(r => r.Place).ThenBy(r => r.Lane).ToList();

        // Assert
        // Expected order: Lane 2 (place 1), Lane 1 (place 2), Lane 7 (place 3), 
        //                 Lane 5 (no place), Lane 6 (no place), Lane 3 (DNF), Lane 4 (DNS)
        var expectedLanes = new[] { 2, 1, 7, 5, 6, 3, 4 };
        var actualLanes = sortedRacers.Select(r => r.Lane).ToArray();
        Assert.Equal(expectedLanes, actualLanes);
    }

    [Fact]
    public void RacerSorting_OnlyNumericPlaces_SortsByPlaceValue()
    {
        // Arrange - Test racers with only numeric places
        var racers = new List<Racer>
        {
            new Racer { Lane = 3, Place = new PlaceData("3") },
            new Racer { Lane = 1, Place = new PlaceData("1") },
            new Racer { Lane = 2, Place = new PlaceData("2") },
            new Racer { Lane = 4, Place = new PlaceData("10") }  // Test larger numbers
        };

        // Act
        var sortedRacers = racers.OrderBy(r => r.Place).ThenBy(r => r.Lane).ToList();

        // Assert - Should sort by place value, not lane
        var expectedLanes = new[] { 1, 2, 3, 4 }; // 1, 2, 3, 10
        var actualLanes = sortedRacers.Select(r => r.Lane).ToArray();
        Assert.Equal(expectedLanes, actualLanes);
    }

    [Fact]
    public void RacerSorting_OnlyNoPlaceData_SortsByLaneNumber()
    {
        // Arrange - Test racers with no place data
        var racers = new List<Racer>
        {
            new Racer { Lane = 5, Place = new PlaceData("") },
            new Racer { Lane = 2, Place = new PlaceData("") },
            new Racer { Lane = 8, Place = new PlaceData("") },
            new Racer { Lane = 1, Place = new PlaceData("") }
        };

        // Act
        var sortedRacers = racers.OrderBy(r => r.Place).ThenBy(r => r.Lane).ToList();

        // Assert - Should sort by lane number
        var expectedLanes = new[] { 1, 2, 5, 8 };
        var actualLanes = sortedRacers.Select(r => r.Lane).ToArray();
        Assert.Equal(expectedLanes, actualLanes);
    }

    [Fact]
    public void RacerSorting_OnlyStringPlaces_SortsAlphabetically()
    {
        // Arrange - Test racers with only string places
        var racers = new List<Racer>
        {
            new Racer { Lane = 3, Place = new PlaceData("DNF") },
            new Racer { Lane = 1, Place = new PlaceData("DNS") },
            new Racer { Lane = 2, Place = new PlaceData("DSQ") },
            new Racer { Lane = 4, Place = new PlaceData("ABC") }
        };

        // Act
        var sortedRacers = racers.OrderBy(r => r.Place).ThenBy(r => r.Lane).ToList();

        // Assert - Should sort alphabetically by place text
        // Actual order: ABC (Lane 4), DNF (Lane 3), DNS (Lane 1), DSQ (Lane 2)
        var expectedLanes = new[] { 4, 3, 1, 2 }; // ABC, DNF, DNS, DSQ
        var actualLanes = sortedRacers.Select(r => r.Lane).ToArray();
        Assert.Equal(expectedLanes, actualLanes);
    }

    [Fact]
    public void RacerSorting_MixedScenarios_RespectsPriorityOrder()
    {
        // Arrange - Test the complete priority system
        var racers = new List<Racer>
        {
            // String places (should be last)
            new Racer { Lane = 7, Place = new PlaceData("DNF") },
            new Racer { Lane = 8, Place = new PlaceData("DNS") },
            
            // No place data (should be middle, sorted by lane)
            new Racer { Lane = 5, Place = new PlaceData("") },
            new Racer { Lane = 3, Place = new PlaceData("") },
            
            // Numeric places (should be first)
            new Racer { Lane = 2, Place = new PlaceData("2") },
            new Racer { Lane = 1, Place = new PlaceData("1") },
            new Racer { Lane = 4, Place = new PlaceData("10") }
        };

        // Act
        var sortedRacers = racers.OrderBy(r => r.Place).ThenBy(r => r.Lane).ToList();

        // Assert
        // Expected order: 
        // 1. Numeric places: Lane 1 (place 1), Lane 2 (place 2), Lane 4 (place 10)
        // 2. No place data: Lane 3 (no place), Lane 5 (no place)  
        // 3. String places: Lane 7 (DNF), Lane 8 (DNS)
        var expectedLanes = new[] { 1, 2, 4, 3, 5, 7, 8 };
        var actualLanes = sortedRacers.Select(r => r.Lane).ToArray();
        Assert.Equal(expectedLanes, actualLanes);
    }

    [Fact]
    public void RacerSorting_EdgeCases_HandlesCorrectly()
    {
        // Arrange - Test edge cases
        var racers = new List<Racer>
        {
            new Racer { Lane = 1, Place = new PlaceData("0") },     // Zero (not > 0, so treated as string)
            new Racer { Lane = 2, Place = new PlaceData("-1") },    // Negative (treated as string)
            new Racer { Lane = 3, Place = new PlaceData("1.5") },   // Decimal (treated as string)
            new Racer { Lane = 4, Place = new PlaceData("1") },     // Valid integer
            new Racer { Lane = 5, Place = new PlaceData("") },      // No place data
            new Racer { Lane = 6, Place = new PlaceData("   ") }    // Whitespace (treated as no place data)
        };

        // Act
        var sortedRacers = racers.OrderBy(r => r.Place).ThenBy(r => r.Lane).ToList();

        // Assert
        // Expected order: 
        // 1. Valid integer: Lane 4 (place 1)
        // 2. No place data: Lane 5 (empty), Lane 6 (whitespace)
        // 3. String values: Lane 2 (-1), Lane 1 (0), Lane 3 (1.5) - sorted alphabetically
        var expectedLanes = new[] { 4, 5, 6, 2, 1, 3 }; // 1, "", "", "-1", "0", "1.5"
        var actualLanes = sortedRacers.Select(r => r.Lane).ToArray();
        Assert.Equal(expectedLanes, actualLanes);
    }

    [Fact]
    public void RacerSorting_WithIdenticalPlaces_SortsByLane()
    {
        // Arrange - Test racers with identical place data
        var racers = new List<Racer>
        {
            new Racer { Lane = 3, Place = new PlaceData("1") },
            new Racer { Lane = 1, Place = new PlaceData("1") },
            new Racer { Lane = 2, Place = new PlaceData("1") },
            new Racer { Lane = 4, Place = new PlaceData("") },
            new Racer { Lane = 6, Place = new PlaceData("") },
            new Racer { Lane = 5, Place = new PlaceData("") }
        };

        // Act
        var sortedRacers = racers.OrderBy(r => r.Place).ThenBy(r => r.Lane).ToList();

        // Assert
        // Expected order: 
        // 1. Place 1: Lane 1, Lane 2, Lane 3 (sorted by lane)
        // 2. No place: Lane 4, Lane 5, Lane 6 (sorted by lane)
        var expectedLanes = new[] { 1, 2, 3, 4, 5, 6 };
        var actualLanes = sortedRacers.Select(r => r.Lane).ToArray();
        Assert.Equal(expectedLanes, actualLanes);
    }

    #endregion
}
