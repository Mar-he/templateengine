using Xunit;
using System.Globalization;
using TemplateEngine.Modifiers;

namespace TemplateEngine.Tests;

public class ModifierTests
{
    [Fact]
    public void ProcessTemplate_WithRounding_RoundsCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 123.456789, Unit = "km/h" }
        };
        
        var engine = new TemplateEngine(items);
        
        // Act & Assert
        Assert.Equal("123.46", engine.ProcessTemplate("{{speed.value:round(2)}}"));
        Assert.Equal("123", engine.ProcessTemplate("{{speed.value:round(0)}}"));
        Assert.Equal("123.4568", engine.ProcessTemplate("{{speed.value:round(4)}}"));
    }

    [Fact]
    public void ProcessTemplate_WithUnitConversion_ConvertsCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 100, Unit = "km/h" },
            new() { Name = "consumption", NumericValue = 8.5, Unit = "l/100km" },
            new() { Name = "temp", NumericValue = 25, Unit = "celsius" }
        };
        
        var engine = new TemplateEngine(items);
        
        // Act & Assert - Use rounded values to avoid floating point precision issues
        Assert.Equal("62.1371", engine.ProcessTemplate("{{speed.value:convert(mph):round(4)}}"));
        Assert.Equal("27.7", engine.ProcessTemplate("{{consumption.value:convert(mpg):round(1)}}"));
        Assert.Equal("77", engine.ProcessTemplate("{{temp.value:convert(fahrenheit):round(0)}}"));
    }

    [Fact]
    public void ProcessTemplate_WithConvertAndRound_AppliesInOrder()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 100, Unit = "km/h" }
        };
        
        var engine = new TemplateEngine(items);
        
        // Act & Assert
        Assert.Equal("62.1", engine.ProcessTemplate("{{speed.value:convert(mph):round(1)}}"));
        Assert.Equal("62", engine.ProcessTemplate("{{speed.value:convert(mph):round(0)}}"));
    }

    [Fact]
    public void ProcessTemplate_WithRoundAndConvert_AppliesInOrder()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 100.789, Unit = "km/h" }
        };
        
        var engine = new TemplateEngine(items);
        
        // Act & Assert
        // First round to 101, then convert to mph
        Assert.Equal("62.8", engine.ProcessTemplate("{{speed.value:round(0):convert(mph):round(1)}}"));
    }

    [Fact]
    public void ProcessTemplate_WithInvalidConversion_ReturnsOriginalValue()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "weight", NumericValue = 100, Unit = "kg" }
        };
        
        var engine = new TemplateEngine(items);
        
        // Act - Try to convert kg to mph (invalid conversion)
        var result = engine.ProcessTemplate("{{weight.value:convert(mph)}}");
        
        // Assert - Should return original value since conversion is not possible
        Assert.Equal("100", result);
    }

    [Fact]
    public void ProcessTemplate_WithStringValue_IgnoresModifiers()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "type", StringValue = "electric power" }
        };
        
        var engine = new TemplateEngine(items);
        
        // Act
        var result = engine.ProcessTemplate("{{type.value:round(2)}}");
        
        // Assert - String values should ignore numeric modifiers
        Assert.Equal("electric power", result);
    }

    [Fact]
    public void ProcessTemplate_ComplexTemplate_HandlesMultipleConversions()
    {
        // Arrange
        var jsonData = """
        [{
          "name": "car_speed",
          "numeric_value": 120.5,
          "unit": "km/h"
        }, {
          "name": "fuel_consumption",
          "numeric_value": 7.2,
          "unit": "l/100km"
        }, {
          "name": "temperature",
          "numeric_value": 22.5,
          "unit": "celsius"
        }]
        """;
        
        var engine = new TemplateEngine(jsonData);
        var template = "Speed: {{car_speed.value:convert(mph):round(1)}} mph, " +
                      "Consumption: {{fuel_consumption.value:convert(mpg):round(1)}} mpg, " +
                      "Temp: {{temperature.value:convert(fahrenheit):round(0)}}°F";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert - 22.5°C = 72.5°F, rounded to 72°F (banker's rounding)
        Assert.Equal("Speed: 74.9 mph, Consumption: 32.7 mpg, Temp: 72°F", result);
    }

    [Fact]
    public void RegisterModifier_CustomModifier_WorksCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "test", NumericValue = 10, Unit = "units" }
        };
        
        var engine = new TemplateEngine(items);
        
        // Create a custom modifier that multiplies by 2
        var customModifier = new TestMultiplyModifier();
        engine.RegisterModifier(customModifier);
        
        // Act
        var result = engine.ProcessTemplate("{{test.value:multiply(2)}}");
        
        // Assert
        Assert.Equal("20", result);
    }

    [Fact]
    public void ModifierProcessor_ChainedCustomModifiers_WorksCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "test", NumericValue = 5.555, Unit = "km/h" }
        };
        
        var engine = new TemplateEngine(items);
        
        // Register custom modifier
        engine.RegisterModifier(new TestMultiplyModifier());
        
        // Act - Test chaining custom modifier with existing ones
        var result = engine.ProcessTemplate("{{test.value:multiply(2):convert(mph):round(1)}}");
        
        // Assert - 5.555 * 2 = 11.11 km/h -> ~6.9 mph -> 6.9
        Assert.Equal("6.9", result);
    }
}

// Test helper class for custom modifier
public class TestMultiplyModifier : IValueModifier
{
    public bool CanHandle(string modifierString)
    {
        return modifierString.StartsWith("multiply(", StringComparison.OrdinalIgnoreCase) && 
               modifierString.EndsWith(")");
    }

    public void Apply(ModifierContext context, string modifierString)
    {
        var parameter = modifierString[9..^1]; // Extract between "multiply(" and ")"
        if (double.TryParse(parameter, NumberStyles.Float, CultureInfo.InvariantCulture, out var multiplier))
        {
            context.Value *= multiplier;
        }
    }
}
