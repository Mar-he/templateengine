using Xunit;
using System.Text.Json;

namespace TemplateEngine.Tests;

public class TemplateEngineTests
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
        var template = "The weight is {foo.value} {foo.unit}";
        
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
        var template = "The type is {foo.value}";
        
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
        var template = "Item: {type.value}, Weight: {weight.value} {weight.unit}";
        
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
        var template = "Name: {foo.name}, Value: {foo.value}";
        
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
        var template = "Value: {bar.value}";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert
        Assert.Equal("Value: {bar.value}", result);
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
        var template = "Value: {foo.unknown}";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert
        Assert.Equal("Value: {foo.unknown}", result);
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
        var template = "Unit: '{foo.unit}'";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert
        Assert.Equal("Unit: ''", result);
    }

    [Fact]
    public void TemplateItem_Value_ReturnsStringValueWhenBothExist()
    {
        // Arrange
        var item = new TemplateItem
        {
            Name = "test",
            StringValue = "string",
            NumericValue = 42
        };
        
        // Act & Assert
        Assert.Equal("string", item.Value);
    }

    [Fact]
    public void TemplateItem_Value_ReturnsNumericValueWhenStringIsNull()
    {
        // Arrange
        var item = new TemplateItem
        {
            Name = "test",
            StringValue = null,
            NumericValue = 42
        };
        
        // Act & Assert
        Assert.Equal(42.0, item.Value);
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
        var template = "{FOO.value} {FOO.unit}";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert
        Assert.Equal("123 KG", result);
    }

    [Fact]
    public void ProcessTemplate_WithRounding_RoundsCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 123.456789, Unit = "km/h" }
        };
        
        var engine = new SimpleTemplateEngine(items);
        
        // Act & Assert
        Assert.Equal("123.46", engine.ProcessTemplate("{speed.value:round(2)}"));
        Assert.Equal("123", engine.ProcessTemplate("{speed.value:round(0)}"));
        Assert.Equal("123.4568", engine.ProcessTemplate("{speed.value:round(4)}"));
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
        
        var engine = new SimpleTemplateEngine(items);
        
        // Act & Assert - Use rounded values to avoid floating point precision issues
        Assert.Equal("62.1371", engine.ProcessTemplate("{speed.value:convert(mph):round(4)}"));
        Assert.Equal("27.7", engine.ProcessTemplate("{consumption.value:convert(mpg):round(1)}"));
        Assert.Equal("77", engine.ProcessTemplate("{temp.value:convert(fahrenheit):round(0)}"));
    }

    [Fact]
    public void ProcessTemplate_WithConvertAndRound_AppliesInOrder()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 100, Unit = "km/h" }
        };
        
        var engine = new SimpleTemplateEngine(items);
        
        // Act & Assert
        Assert.Equal("62.1", engine.ProcessTemplate("{speed.value:convert(mph):round(1)}"));
        Assert.Equal("62", engine.ProcessTemplate("{speed.value:convert(mph):round(0)}"));
    }

    [Fact]
    public void ProcessTemplate_WithRoundAndConvert_AppliesInOrder()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 100.789, Unit = "km/h" }
        };
        
        var engine = new SimpleTemplateEngine(items);
        
        // Act & Assert
        // First round to 101, then convert to mph
        Assert.Equal("62.8", engine.ProcessTemplate("{speed.value:round(0):convert(mph):round(1)}"));
    }

    [Fact]
    public void ProcessTemplate_WithInvalidConversion_ReturnsOriginalValue()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "weight", NumericValue = 100, Unit = "kg" }
        };
        
        var engine = new SimpleTemplateEngine(items);
        
        // Act - Try to convert kg to mph (invalid conversion)
        var result = engine.ProcessTemplate("{weight.value:convert(mph)}");
        
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
        
        var engine = new SimpleTemplateEngine(items);
        
        // Act
        var result = engine.ProcessTemplate("{type.value:round(2)}");
        
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
        
        var engine = new SimpleTemplateEngine(jsonData);
        var template = "Speed: {car_speed.value:convert(mph):round(1)} mph, " +
                      "Consumption: {fuel_consumption.value:convert(mpg):round(1)} mpg, " +
                      "Temp: {temperature.value:convert(fahrenheit):round(0)}°F";
        
        // Act
        var result = engine.ProcessTemplate(template);
        
        // Assert - 22.5°C = 72.5°F, rounded to 72°F (banker's rounding)
        Assert.Equal("Speed: 74.9 mph, Consumption: 32.7 mpg, Temp: 72°F", result);
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

public class UnitConverterTests
{
    [Theory]
    [InlineData("km/h", "mph", 100, 62.1371)]
    [InlineData("km/h", "m/s", 36, 10)]
    [InlineData("l/100km", "mpg", 8, 29.401822875)]
    [InlineData("celsius", "fahrenheit", 0, 32)]
    [InlineData("celsius", "fahrenheit", 100, 212)]
    [InlineData("kg", "lbs", 1, 2.20462)]
    [InlineData("m", "ft", 1, 3.28084)]
    public void Convert_ValidConversions_ReturnsExpectedResult(string fromUnit, string toUnit, double input, double expected)
    {
        // Act
        var result = UnitConverter.Convert(input, fromUnit, toUnit);
        
        // Assert
        Assert.Equal(expected, result, 5); // 5 decimal places precision
    }

    [Fact]
    public void Convert_InvalidConversion_ReturnsOriginalValue()
    {
        // Act
        var result = UnitConverter.Convert(100, "kg", "mph");
        
        // Assert
        Assert.Equal(100, result);
    }

    [Fact]
    public void CanConvert_ValidConversion_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(UnitConverter.CanConvert("km/h", "mph"));
        Assert.True(UnitConverter.CanConvert("l/100km", "mpg"));
        Assert.True(UnitConverter.CanConvert("celsius", "fahrenheit"));
    }

    [Fact]
    public void CanConvert_InvalidConversion_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(UnitConverter.CanConvert("kg", "mph"));
        Assert.False(UnitConverter.CanConvert("nonexistent", "mph"));
    }

    [Fact]
    public void GetAvailableConversions_ValidUnit_ReturnsConversions()
    {
        // Act
        var conversions = UnitConverter.GetAvailableConversions("km/h").ToList();
        
        // Assert
        Assert.Contains("mph", conversions);
        Assert.Contains("m/s", conversions);
        Assert.Contains("knots", conversions);
    }

    [Fact]
    public void GetAvailableConversions_InvalidUnit_ReturnsEmpty()
    {
        // Act
        var conversions = UnitConverter.GetAvailableConversions("nonexistent").ToList();
        
        // Assert
        Assert.Empty(conversions);
    }
}
