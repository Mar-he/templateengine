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
        
        var templateDto = new TemplateDto
        {
            TemplateLiteral = "{{speedValue}} {{speedUnit}} {{vehicleType}}",
            Variables = new Dictionary<string, TemplateVariable>
            {
                ["speedValue"] = new() { Id = "speed", Source = "number_value" },
                ["speedUnit"] = new() { Id = "speed", Source = "unit" },
                ["vehicleType"] = new() { Id = "type", Source = "string_value" }
            }
        };
        
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
        
        var templateDto = new TemplateDto
        {
            TemplateLiteral = "Temperature: {{temp}}",
            Variables = new Dictionary<string, TemplateVariable>
            {
                ["temp"] = new() { Id = "temperature", Source = "number_value" }
            }
        };
        
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
        
        var templateDto = new TemplateDto
        {
            TemplateLiteral = "Description: {{desc}}",
            Variables = new Dictionary<string, TemplateVariable>
            {
                ["desc"] = new() { Id = "description", Source = "string_value" }
            }
        };
        
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
        
        var templateDto = new TemplateDto
        {
            TemplateLiteral = "Unit: {{unit}}",
            Variables = new Dictionary<string, TemplateVariable>
            {
                ["unit"] = new() { Id = "speed", Source = "unit" }
            }
        };
        
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
        
        var templateDto = new TemplateDto
        {
            TemplateLiteral = "{{existingItem}} and {{nonExistentItem}}",
            Variables = new Dictionary<string, TemplateVariable>
            {
                ["existingItem"] = new() { Id = "existing", Source = "number_value" },
                ["nonExistentItem"] = new() { Id = "nonexistent", Source = "number_value" }
            }
        };
        
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
        
        var templateDto = new TemplateDto
        {
            TemplateLiteral = "{{definedVar}} and {{undefinedVar}}",
            Variables = new Dictionary<string, TemplateVariable>
            {
                ["definedVar"] = new() { Id = "test", Source = "number_value" }
                // undefinedVar is missing from Variables dictionary
            }
        };
        
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => engine.ProcessTemplate(templateDto));
        Assert.Contains("undefinedVar", ex.Message);
        Assert.Contains("not defined in Variables", ex.Message);
    }

    [Fact]
    public void ProcessTemplate_WithTemplateDto_EmptyVariablesDictionary_ValidatesCorrectly()
    {
        // Arrange
        var engine = new TemplateEngine(new List<TemplateItem>());
        
        var templateDto = new TemplateDto
        {
            TemplateLiteral = "No variables here",
            Variables = new Dictionary<string, TemplateVariable>()
        };
        
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
        
        var templateDto = new TemplateDto
        {
            TemplateLiteral = "{{val}} + {{val}} = {{val}}",
            Variables = new Dictionary<string, TemplateVariable>
            {
                ["val"] = new() { Id = "value", Source = "number_value" }
            }
        };
        
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
        var templateDto = new TemplateDto
        {
            TemplateLiteral = "",
            Variables = new Dictionary<string, TemplateVariable>()
        };
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => engine.ProcessTemplate(templateDto));
    }

    [Fact]
    public void ProcessTemplate_WithNullTemplateLiteral_ThrowsArgumentException()
    {
        // Arrange
        var engine = new TemplateEngine(new List<TemplateItem>());
        var templateDto = new TemplateDto
        {
            TemplateLiteral = null!,
            Variables = new Dictionary<string, TemplateVariable>()
        };
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => engine.ProcessTemplate(templateDto));
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
        
        var templateDto = new TemplateDto
        {
            TemplateLiteral = "Value: {{value}}, Unit: '{{unit}}'",
            Variables = new Dictionary<string, TemplateVariable>
            {
                ["value"] = new() { Id = "test", Source = "number_value" },
                ["unit"] = new() { Id = "test", Source = "unit" }
            }
        };
        
        // Act
        var result = engine.ProcessTemplate(templateDto);
        
        // Assert
        Assert.Equal("Value: 42, Unit: ''", result);
    }
}
