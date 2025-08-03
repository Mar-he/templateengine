using TemplateEngine.Modifiers;
using Xunit;

namespace TemplateEngine.Tests;

/// <summary>
/// Tests specifically for InvalidModifierException functionality.
/// </summary>
public class InvalidModifierTests
{
    [Fact]
    public void ModifierProcessor_WithInvalidModifier_ShouldThrowInvalidModifierException()
    {
        // Arrange
        var processor = new ModifierProcessor();
        
        // Act & Assert
        var exception = Assert.Throws<InvalidModifierException>(() =>
        {
            processor.ProcessModifiers(42.0, "kg", "invalidmodifier(abc)");
        });
        
        // Assert exception details
        Assert.Equal("invalidmodifier", exception.ModifierName);
        Assert.Equal("invalidmodifier(abc)", exception.ModifierString);
        Assert.Contains("invalidmodifier", exception.Message);
        Assert.Contains("invalidmodifier(abc)", exception.Message);
    }

    [Fact]
    public void TemplateEngine_WithInvalidModifier_ShouldThrowInvalidModifierException()
    {
        // Arrange
        var items = new List<TemplateItem>
        {
            new() { Name = "test", NumericValue = 42.0 }
        };
        var engine = new TemplateEngine(items);
        
        // Act & Assert
        var exception = Assert.Throws<InvalidModifierException>(() =>
        {
            engine.ProcessTemplate("{{test.value:invalidmodifier(abc)}}");
        });
        
        // Assert exception details
        Assert.Equal("invalidmodifier", exception.ModifierName);
        Assert.Equal("invalidmodifier(abc)", exception.ModifierString);
    }
}
