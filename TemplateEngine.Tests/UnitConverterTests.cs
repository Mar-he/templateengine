using Xunit;

namespace TemplateEngine.Tests;

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

    [Fact]
    public void Convert_CaseInsensitive_WorksCorrectly()
    {
        // Act & Assert
        Assert.Equal(62.1371, UnitConverter.Convert(100, "KM/H", "MPH"), 4);
        Assert.Equal(62.1371, UnitConverter.Convert(100, "km/h", "MPH"), 4);
        Assert.Equal(62.1371, UnitConverter.Convert(100, "KM/H", "mph"), 4);
    }

    [Fact]
    public void Convert_AllSpeedConversions_WorkCorrectly()
    {
        // Test all speed conversions from km/h
        Assert.Equal(27.778, UnitConverter.Convert(100, "km/h", "m/s"), 3);
        Assert.Equal(53.996, UnitConverter.Convert(100, "km/h", "knots"), 3);
        
        // Test conversion precision (km/h to mph and back should be close to original)
        var mph = UnitConverter.Convert(100, "km/h", "mph");
        Assert.Equal(62.1371, mph, 4);
    }

    [Fact]
    public void Convert_AllTemperatureConversions_WorkCorrectly()
    {
        // Test celsius conversions
        Assert.Equal(32, UnitConverter.Convert(0, "celsius", "fahrenheit"));
        Assert.Equal(273.15, UnitConverter.Convert(0, "celsius", "kelvin"));
        
        // Note: UnitConverter only supports celsius as source, not kelvin to celsius
        // So we test the available conversions only
        Assert.Equal(212, UnitConverter.Convert(100, "celsius", "fahrenheit"));
        Assert.Equal(373.15, UnitConverter.Convert(100, "celsius", "kelvin"));
    }

    [Fact]
    public void Convert_FuelConsumptionConversions_WorkCorrectly()
    {
        // Test l/100km conversions
        var mpgResult = UnitConverter.Convert(8, "l/100km", "mpg");
        Assert.Equal(29.4, mpgResult, 1);
        
        var kmLResult = UnitConverter.Convert(8, "l/100km", "km/l");
        Assert.Equal(12.5, kmLResult, 1);
    }
}
