using Xunit;
using TemplateEngine.Modifiers;

namespace TemplateEngine.Tests;

public class ModifierTests
{
    [Fact]
    public void RoundModifier_CanHandle_RecognizesRoundModifier()
    {
        // Arrange
        var modifier = new RoundModifier();
        
        // Act & Assert
        Assert.True(modifier.CanHandle("round(2)"));
        Assert.True(modifier.CanHandle("ROUND(0)"));
        Assert.False(modifier.CanHandle("floor"));
        Assert.False(modifier.CanHandle("convert(mph)"));
    }

    [Fact]
    public void RoundModifier_Apply_RoundsCorrectly()
    {
        // Arrange
        var modifier = new RoundModifier();
        var context = new ModifierContext(3.14159, "");
        
        // Act
        modifier.Apply(context, "round(2)");
        
        // Assert
        Assert.Equal(3.14, context.Value);
    }

    [Fact]
    public void ConvertModifier_CanHandle_RecognizesConvertModifier()
    {
        // Arrange
        var modifier = new ConvertModifier();
        
        // Act & Assert
        Assert.True(modifier.CanHandle("convert(mph)"));
        Assert.True(modifier.CanHandle("CONVERT(fahrenheit)"));
        Assert.False(modifier.CanHandle("round(2)"));
        Assert.False(modifier.CanHandle("floor"));
    }

    [Fact]
    public void ModifierProcessor_ProcessModifier_AppliesRoundModifier()
    {
        // Arrange
        var processor = new ModifierProcessor();
        
        // Act
        var result = processor.ProcessModifier(3.14159, "", "round(2)");
        
        // Assert
        Assert.Equal("3.14", result);
    }
    
    [Fact]
    public void ModifierProcessor_ProcessModifier_AppliesConvertModifier()
    {
        // Arrange
        var processor = new ModifierProcessor();
        
        // Act
        var result = processor.ProcessModifier(100.0, "km/h", "convert(mph)");
        
        // Assert
        Assert.Equal("62.137100000000004", result);
    }
}
