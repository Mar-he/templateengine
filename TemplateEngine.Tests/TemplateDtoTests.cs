using Xunit;

namespace TemplateEngine.Tests;

public class TemplateDtoTests
{
    [Fact]
    public void ProcessTemplate_WithTemplateDto_ReplacesVariablesCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "speed", NumericValue = 25.5, Unit = "km/h" },
            new() { Category = "type", StringValue = "electric vehicle" }
        };
        
        var engine = new TemplateEngine(items);
        
        var templateDto = TemplateDto.Create(
            "{{speedValue}} {{speedUnit}} {{vehicleType}}",
            new Dictionary<string, TemplateVariable>
            {
                ["speedValue"] = new() { Id = "speed", Source = VariableSource.NumberValue },
                ["speedUnit"] = new() { Id = "speed", Source = VariableSource.Unit },
                ["vehicleType"] = new() { Id = "type", Source = VariableSource.StringValue }
            });
        
        // Act
        var result = engine.ProcessTemplate(templateDto);
        
        // Assert
        Assert.Equal("25.5 km/h electric vehicle", result);
    }

    [Fact]
    public void ProcessTemplate_WithTemplateDto_NumberValueSource_ReplacesCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "temperature", NumericValue = 25.789, Unit = "°C" }
        };
        
        var engine = new TemplateEngine(items);
        
        var templateDto = TemplateDto.Create(
            "Temperature: {{temp}}",
            new Dictionary<string, TemplateVariable>
            {
                ["temp"] = new() { Id = "temperature", Source = VariableSource.NumberValue }
            });
        
        // Act
        var result = engine.ProcessTemplate(templateDto);
        
        // Assert
        Assert.Equal("Temperature: 25.789", result);
    }

    [Fact]
    public void ProcessTemplate_WithTemplateDto_StringValueSource_ReplacesCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "description", StringValue = "test description" }
        };
        
        var engine = new TemplateEngine(items);
        
        var templateDto = TemplateDto.Create(
            "Description: {{desc}}",
            new Dictionary<string, TemplateVariable>
            {
                ["desc"] = new() { Id = "description", Source = VariableSource.StringValue }
            });
        
        // Act
        var result = engine.ProcessTemplate(templateDto);
        
        // Assert
        Assert.Equal("Description: test description", result);
    }

    [Fact]
    public void ProcessTemplate_WithTemplateDto_UnitSource_ReplacesCorrectly()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "speed", NumericValue = 100, Unit = "km/h" }
        };
        
        var engine = new TemplateEngine(items);
        
        var templateDto = TemplateDto.Create(
            "Unit: {{unit}}",
            new Dictionary<string, TemplateVariable>
            {
                ["unit"] = new() { Id = "speed", Source = VariableSource.Unit }
            });
        
        // Act
        var result = engine.ProcessTemplate(templateDto);
        
        // Assert
        Assert.Equal("Unit: km/h", result);
    }

    [Fact]
    public void ProcessTemplate_WithTemplateDto_NonExistentItem_LeavesTokenUnchanged()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "existing", NumericValue = 25.5 }
        };
        
        var engine = new TemplateEngine(items);
        
        var templateDto = TemplateDto.Create(
            "{{existingItem}} and {{nonExistentItem}}",
            new Dictionary<string, TemplateVariable>
            {
                ["existingItem"] = new() { Id = "existing", Source = VariableSource.NumberValue },
                ["nonExistentItem"] = new() { Id = "nonexistent", Source = VariableSource.NumberValue }
            });
        
        // Act
        var result = engine.ProcessTemplate(templateDto);
        
        // Assert
        Assert.Equal("25.5 and {{nonExistentItem}}", result);
    }

    [Fact]
    public void ProcessTemplate_WithTemplateDto_InvalidDto_UndefinedVariable_ThrowsArgumentException()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "test", NumericValue = 25.5 }
        };
        
        var engine = new TemplateEngine(items);
        
        // This test should now fail at TemplateDto.Create() due to validation
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            TemplateDto.Create(
                "{{definedVar}} and {{undefinedVar}}",
                new Dictionary<string, TemplateVariable>
                {
                    ["definedVar"] = new() { Id = "test", Source = VariableSource.NumberValue }
                    // undefinedVar is missing from Variables dictionary
                }));
        Assert.Contains("undefinedVar", ex.Message);
        Assert.Contains("not defined in Variables", ex.Message);
    }

    [Fact]
    public void ProcessTemplate_WithTemplateDto_EmptyVariablesDictionary_ValidatesCorrectly()
    {
        // Arrange
        var engine = new TemplateEngine(new List<TemplateItem>());
        
        var templateDto = TemplateDto.Create(
            "No variables here",
            new Dictionary<string, TemplateVariable>());
        
        // Act
        var result = engine.ProcessTemplate(templateDto);
        
        // Assert
        Assert.Equal("No variables here", result);
    }

    [Fact]
    public void ProcessTemplate_WithTemplateDto_MultipleOccurrencesOfSameVariable_ReplacesAll()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "value", NumericValue = 42 }
        };
        
        var engine = new TemplateEngine(items);
        
        var templateDto = TemplateDto.Create(
            "{{val}} + {{val}} = {{val}}",
            new Dictionary<string, TemplateVariable>
            {
                ["val"] = new() { Id = "value", Source = VariableSource.NumberValue }
            });
        
        // Act
        var result = engine.ProcessTemplate(templateDto);
        
        // Assert
        Assert.Equal("42 + 42 = 42", result);
    }

    [Fact]
    public void ProcessTemplate_WithNullTemplateDto_ThrowsArgumentNullException()
    {
        // Arrange
        var engine = new TemplateEngine(new List<TemplateItem>());
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => engine.ProcessTemplate((TemplateDto)null!));
    }

    [Fact]
    public void ProcessTemplate_WithEmptyTemplateLiteral_ThrowsArgumentException()
    {
        // Arrange
        var engine = new TemplateEngine(new List<TemplateItem>());
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TemplateDto.Create("", new Dictionary<string, TemplateVariable>()));
    }

    [Fact]
    public void ProcessTemplate_WithNullTemplateLiteral_ThrowsArgumentException()
    {
        // Arrange
        var engine = new TemplateEngine(new List<TemplateItem>());
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TemplateDto.Create(null!, new Dictionary<string, TemplateVariable>()));
    }

    [Fact]
    public void ProcessTemplate_WithTemplateDto_MissingUnitProperty_ReturnsEmptyString()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Category = "test", NumericValue = 42 } // No Unit property set
        };
        
        var engine = new TemplateEngine(items);
        
        var templateDto = TemplateDto.Create(
            "Value: {{value}}, Unit: '{{unit}}'",
            new Dictionary<string, TemplateVariable>
            {
                ["value"] = new() { Id = "test", Source = VariableSource.NumberValue },
                ["unit"] = new() { Id = "test", Source = VariableSource.Unit }
            });
        
        // Act
        var result = engine.ProcessTemplate(templateDto);
        
        // Assert
        Assert.Equal("Value: 42, Unit: ''", result);
    }
}
