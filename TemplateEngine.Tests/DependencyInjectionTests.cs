using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using TemplateEngine.Extensions;
using TemplateEngine.Modifiers;
using Xunit;

namespace TemplateEngine.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddTemplateEngine_WithJsonData_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsonData = """
        [{
          "category":"test",
          "numeric_value": 42,
          "unit": "kg"
        }]
        """;

        // Act
        services.AddTemplateEngine(jsonData);
        var serviceProvider = services.BuildServiceProvider();
        var templateEngine = serviceProvider.GetRequiredService<ITemplateEngine>();

        // Assert
        Assert.NotNull(templateEngine);
        
        var templateDto = TemplateDto.Create(
            "Value: {{value}} {{unit}}",
            new Dictionary<string, TemplateVariable>
            {
                ["value"] = new() { Id = "test", Source = VariableSource.NumberValue },
                ["unit"] = new() { Id = "test", Source = VariableSource.Unit }
            });
        
        var result = templateEngine.ProcessTemplate(templateDto);
        Assert.Equal("Value: 42 kg", result);
    }

    [Fact]
    public void AddTemplateEngine_WithJsonDataAndOptions_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsonData = """
        [{
          "category":"test",
          "numeric_value": 1234.56,
          "unit": "kg"
        }]
        """;

        // Act
        services.AddTemplateEngine(jsonData, options =>
        {
            options.Culture = new CultureInfo("de-DE");
        });

        var serviceProvider = services.BuildServiceProvider();
        var templateEngine = serviceProvider.GetRequiredService<ITemplateEngine>();

        // Assert
        Assert.NotNull(templateEngine);
        
        var templateDto = TemplateDto.Create(
            "Value: {{value}}",
            new Dictionary<string, TemplateVariable>
            {
                ["value"] = new() { Id = "test", Source = VariableSource.NumberValue }
            });
        
        var result = templateEngine.ProcessTemplate(templateDto);
        Assert.Equal("Value: 1234,56", result); // German culture uses comma as decimal separator
    }

    [Fact]
    public void AddTemplateEngine_WithTemplateItems_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var items = new List<TemplateItem>
        {
            new() { Category = "test", NumericValue = 100, Unit = "m" }
        };

        // Act
        services.AddTemplateEngine(items);
        var serviceProvider = services.BuildServiceProvider();
        var templateEngine = serviceProvider.GetRequiredService<ITemplateEngine>();

        // Assert
        Assert.NotNull(templateEngine);
        
        var templateDto = TemplateDto.Create(
            "Distance: {{distance}} {{unit}}",
            new Dictionary<string, TemplateVariable>
            {
                ["distance"] = new() { Id = "test", Source = VariableSource.NumberValue },
                ["unit"] = new() { Id = "test", Source = VariableSource.Unit }
            });
        
        var result = templateEngine.ProcessTemplate(templateDto);
        Assert.Equal("Distance: 100 m", result);
    }

    [Fact]
    public void AddTemplateEngine_WithTemplateItemsAndCulture_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var items = new List<TemplateItem>
        {
            new() { Category = "test", NumericValue = 9876.54 }
        };

        // Act
        services.AddTemplateEngine(items, new CultureInfo("fr-FR"));
        var serviceProvider = services.BuildServiceProvider();
        var templateEngine = serviceProvider.GetRequiredService<ITemplateEngine>();

        // Assert
        Assert.NotNull(templateEngine);
        
        var templateDto = TemplateDto.Create(
            "Valeur: {{value}}",
            new Dictionary<string, TemplateVariable>
            {
                ["value"] = new() { Id = "test", Source = VariableSource.NumberValue }
            });
        
        var result = templateEngine.ProcessTemplate(templateDto);
        Assert.Equal("Valeur: 9876,54", result); // French culture uses comma as decimal separator
    }

    [Fact]
    public void AddTemplateEngine_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsonData = """[{"category":"test", "numeric_value": 42}]""";

        // Act
        services.AddTemplateEngine(jsonData);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var instance1 = serviceProvider.GetRequiredService<ITemplateEngine>();
        var instance2 = serviceProvider.GetRequiredService<ITemplateEngine>();
        Assert.Same(instance1, instance2); // Should be the same instance (singleton)
    }
}
