using TemplateEngine.Modifiers;
using Xunit;

namespace TemplateEngine.Tests;

public class InvalidModifierTests
{
    [Fact]
    public void ProcessModifiers_WithInvalidModifier_ThrowsInvalidModifierException()
    {
        // Arrange
        var processor = new ModifierProcessor();
        
        // Act & Assert
        var exception = Assert.Throws<InvalidModifierException>(() => 
            processor.ProcessModifiers(100.0, "km/h", "invalidmodifier"));
        
        Assert.Contains("invalidmodifier", exception.Message);
    }

    [Fact]
    public void ProcessModifiers_WithInvalidRoundParameter_ThrowsInvalidModifierException()
    {
        // Arrange
        var processor = new ModifierProcessor();
        
        // Act & Assert
        var exception = Assert.Throws<InvalidModifierException>(() => 
            processor.ProcessModifiers(100.0, "", "round(invalid)"));
        
        Assert.Contains("round(invalid)", exception.Message);
    }

    [Fact]
    public void ProcessModifiers_WithInvalidConvertUnit_ThrowsInvalidModifierException()
    {
        // Arrange
        var processor = new ModifierProcessor();
        
        // Act & Assert
        var exception = Assert.Throws<InvalidModifierException>(() => 
            processor.ProcessModifiers(100.0, "km/h", "convert(invalidunit)"));
        
        Assert.Contains("No conversion available", exception.Message);
    }
}
