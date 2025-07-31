using Xunit;

namespace TemplateEngine.Tests;

public class TemplateItemTests
{
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
    public void TemplateItem_Value_ReturnsNullWhenBothAreNull()
    {
        // Arrange
        var item = new TemplateItem
        {
            Name = "test",
            StringValue = null,
            NumericValue = null
        };
        
        // Act & Assert
        Assert.Null(item.Value);
    }

    [Fact]
    public void TemplateItem_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var item = new TemplateItem();
        
        // Assert
        Assert.Equal(string.Empty, item.Name);
        Assert.Null(item.NumericValue);
        Assert.Null(item.StringValue);
        Assert.Null(item.Unit);
    }
}
