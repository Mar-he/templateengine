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
          "name":"test",
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
        var result = templateEngine.ProcessTemplate("Value: {{test.value}} {{test.unit}}");
        Assert.Equal("Value: 42 kg", result);
    }

    [Fact]
    public void AddTemplateEngine_WithJsonDataAndOptions_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsonData = """
        [{
          "name":"test",
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
        var result = templateEngine.ProcessTemplate("Value: {{test.value}}");
        Assert.Equal("Value: 1234,56", result); // German culture uses comma as decimal separator
    }

    [Fact]
    public void AddTemplateEngine_WithItems_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var items = new List<TemplateItem>
        {
            new() { Name = "speed", NumericValue = 100, Unit = "km/h" }
        };

        // Act
        services.AddTemplateEngine(items);
        var serviceProvider = services.BuildServiceProvider();
        var templateEngine = serviceProvider.GetRequiredService<ITemplateEngine>();

        // Assert
        Assert.NotNull(templateEngine);
        var result = templateEngine.ProcessTemplate("Speed: {{speed.value}} {{speed.unit}}");
        Assert.Equal("Speed: 100 km/h", result);
    }

    [Fact]
    public void AddTemplateEngine_WithItemsAndCulture_AppliesCulture()
    {
        // Arrange
        var services = new ServiceCollection();
        var items = new List<TemplateItem>
        {
            new() { Name = "price", NumericValue = 1234.56, Unit = "EUR" }
        };

        // Act
        services.AddTemplateEngine(items, new CultureInfo("fr-FR"));
        var serviceProvider = services.BuildServiceProvider();
        var templateEngine = serviceProvider.GetRequiredService<ITemplateEngine>();

        // Assert
        var result = templateEngine.ProcessTemplate("Price: {{price.value}}");
        Assert.Equal("Price: 1234,56", result); // French culture uses comma as decimal separator
    }

    [Fact]
    public void AddTemplateEngine_WithFactory_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var items = new List<TemplateItem>
        {
            new() { Name = "test", StringValue = "factory created" }
        };

        // Act
        services.AddTemplateEngine(provider => new TemplateEngine(items));
        var serviceProvider = services.BuildServiceProvider();
        var templateEngine = serviceProvider.GetRequiredService<ITemplateEngine>();

        // Assert
        Assert.NotNull(templateEngine);
        var result = templateEngine.ProcessTemplate("Value: {{test.value}}");
        Assert.Equal("Value: factory created", result);
    }

    [Fact]
    public void AddTemplateEngine_WithBuilder_ConfiguresCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsonData = """
        [{
          "name":"speed",
          "numeric_value": 100,
          "unit": "km/h"
        }]
        """;

        // Act
        services.AddTemplateEngine(builder =>
        {
            builder.UseJsonData(jsonData)
                   .UseCulture(new CultureInfo("en-US"))
                   .AddModifier<TestDoubleModifier>();
        });

        var serviceProvider = services.BuildServiceProvider();
        var templateEngine = serviceProvider.GetRequiredService<ITemplateEngine>();

        // Assert
        Assert.NotNull(templateEngine);
        var result = templateEngine.ProcessTemplate("Speed: {{speed.value:double}}");
        Assert.Equal("Speed: 200", result);
    }

    [Fact]
    public void AddTemplateEngine_WithBuilderAndItems_ConfiguresCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var items = new List<TemplateItem>
        {
            new() { Name = "temp", NumericValue = 25.5, Unit = "celsius" }
        };

        // Act
        services.AddTemplateEngine(builder =>
        {
            builder.UseItems(items)
                   .UseCulture(new CultureInfo("de-DE"))
                   .AddModifier(new TestDoubleModifier());
        });

        var serviceProvider = services.BuildServiceProvider();
        var templateEngine = serviceProvider.GetRequiredService<ITemplateEngine>();

        // Assert
        var result = templateEngine.ProcessTemplate("Temp: {{temp.value:double}}");
        Assert.Equal("Temp: 51", result); // 25.5 * 2 = 51, with German culture (no decimals in this case)
    }

    [Fact]
    public void AddTemplateEngine_WithBuilderJsonOptions_ConfiguresCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsonData = """
        [{
          "name":"test",
          "numeric_value": 42.5,
          "unit": "kg"
        }]
        """;

        // Act
        services.AddTemplateEngine(builder =>
        {
            builder.UseJsonData(jsonData)
                   .ConfigureJsonOptions(options =>
                   {
                       options.AllowTrailingCommas = true;
                       options.ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip;
                   });
        });

        var serviceProvider = services.BuildServiceProvider();
        var templateEngine = serviceProvider.GetRequiredService<ITemplateEngine>();

        // Assert
        Assert.NotNull(templateEngine);
        var result = templateEngine.ProcessTemplate("Value: {{test.value}}");
        Assert.Equal("Value: 42.5", result);
    }

    [Fact]
    public void AddTemplateEngine_BuilderWithoutDataOrItems_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTemplateEngine(builder =>
        {
            builder.UseCulture(new CultureInfo("en-US"));
            // No UseJsonData or UseItems called
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.Throws<InvalidOperationException>(() =>
            serviceProvider.GetRequiredService<ITemplateEngine>());
    }

    [Fact]
    public void AddTemplateEngine_MultipleRegistrations_UsesFirstRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsonData1 = """[{"name":"first","string_value":"first value"}]""";
        var jsonData2 = """[{"name":"second","string_value":"second value"}]""";

        // Act - Add multiple registrations
        services.AddTemplateEngine(jsonData1);
        services.AddTemplateEngine(jsonData2); // This should be ignored due to TryAddSingleton

        var serviceProvider = services.BuildServiceProvider();
        var templateEngine = serviceProvider.GetRequiredService<ITemplateEngine>();

        // Assert - Should use the first registration
        var result = templateEngine.ProcessTemplate("{{first.value}}");
        Assert.Equal("first value", result);
        
        var result2 = templateEngine.ProcessTemplate("{{second.value}}");
        Assert.Equal("{{second.value}}", result2); // Should remain unchanged as item doesn't exist
    }

    [Fact]
    public void AddTemplateEngine_AsSingleton_ReturnsSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsonData = """[{"name":"test","string_value":"singleton test"}]""";

        // Act
        services.AddTemplateEngine(jsonData);
        var serviceProvider = services.BuildServiceProvider();
        
        var instance1 = serviceProvider.GetRequiredService<ITemplateEngine>();
        var instance2 = serviceProvider.GetRequiredService<ITemplateEngine>();

        // Assert
        Assert.Same(instance1, instance2);
    }
}

// Test helper class for DI tests
public class TestDoubleModifier : IValueModifier
{
    public bool CanHandle(string modifierString)
    {
        return modifierString.Equals("double", StringComparison.OrdinalIgnoreCase);
    }

    public void Apply(ModifierContext context, string modifierString)
    {
        context.Value *= 2;
    }
}
