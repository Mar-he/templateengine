namespace TemplateEngine;

/// <summary>
/// Provides unit conversion functionality for various measurement types.
/// </summary>
public static class UnitConverter
{
    private static readonly Dictionary<string, Dictionary<string, Func<double, double>>> _conversions = new()
    {
        ["km/h"] = new Dictionary<string, Func<double, double>>
        {
            ["mph"] = value => value * 0.621371,
            ["m/s"] = value => value / 3.6,
            ["knots"] = value => value * 0.539957
        },
        ["l/100km"] = new Dictionary<string, Func<double, double>>
        {
            ["mpg"] = value => 235.214583 / value, // US gallons
            ["mpg_uk"] = value => 282.481 / value, // UK gallons
            ["km/l"] = value => 100 / value
        },
        ["celsius"] = new Dictionary<string, Func<double, double>>
        {
            ["fahrenheit"] = value => (value * 9 / 5) + 32,
            ["kelvin"] = value => value + 273.15
        },
        ["kg"] = new Dictionary<string, Func<double, double>>
        {
            ["lbs"] = value => value * 2.20462,
            ["oz"] = value => value * 35.274
        },
        ["m"] = new Dictionary<string, Func<double, double>>
        {
            ["ft"] = value => value * 3.28084,
            ["inches"] = value => value * 39.3701,
            ["miles"] = value => value * 0.000621371,
            ["km"] = value => value / 1000
        }
    };

    /// <summary>
    /// Converts a value from one unit to another.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="fromUnit">The source unit.</param>
    /// <param name="toUnit">The target unit.</param>
    /// <returns>The converted value, or the original value if conversion is not supported.</returns>
    public static double Convert(double value, string fromUnit, string toUnit)
    {
        if (_conversions.TryGetValue(fromUnit.ToLowerInvariant(), out var conversions))
        {
            if (conversions.TryGetValue(toUnit.ToLowerInvariant(), out var converter))
            {
                return converter(value);
            }
        }
        
        // If no conversion found, return original value
        return value;
    }

    /// <summary>
    /// Checks if a conversion from one unit to another is supported.
    /// </summary>
    /// <param name="fromUnit">The source unit.</param>
    /// <param name="toUnit">The target unit.</param>
    /// <returns>True if the conversion is supported, false otherwise.</returns>
    public static bool CanConvert(string fromUnit, string toUnit)
    {
        return _conversions.TryGetValue(fromUnit.ToLowerInvariant(), out var conversions) &&
               conversions.ContainsKey(toUnit.ToLowerInvariant());
    }

    /// <summary>
    /// Gets all available conversion targets for a given source unit.
    /// </summary>
    /// <param name="fromUnit">The source unit.</param>
    /// <returns>An enumerable of available target units.</returns>
    public static IEnumerable<string> GetAvailableConversions(string fromUnit)
    {
        if (_conversions.TryGetValue(fromUnit.ToLowerInvariant(), out var conversions))
        {
            return conversions.Keys;
        }
        return Enumerable.Empty<string>();
    }
}
