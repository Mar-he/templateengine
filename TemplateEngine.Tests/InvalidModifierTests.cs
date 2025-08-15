using TemplateEngine.Modifiers;
using Xunit;

namespace TemplateEngine.Tests;

public class InvalidModifierTests
{
    [Fact]
    public void ProcessModifier_WithInvalidModifier_ThrowsInvalidModifierException()
    {
        // Arrange
        var processor = new ModifierProcessor();
        
        // Act & Assert
        var exception = Assert.Throws<InvalidModifierException>(() => 
            processor.ProcessModifier(100.0, "km/h", "invalidmodifier"));
        
        Assert.Contains("invalidmodifier", exception.Message);
    }

    [Fact]
    public void ProcessModifier_WithInvalidRoundParameter_ThrowsInvalidModifierException()
    {
        // Arrange
        var processor = new ModifierProcessor();
        
        // Act & Assert
        var exception = Assert.Throws<InvalidModifierException>(() => 
            processor.ProcessModifier(100.0, "", "round(invalid)"));
        
        Assert.Contains("round(invalid)", exception.Message);
    }

    [Fact]
    public void ProcessModifier_WithInvalidConvertUnit_ThrowsInvalidModifierException()
    {
        // Arrange
        var processor = new ModifierProcessor();
        
        // Act & Assert
        var exception = Assert.Throws<InvalidModifierException>(() => 
            processor.ProcessModifier(100.0, "km/h", "convert(invalidunit)"));
        
        Assert.Contains("No conversion available", exception.Message);
    }
}
