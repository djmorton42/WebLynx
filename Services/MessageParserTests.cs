using Xunit;
using WebLynx.Utilities;
using Microsoft.Extensions.Logging;

namespace WebLynx.Services.Tests;

public class MessageParserTests
{
    [Fact]
    public void ParseRacerFromResultsLine_GivenValidLine_ReturnsValidRacer()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MessageParser>();
        var parser = new MessageParser(logger);
        string text = "1   3   279  Some Racer                                         Some Club                       1:21.404 1:21.404        ";


        // Act
        var racer = parser.ParseRacerFromResultsLine(text);

        // Log the racer details before assertion
        if (racer != null)
        {
            Console.WriteLine($"Parsed Racer: {racer.ToString()}");
        }
        else
        {
            Console.WriteLine("Racer is null");
        }

        Assert.Equal("Some Club", racer.Affiliation);
        Assert.Equal("Some Racer", racer.Name);
        Assert.Equal(1, racer.Place);
        Assert.Equal(3, racer.Lane);
        Assert.Equal(279, racer.Id);
        Assert.Equal(TimeSpan.FromSeconds(81).Add(TimeSpan.FromMilliseconds(404)), racer.FinalTime);
    }
}