using Xunit;
using System.Text.Json;
using System.Globalization;

namespace TemplateEngine.Tests;

public class TemplateEngineTests
{
    [Fact]
    public void ProcessTemplate_WithNumericValue_ReplacesTokenCorrectly()
    {
        // Arrange
        var jsonData = """
        [{
          "category":"foo",
          "numeric_value": 2,
          "unit": "kg"
        }]
        """;
        
        var engine = new TemplateEngine(jsonData);
        var template = "The weight is {{foo.value}} {{foo.unit}}";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert
        Assert.Equal("The weight is 2 kg", result);
    }

    [Fact]
    public void ProcessTemplate_WithStringValue_ReplacesTokenCorrectly()
    {
        // Arrange
        var jsonData = """
        [{
          "category":"foo",
          "string_value": "electric power"
        }]
        """;
        
        var engine = new TemplateEngine(jsonData);
        var template = "The type is {{foo.value}}";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert
        Assert.Equal("The type is electric power", result);
    }

    [Fact]
    public void ProcessTemplate_WithMultipleItems_ReplacesAllTokens()
    {
        // Arrange
        var jsonData = """
        [{
          "category":"weight",
          "numeric_value": 2.5,
          "unit": "kg"
        }, {
          "category":"type",
          "string_value": "electric power"
        }]
        """;
        
        var engine = new TemplateEngine(jsonData);
        var template = "Item: {{type.value}}, Weight: {{weight.value}} {{weight.unit}}";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert
        Assert.Equal("Item: electric power, Weight: 2.5 kg", result);
    }

    [Fact]
    public void ProcessTemplate_WithNameToken_ReplacesCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "foo", NumericValue = 42, Unit = "units" }
        };
        
        var engine = new TemplateEngine(items);
        var template = "Name: {{foo.name}}, Value: {{foo.value}}";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert
        Assert.Equal("Name: foo, Value: 42", result);
    }

    [Fact]
    public void ProcessTemplate_WithNonExistentItem_LeavesTokenUnchanged()
    {
        // Arrange
        var jsonData = """
        [{
          "category":"foo",
          "numeric_value": 2,
          "unit": "kg"
        }]
        """;
        
        var engine = new TemplateEngine(jsonData);
        var template = "Value: {{bar.value}}";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert
        Assert.Equal("Value: {{bar.value}}", result);
    }

    [Fact]
    public void ProcessTemplate_WithNonExistentProperty_LeavesTokenUnchanged()
    {
        // Arrange
        var jsonData = """
        [{
          "category":"foo",
          "numeric_value": 2,
          "unit": "kg"
        }]
        """;
        
        var engine = new TemplateEngine(jsonData);
        var template = "Value: {{foo.unknown}}";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert
        Assert.Equal("Value: {{foo.unknown}}", result);
    }

    [Fact]
    public void ProcessTemplate_WithMissingUnit_ReturnsEmptyString()
    {
        // Arrange
        var jsonData = """
        [{
          "category":"foo",
          "string_value": "test"
        }]
        """;
        
        var engine = new TemplateEngine(jsonData);
        var template = "Unit: '{{foo.unit}}'";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert
        Assert.Equal("Unit: ''", result);
    }

    [Fact]
    public void ParseJsonData_CaseInsensitive_ParsesCorrectly()
    {
        // Arrange
        var jsonData = """
        [{
          "CATEGORY":"FOO",
          "NUMERIC_VALUE": 123,
          "UNIT": "KG"
        }]
        """;
        
        var engine = new TemplateEngine(jsonData);
        var template = "{{FOO.value}} {{FOO.unit}}";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert
        Assert.Equal("123 KG", result);
    }

    [Fact]
    public void Constructor_WithCustomJsonOptions_AppliesCustomConfiguration()
    {
        // Arrange
        var jsonData = """
        [{
          "myCustomName":"test_item",
          "myCustomNumericValue": 42.5,
          "myCustomUnit": "customUnit"
        }]
        """;
        
        var options = new TemplateEngineOptions
        {
            ConfigureJsonOptions = jsonOptions =>
            {
                // Override the default naming policy to use camel case
                jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            }
        };
        
        // Act
        var engine = new TemplateEngine(jsonData, options);
        
        // Since we changed to camel case and the JSON properties don't match our C# properties,
        // this should result in a TemplateItem with default values
        var items = engine.GetItems();
        
        // Assert - Should have one item but with default/null values since property mapping failed
        Assert.Single(items);
        Assert.True(string.IsNullOrEmpty(items[0].Category));
        Assert.Null(items[0].NumericValue);
        Assert.Null(items[0].StringValue);
        Assert.Null(items[0].Unit);
    }

    [Fact]
    public void Constructor_WithoutCustomJsonOptions_UsesDefaults()
    {
        // Arrange
        var jsonData = """
        [{
          "category":"test_item",
          "numeric_value": 42.5,
          "unit": "kg"
        }]
        """;
        
        // Act - No custom options provided
        var engine = new TemplateEngine(jsonData);
        var items = engine.GetItems();
        
        // Assert - Should parse correctly with default options
        Assert.Single(items);
        Assert.Equal("test_item", items[0].Category);
        Assert.Equal(42.5, items[0].NumericValue);
        Assert.Equal("kg", items[0].Unit);
    }

    [Fact]
    public void Constructor_WithCustomJsonOptions_PreservesDefaults()
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
                // Add a custom option while preserving defaults
                jsonOptions.AllowTrailingCommas = true;
                jsonOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            }
        };
        
        // Act - Custom options that don't interfere with existing functionality
        var engine = new TemplateEngine(jsonData, options);
        
        var items = engine.GetItems();
        
        // Assert - Should still work with default case insensitive and snake_case settings
        Assert.Single(items);
        Assert.Equal("test_item", items[0].Category);
        Assert.Equal(42.5, items[0].NumericValue);
        Assert.Equal("kg", items[0].Unit);
    }

    [Fact]
    public void Constructor_WithCustomCulture_FormatsNumbersCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "test", NumericValue = 1234.56, Unit = "kg" }
        };
        
        var germanCulture = new CultureInfo("de-DE");
        var engine = new TemplateEngine(items, germanCulture);
        var template = "Value: {{test.value}}";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert - German culture uses comma as decimal separator
        Assert.Equal("Value: 1234,56", result);
    }

    [Fact]
    public void Constructor_WithOptionsAndCulture_AppliesBothCorrectly()
    {
        // Arrange
        var jsonData = """
        [{
          "category":"test_item",
          "numeric_value": 42.75,
          "unit": "kg"
        }]
        """;
        
        var options = new TemplateEngineOptions
        {
            Culture = new CultureInfo("fr-FR"),
            ConfigureJsonOptions = jsonOptions =>
            {
                jsonOptions.AllowTrailingCommas = true;
            }
        };
        
        // Act
        var engine = new TemplateEngine(jsonData, options);
        var template = "Value: {{test_item.value}}";
        var result = engine.ProcessTemplate(template);
        
        // Assert - French culture uses comma as decimal separator
        Assert.Equal("Value: 42,75", result);
    }
}
