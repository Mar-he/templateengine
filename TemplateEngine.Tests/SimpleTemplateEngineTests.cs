using Xunit;
using System.Text.Json;
using System.Globalization;
using TemplateEngine.Modifiers;

namespace TemplateEngine.Tests;

public class TemplateEngineTests
{
    [Fact]
    public void Constructor_WithJsonData_ParsesCorrectly()
    {
        // Arrange
        var jsonData = """
                       [{
                         "category":"speed",
                         "numeric_value": 100,
                         "unit": "km/h"
                       }, {
                         "category":"type",
                         "string_value": "electric vehicle"
                       }]
                       """;

        // Act
        var engine = new TemplateEngine(jsonData);
        var items = engine.GetItems();

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Equal("speed", items[0].Category);
        Assert.Equal(100.0, items[0].NumericValue);
        Assert.Equal("km/h", items[0].Unit);
        Assert.Equal("type", items[1].Category);
        Assert.Equal("electric vehicle", items[1].StringValue);
    }

    [Fact]
    public void Constructor_WithTemplateItems_InitializesCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "test", NumericValue = 42, Unit = "units" }
        };

        // Act
        var engine = new TemplateEngine(items);
        var retrievedItems = engine.GetItems();

        // Assert
        Assert.Single(retrievedItems);
        Assert.Equal("test", retrievedItems[0].Category);
        Assert.Equal(42.0, retrievedItems[0].NumericValue);
        Assert.Equal("units", retrievedItems[0].Unit);
    }

    [Fact]
    public void Constructor_WithCustomCulture_FormatsNumbersCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "test", NumericValue = 1234.56 }
        };

        var germanCulture = new CultureInfo("de-DE");
        var engine = new TemplateEngine(items, germanCulture);

        var templateDto = TemplateDto.Create(
            "Value: {{value}}",
            new Dictionary<string, TemplateVariable>
            {
                ["value"] = new() { Id = "test", Source = VariableSource.NumberValue }
            });

        // Act
        var result = engine.ProcessTemplate(templateDto);

        // Assert - German culture uses comma as decimal separator
        Assert.Equal("Value: 1234,56", result);
    }

    [Fact]
    public void Constructor_WithCustomJsonOptions_AppliesConfiguration()
    {
        // Arrange
        var jsonData = """
                       [{
                         "category":"test_item",
                         "numeric_value": 42.5,
                         "unit": "kg"
                       }]
                       """;

        var options = new TemplateEngineOptions
        {
            ConfigureJsonOptions = jsonOptions =>
            {
                jsonOptions.AllowTrailingCommas = true;
                jsonOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            }
        };

        // Act
        var engine = new TemplateEngine(jsonData, options);
        var items = engine.GetItems();

        // Assert
        Assert.Single(items);
        Assert.Equal("test_item", items[0].Category);
        Assert.Equal(42.5, items[0].NumericValue);
        Assert.Equal("kg", items[0].Unit);
    }


    [Fact]
    public void RegisterModifier_CustomModifier_IsRegistered()
    {
        // Arrange
        var engine = new TemplateEngine(new List<TemplateItem>());
        var customModifier = new TestCustomModifier();

        // Act & Assert - Should not throw
        engine.RegisterModifier(customModifier);
    }

    [Fact]
    public void GetItems_ReturnsDefensiveCopy()
    {
        // Arrange
        var originalItems = new List<TemplateItem>
        {
            new() { Category = "test", NumericValue = 42 }
        };

        var engine = new TemplateEngine(originalItems);

        // Act
        var retrievedItems = engine.GetItems();
        retrievedItems.Clear(); // Modify the returned list

        // Assert - Original items should still be intact
        var itemsAgain = engine.GetItems();
        Assert.Single(itemsAgain);
        Assert.Equal("test", itemsAgain[0].Category);
    }

    [Fact]
    public void ProcessTemplate_Rounds_Correctly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "test", NumericValue = 42.52 }
        };

        var dto = TemplateDto.Create(
            "This is a test: {{test}}",
            new Dictionary<string, TemplateVariable>()
            {
                {
                    "test",
                    new TemplateVariable { Id = "test", Source = VariableSource.NumberValue, Round = "round(1)" }
                }
            });

        var engine = new TemplateEngine(items);

        // Act
        var result = engine.ProcessTemplate(dto);


        // Assert 
        Assert.Equal("This is a test: 42.5", result);
    }

    [Fact]
    public void ProcessTemplate_Parses_Multiple_Items()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "test", NumericValue = 42.52 },
            new() { Category = "1.2.3", StringValue = "Hello World" },
        };

        var testDto = TemplateDto.Create(
            "This is a test: {{test}}",
            new Dictionary<string, TemplateVariable>()
            {
                {
                    "test",
                    new TemplateVariable { Id = "test", Source = VariableSource.NumberValue, Round = "round(1)" }
                }
            });
        var someOtherData = TemplateDto.Create(
            "{{here}}",
            new Dictionary<string, TemplateVariable>()
            {
                {
                    "here",
                    new TemplateVariable { Id = "1.2.3", Source = VariableSource.StringValue }
                }
            });

        var engine = new TemplateEngine(items);

        // Act
        var result = engine.ProcessTemplate(testDto);
        var helloWorld = engine.ProcessTemplate(someOtherData);


        // Assert 
        Assert.Equal("This is a test: 42.5", result);
        Assert.Equal("Hello World", helloWorld);
    }


    [Fact]
    public void ProcessTemplate_WithConversionAndRounding_AppliesModifiersInCorrectOrder()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "speed", NumericValue = 100.0, Unit = "km/h" }
        };

        var engine = new TemplateEngine(items);

        var templateDto = TemplateDto.Create(
            "Speed: {{speedValue}} {{speedUnit}}",
            new Dictionary<string, TemplateVariable>
            {
                ["speedValue"] = new()
                {
                    Id = "speed",
                    Source = VariableSource.NumberValue,
                    Convert = "mph",
                    Round = "round(1)"
                },
                ["speedUnit"] = new()
                {
                    Id = "speed",
                    Source = VariableSource.Unit
                }
            });

        // Act
        var result = engine.ProcessTemplate(templateDto);

        // Assert - 100 km/h = ~62.137 mph, rounded to 1 decimal place = 62.1
        Assert.Equal("Speed: 62.1 km/h", result);
    }

    [Fact]
    public void ProcessTemplate_WithRoundingOnly_AppliesRoundingCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "pi", NumericValue = 3.14159, Unit = "" }
        };

        var engine = new TemplateEngine(items);

        var templateDto = TemplateDto.Create(
            "Pi: {{piValue}}",
            new Dictionary<string, TemplateVariable>
            {
                ["piValue"] = new()
                {
                    Id = "pi",
                    Source = VariableSource.NumberValue,
                    Round = "round(2)"
                }
            });

        // Act
        var result = engine.ProcessTemplate(templateDto);

        // Assert
        Assert.Equal("Pi: 3.14", result);
    }

    [Fact]
    public void ProcessTemplate_WithConversionOnly_AppliesConversionCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "speed", NumericValue = 100.0, Unit = "km/h" }
        };

        var engine = new TemplateEngine(items);

        var templateDto = TemplateDto.Create(
            "Speed: {{speedValue}}",
            new Dictionary<string, TemplateVariable>
            {
                ["speedValue"] = new()
                {
                    Id = "speed",
                    Source = VariableSource.NumberValue,
                    Convert = "mph"
                }
            });

        // Act
        var result = engine.ProcessTemplate(templateDto);

        // Assert - 100 km/h = 62.137100000000004 mph
        Assert.Equal("Speed: 62.137100000000004", result);
    }
}

// Test helper class
public class TestCustomModifier : IValueModifier
{
    public bool CanHandle(string modifierString) => modifierString == "test";

    public void Apply(ModifierContext context, string modifierString)
    {
        context.Value = context.Value * 2;
    }
}