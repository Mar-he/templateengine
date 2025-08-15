using Xunit;
using TemplateEngine.Events;
using TemplateEngine.Modifiers;

namespace TemplateEngine.Tests;

public class TemplateEngineEventsTests
{
    [Fact]
    public void ProcessTemplate_RaisesTemplateProcessingStartedEvent()
    {
        // Arrange
        var engine = new TemplateEngine(new List<TemplateItem>());
        var eventRaised = false;
        string? receivedTemplate = null;
        
        engine.TemplateProcessingStarted += (sender, e) =>
        {
            eventRaised = true;
            receivedTemplate = e.Template;
        };

        var templateDto = TemplateDto.Create(
            "Test template",
            new Dictionary<string, TemplateVariable>());

        // Act
        engine.ProcessTemplate(templateDto);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("Test template", receivedTemplate);
    }

    [Fact]
    public void ProcessTemplate_RaisesTemplateProcessingCompletedEvent()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "test", NumericValue = 42 }
        };
        
        var engine = new TemplateEngine(items);
        var eventRaised = false;
        string? receivedResult = null;
        
        engine.TemplateProcessingCompleted += (sender, e) =>
        {
            eventRaised = true;
            receivedResult = e.Result;
        };

        var templateDto = TemplateDto.Create(
            "Value: {{value}}",
            new Dictionary<string, TemplateVariable>
            {
                ["value"] = new() { Id = "test", Source = VariableSource.NumberValue }
            });

        // Act
        engine.ProcessTemplate(templateDto);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("Value: 42", receivedResult);
    }

    [Fact]
    public void ProcessTemplate_RaisesTokenProcessingEvent()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "test", StringValue = "test value" }
        };
        
        var engine = new TemplateEngine(items);
        var eventRaised = false;
        string? receivedToken = null;
        
        engine.TokenProcessing += (sender, e) =>
        {
            eventRaised = true;
            receivedToken = e.Token;
        };

        var templateDto = TemplateDto.Create(
            "{{testVar}}",
            new Dictionary<string, TemplateVariable>
            {
                ["testVar"] = new() { Id = "test", Source = VariableSource.StringValue }
            });

        // Act
        engine.ProcessTemplate(templateDto);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("{{testVar}}", receivedToken);
    }

    [Fact]
    public void ProcessTemplate_WithInvalidDto_RaisesErrorOccurredEvent()
    {
        // Arrange
        var engine = new TemplateEngine(new List<TemplateItem>());
        var eventRaised = false;
        string? receivedMessage = null;
        
        engine.ErrorOccurred += (sender, e) =>
        {
            eventRaised = true;
            receivedMessage = e.Exception.Message;
        };

        // This test should now fail at TemplateDto.Create() due to validation
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            TemplateDto.Create(
                "{{undefinedVar}}",
                new Dictionary<string, TemplateVariable>()));
        Assert.Contains("undefinedVar", ex.Message);
    }
}
