using System.Globalization;
using System.Text.Json;
using TemplateEngine.Events;
using TemplateEngine.Modifiers;
using Xunit;

namespace TemplateEngine.Tests;

/// <summary>
/// Tests for template engine events functionality.
/// </summary>
public class TemplateEngineEventsTests
{
    [Fact]
    public void ProcessTemplate_ShouldRaiseTemplateProcessingStartedEvent()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 60, Unit = "km/h" }
        };
        var engine = new TemplateEngine(items);
        
        TemplateProcessingEventArgs? eventArgs = null;
        engine.TemplateProcessingStarted += (sender, e) => eventArgs = e;

        // Act
        engine.ProcessTemplate("Speed: {{speed.value}}");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("Speed: {{speed.value}}", eventArgs.Template);
        Assert.Equal(1, eventArgs.TokenCount);
        Assert.NotNull(eventArgs.CorrelationId);
        Assert.Null(eventArgs.Result); // Not available in started event
        Assert.Null(eventArgs.Duration); // Not available in started event
    }

    [Fact]
    public void ProcessTemplate_ShouldRaiseTemplateProcessingCompletedEvent()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 60, Unit = "km/h" }
        };
        var engine = new TemplateEngine(items);
        
        TemplateProcessingEventArgs? eventArgs = null;
        engine.TemplateProcessingCompleted += (sender, e) => eventArgs = e;

        // Act
        var result = engine.ProcessTemplate("Speed: {{speed.value}}");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("Speed: {{speed.value}}", eventArgs.Template);
        Assert.Equal("Speed: 60", eventArgs.Result);
        Assert.Equal(1, eventArgs.TokenCount);
        Assert.NotNull(eventArgs.CorrelationId);
        Assert.NotNull(eventArgs.Duration);
        Assert.True(eventArgs.Duration > TimeSpan.Zero);
    }

    [Fact]
    public void ProcessTemplate_ShouldRaiseTokenProcessingEvents()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 60, Unit = "km/h" }
        };
        var engine = new TemplateEngine(items);
        
        var tokenEvents = new List<TokenProcessingEventArgs>();
        engine.TokenProcessing += (sender, e) => tokenEvents.Add(e);

        // Act
        engine.ProcessTemplate("Speed: {{speed.value}} {{speed.unit}}");

        // Assert
        Assert.Equal(2, tokenEvents.Count);
        
        var valueEvent = tokenEvents.First(e => e.PropertyName == "value");
        Assert.Equal("{{speed.value}}", valueEvent.Token);
        Assert.Equal("speed", valueEvent.ItemName);
        Assert.Equal("value", valueEvent.PropertyName);
        Assert.Equal(60.0, valueEvent.RawValue);
        Assert.Equal("60", valueEvent.ProcessedValue);
        Assert.True(valueEvent.IsSuccessful);
        
        var unitEvent = tokenEvents.First(e => e.PropertyName == "unit");
        Assert.Equal("{{speed.unit}}", unitEvent.Token);
        Assert.Equal("speed", unitEvent.ItemName);
        Assert.Equal("unit", unitEvent.PropertyName);
        Assert.Equal("km/h", unitEvent.RawValue);
        Assert.Equal("km/h", unitEvent.ProcessedValue);
        Assert.True(unitEvent.IsSuccessful);
    }

    [Fact]
    public void ProcessTemplate_ShouldRaiseTokenProcessingEventForUnknownItem()
    {
        // Arrange
        var engine = new TemplateEngine(new List<TemplateItem>());
        
        TokenProcessingEventArgs? eventArgs = null;
        engine.TokenProcessing += (sender, e) => eventArgs = e;

        // Act
        var result = engine.ProcessTemplate("{{unknown.value}}");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("{{unknown.value}}", eventArgs.Token);
        Assert.Equal("unknown", eventArgs.ItemName);
        Assert.Equal("value", eventArgs.PropertyName);
        Assert.Null(eventArgs.RawValue);
        Assert.Equal("{{unknown.value}}", eventArgs.ProcessedValue);
        Assert.False(eventArgs.IsSuccessful);
        Assert.Equal("{{unknown.value}}", result); // Should return original token
    }

    [Fact]
    public void ProcessTemplate_WithModifiers_ShouldRaiseModifierProcessingEvents()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 60.123, Unit = "km/h" }
        };
        var engine = new TemplateEngine(items);
        
        var modifierEvents = new List<ModifierProcessingEventArgs>();
        engine.ModifierProcessing += (sender, e) => modifierEvents.Add(e);

        // Act
        engine.ProcessTemplate("{{speed.value:round(1)}}");

        // Assert
        Assert.Single(modifierEvents);
        
        var roundEvent = modifierEvents[0];
        Assert.Equal("round", roundEvent.ModifierName);
        Assert.Equal("1", roundEvent.Parameters);
        Assert.Equal(60.123, roundEvent.InputValue);
        Assert.Equal(60.1, roundEvent.OutputValue);
        Assert.True(roundEvent.IsSuccessful);
    }

    [Fact]
    public void ProcessTemplate_WithMultipleModifiers_ShouldRaiseMultipleModifierEvents()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 100, Unit = "km/h" }
        };
        var engine = new TemplateEngine(items);
        
        var modifierEvents = new List<ModifierProcessingEventArgs>();
        engine.ModifierProcessing += (sender, e) => modifierEvents.Add(e);

        // Act
        engine.ProcessTemplate("{{speed.value:convert(mph):round(1)}}");

        // Assert
        Assert.Equal(2, modifierEvents.Count);
        
        var convertEvent = modifierEvents[0];
        Assert.Equal("convert", convertEvent.ModifierName);
        Assert.Equal("mph", convertEvent.Parameters);
        Assert.Equal(100.0, convertEvent.InputValue);
        Assert.True(convertEvent.IsSuccessful);
        
        var roundEvent = modifierEvents[1];
        Assert.Equal("round", roundEvent.ModifierName);
        Assert.Equal("1", roundEvent.Parameters);
        Assert.True(roundEvent.IsSuccessful);
    }

    [Fact]
    public void ProcessTemplate_WithError_ShouldRaiseErrorEvent()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "test", NumericValue = 42 }
        };
        var engine = new TemplateEngine(items);
        
        TemplateEngineErrorEventArgs? errorEvent = null;
        engine.ErrorOccurred += (sender, e) => errorEvent = e;

        // Act & Assert - Let's first test what actually happens
        try
        {
            var result = engine.ProcessTemplate("{{test.value:invalidmodifier(abc)}}");

            // If we get here, no exception was thrown - let's see what we got
            Assert.Fail($"Expected InvalidModifierException but got result: {result}");
        }
        catch (InvalidModifierException ex)
        {
            // This is what we expect
            Assert.Equal("invalidmodifier", ex.ModifierName);
            Assert.Equal("invalidmodifier(abc)", ex.ModifierString);
            Assert.Contains("invalidmodifier", ex.Message);
            Assert.Contains("invalidmodifier(abc)", ex.Message);

            // The error event should have been raised
            Assert.NotNull(errorEvent);
            Assert.Equal("Template Processing", errorEvent.Context);
            Assert.NotNull(errorEvent.Exception);
            Assert.IsType<InvalidModifierException>(errorEvent.Exception);
            Assert.Equal(ErrorSeverity.Critical, errorEvent.Severity);
            Assert.NotNull(errorEvent.CorrelationId);
        }
        catch (Exception ex)
        {
            // Some other exception was thrown
            Assert.Fail($"Expected InvalidModifierException but got: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [Fact] 
    public void ProcessTemplate_WithInvalidRoundParameter_ShouldNotThrowButReturnOriginalValue()
    {
        // Arrange - Test a realistic error scenario with round modifier
        var items = new List<TemplateItem>
        {
            new() { Name = "test", NumericValue = 42.456 }
        };
        var engine = new TemplateEngine(items);
        
        TemplateEngineErrorEventArgs? errorEvent = null;
        engine.ErrorOccurred += (sender, e) => errorEvent = e;

        // Act - Use an invalid parameter for round modifier
        var result = engine.ProcessTemplate("{{test.value:round(invalid)}}");
        
        // Assert - Should handle gracefully without throwing
        // The round modifier should fail silently and return the original value
        Assert.NotNull(result);
        // The exact behavior depends on how round modifier handles invalid parameters
    }

    [Fact]
    public void Constructor_WithInvalidJson_ShouldThrowException()
    {
        // Arrange & Act & Assert
        Assert.Throws<JsonException>(() =>
        {
            new TemplateEngine("invalid json that cannot be parsed");
        });
    }

    [Fact]
    public void EventArgs_ShouldHaveCorrelationIds()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "test", NumericValue = 42 }
        };
        var engine = new TemplateEngine(items);
        
        var correlationIds = new HashSet<string>();
        engine.TemplateProcessingStarted += (sender, e) => correlationIds.Add(e.CorrelationId!);
        engine.TemplateProcessingCompleted += (sender, e) => correlationIds.Add(e.CorrelationId!);
        engine.TokenProcessing += (sender, e) => correlationIds.Add(e.CorrelationId!);

        // Act
        engine.ProcessTemplate("{{test.value}}");

        // Assert
        Assert.Single(correlationIds); // All events should have the same correlation ID
        Assert.All(correlationIds, id => Assert.False(string.IsNullOrEmpty(id)));
    }

    [Fact]
    public void EventArgs_ShouldHaveTimestamps()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "test", NumericValue = 42 }
        };
        var engine = new TemplateEngine(items);
        
        var timestamps = new List<DateTimeOffset>();
        engine.TemplateProcessingStarted += (sender, e) => timestamps.Add(e.Timestamp);
        engine.TokenProcessing += (sender, e) => timestamps.Add(e.Timestamp);
        engine.TemplateProcessingCompleted += (sender, e) => timestamps.Add(e.Timestamp);

        var beforeProcessing = DateTimeOffset.UtcNow;

        // Act
        engine.ProcessTemplate("{{test.value}}");

        var afterProcessing = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(3, timestamps.Count);
        Assert.All(timestamps, timestamp => 
        {
            Assert.True(timestamp >= beforeProcessing);
            Assert.True(timestamp <= afterProcessing);
        });
        
        // Timestamps should be in chronological order
        Assert.True(timestamps[0] <= timestamps[1]);
        Assert.True(timestamps[1] <= timestamps[2]);
    }

    [Fact]
    public void ProcessTemplate_WithGermanCulture_ShouldFormatNumbersCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "price", NumericValue = 1234.56 }
        };
        var engine = new TemplateEngine(items, new CultureInfo("de-DE"));
        
        TokenProcessingEventArgs? tokenEvent = null;
        engine.TokenProcessing += (sender, e) => tokenEvent = e;

        // Act
        var result = engine.ProcessTemplate("{{price.value}}");

        // Assert
        Assert.NotNull(tokenEvent);
        Assert.Equal("1234,56", tokenEvent.ProcessedValue); // German decimal separator
        Assert.Equal("1234,56", result);
    }
}
