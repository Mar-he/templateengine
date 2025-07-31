using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;

namespace TemplateEngine;

/// <summary>
/// A simple template engine that processes templates with token replacement, unit conversion, and value formatting.
/// </summary>
public class SimpleTemplateEngine
{
    private readonly List<TemplateItem> _items;
    private readonly Regex _tokenRegex = new Regex(@"\{(\w+)\.(\w+)(?::([^}]+))?\}", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the SimpleTemplateEngine with JSON data.
    /// </summary>
    /// <param name="jsonData">JSON string containing an array of template items.</param>
    /// <param name="configureOptions">Optional action to configure JSON serialization options.</param>
    public SimpleTemplateEngine(string jsonData, Action<JsonSerializerOptions>? configureOptions = null)
    {
        _items = ParseJsonData(jsonData, configureOptions);
    }

    /// <summary>
    /// Initializes a new instance of the SimpleTemplateEngine with a list of template items.
    /// </summary>
    /// <param name="items">The list of template items.</param>
    public SimpleTemplateEngine(List<TemplateItem> items)
    {
        _items = items;
    }

    /// <summary>
    /// Parses JSON data into a list of template items.
    /// </summary>
    /// <param name="jsonData">The JSON data to parse.</param>
    /// <param name="configureOptions">Optional action to configure JSON serialization options.</param>
    /// <returns>A list of template items.</returns>
    private List<TemplateItem> ParseJsonData(string jsonData, Action<JsonSerializerOptions>? configureOptions = null)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Apply custom configuration if provided
        configureOptions?.Invoke(options);

        return JsonSerializer.Deserialize<List<TemplateItem>>(jsonData, options) ?? new List<TemplateItem>();
    }

    /// <summary>
    /// Processes a template string, replacing tokens with actual values.
    /// </summary>
    /// <param name="template">The template string containing tokens to replace.</param>
    /// <returns>The processed template with tokens replaced by actual values.</returns>
    public string ProcessTemplate(string template)
    {
        return _tokenRegex.Replace(template, match =>
        {
            var itemName = match.Groups[1].Value;
            var propertyName = match.Groups[2].Value.ToLowerInvariant();
            var modifiers = match.Groups[3].Success ? match.Groups[3].Value : string.Empty;

            var item = _items.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            if (item == null)
                return match.Value; // Return original token if item not found

            var value = propertyName switch
            {
                "value" => item.Value,
                "unit" => item.Unit ?? string.Empty,
                "name" => item.Name,
                _ => null
            };

            if (value == null)
                return match.Value;

            if (propertyName == "value" && !string.IsNullOrEmpty(modifiers))
            {
                return ProcessValueWithModifiers(value, modifiers, item.Unit);
            }

            return FormatValue(value);
        });
    }

    /// <summary>
    /// Processes a value with the specified modifiers (rounding, conversion, etc.).
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="modifiers">The modifiers to apply (e.g., "convert(mph):round(2)").</param>
    /// <param name="unit">The current unit of the value.</param>
    /// <returns>The processed value as a string.</returns>
    private string ProcessValueWithModifiers(object? value, string modifiers, string? unit)
    {
        if (value is not double numericValue)
        {
            if (value is string || !double.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out numericValue))
            {
                return value?.ToString() ?? string.Empty;
            }
        }

        var currentValue = numericValue;
        var currentUnit = unit?.ToLowerInvariant() ?? string.Empty;

        // Parse modifiers (e.g., "convert(mph):round(2)")
        var modifierParts = modifiers.Split(':');
        
        foreach (var modifier in modifierParts)
        {
            var trimmedModifier = modifier.Trim();
            
            if (trimmedModifier.StartsWith("round(") && trimmedModifier.EndsWith(")"))
            {
                var roundParam = trimmedModifier[6..^1]; // Extract parameter between round( and )
                if (int.TryParse(roundParam, out var decimals))
                {
                    currentValue = Math.Round(currentValue, decimals);
                }
            }
            else if (trimmedModifier.StartsWith("convert(") && trimmedModifier.EndsWith(")"))
            {
                var convertParam = trimmedModifier[8..^1]; // Extract parameter between convert( and )
                if (!string.IsNullOrEmpty(currentUnit))
                {
                    currentValue = UnitConverter.Convert(currentValue, currentUnit, convertParam);
                    currentUnit = convertParam; // Update current unit for potential chaining
                }
            }
        }

        return currentValue.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a value for display, ensuring culture-invariant number formatting.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted value as a string.</returns>
    private static string FormatValue(object? value)
    {
        return value switch
        {
            double d => d.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            _ => value?.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Gets a copy of all template items.
    /// </summary>
    /// <returns>A list containing copies of all template items.</returns>
    public List<TemplateItem> GetItems() => _items.ToList();
}
