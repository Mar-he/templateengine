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
        
        var templateDto = new TemplateDto
        {
            TemplateLiteral = "Value: {{value}}",
            Variables = new Dictionary<string, TemplateVariable>
            {
                ["value"] = new() { Id = "test", Source = "number_value" }
            }
        };
        
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
