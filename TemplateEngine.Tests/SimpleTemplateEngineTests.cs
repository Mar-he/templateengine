using Xunit;
using System.Text.Json;

namespace TemplateEngine.Tests;

public class SimpleTemplateEngineTests
{
    [Fact]
    public void ProcessTemplate_WithNumericValue_ReplacesTokenCorrectly()
    {
        // Arrange
        var jsonData = """
        [{
          "name":"foo",
          "numeric_value": 2,
          "unit": "kg"
        }]
        """;
        
        var engine = new SimpleTemplateEngine(jsonData);
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
          "name":"foo",
          "string_value": "electric power"
        }]
        """;
        
        var engine = new SimpleTemplateEngine(jsonData);
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
          "name":"weight",
          "numeric_value": 2.5,
          "unit": "kg"
        }, {
          "name":"type",
          "string_value": "electric power"
        }]
        """;
        
        var engine = new SimpleTemplateEngine(jsonData);
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
            new() { Name = "foo", NumericValue = 42, Unit = "units" }
        };
        
        var engine = new SimpleTemplateEngine(items);
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
          "name":"foo",
          "numeric_value": 2,
          "unit": "kg"
        }]
        """;
        
        var engine = new SimpleTemplateEngine(jsonData);
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
          "name":"foo",
          "numeric_value": 2,
          "unit": "kg"
        }]
        """;
        
        var engine = new SimpleTemplateEngine(jsonData);
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
          "name":"foo",
          "string_value": "test"
        }]
        """;
        
        var engine = new SimpleTemplateEngine(jsonData);
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
          "NAME":"FOO",
          "NUMERIC_VALUE": 123,
          "UNIT": "KG"
        }]
        """;
        
        var engine = new SimpleTemplateEngine(jsonData);
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
        
        // Act
        var engine = new SimpleTemplateEngine(jsonData, options =>
        {
            // Override the default naming policy to use camel case
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        
        // Since we changed to camel case and the JSON properties don't match our C# properties,
        // this should result in a TemplateItem with default values
        var items = engine.GetItems();
        
        // Assert - Should have one item but with default/null values since property mapping failed
        Assert.Single(items);
        Assert.True(string.IsNullOrEmpty(items[0].Name));
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
          "name":"test_item",
          "numeric_value": 42.5,
          "unit": "kg"
        }]
        """;
        
        // Act - No custom options provided
        var engine = new SimpleTemplateEngine(jsonData);
        var items = engine.GetItems();
        
        // Assert - Should parse correctly with default options
        Assert.Single(items);
        Assert.Equal("test_item", items[0].Name);
        Assert.Equal(42.5, items[0].NumericValue);
        Assert.Equal("kg", items[0].Unit);
    }

    [Fact]
    public void Constructor_WithCustomJsonOptions_PreservesDefaults()
    {
        // Arrange
        var jsonData = """
        [{
          "name":"test_item",
          "numeric_value": 42.5,
          "unit": "kg"
        }]
        """;
        
        // Act - Custom options that don't interfere with existing functionality
        var engine = new SimpleTemplateEngine(jsonData, options =>
        {
            // Add a custom option while preserving defaults
            options.AllowTrailingCommas = true;
            options.ReadCommentHandling = JsonCommentHandling.Skip;
        });
        
        var items = engine.GetItems();
        
        // Assert - Should still work with default case insensitive and snake_case settings
        Assert.Single(items);
        Assert.Equal("test_item", items[0].Name);
        Assert.Equal(42.5, items[0].NumericValue);
        Assert.Equal("kg", items[0].Unit);
    }
}
