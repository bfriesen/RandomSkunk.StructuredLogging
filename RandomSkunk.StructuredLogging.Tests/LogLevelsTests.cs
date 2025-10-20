using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

public class LogLevelsTests
{
    [Fact]
    public void Trace_LogLevel_Is_Trace()
    {
        // Arrange
        Trace trace = default;
        
        // Act
        var logLevel = trace.LogLevel;
        
        // Assert
        logLevel.Should().Be(LogLevel.Trace);
    }

    [Fact]
    public void Debug_LogLevel_Is_Debug()
    {
        // Arrange
        Debug debug = default;
        
        // Act
        var logLevel = debug.LogLevel;
        
        // Assert
        logLevel.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public void Information_LogLevel_Is_Information()
    {
        // Arrange
        Information information = default;
        
        // Act
        var logLevel = information.LogLevel;
        
        // Assert
        logLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void Warning_LogLevel_Is_Warning()
    {
        // Arrange
        Warning warning = default;
        
        // Act
        var logLevel = warning.LogLevel;
        
        // Assert
        logLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void Error_LogLevel_Is_Error()
    {
        // Arrange
        Error error = default;
        
        // Act
        var logLevel = error.LogLevel;
        
        // Assert
        logLevel.Should().Be(LogLevel.Error);
    }

    [Fact]
    public void Critical_LogLevel_Is_Critical()
    {
        // Arrange
        Critical critical = default;
        
        // Act
        var logLevel = critical.LogLevel;
        
        // Assert
        logLevel.Should().Be(LogLevel.Critical);
    }
}
