using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using TemplateEngine.Modifiers;

namespace TemplateEngine;

/// <summary>
/// A simple template engine that processes templates with token replacement, unit conversion, and value formatting.
/// </summary>
public class SimpleTemplateEngine
{
    private readonly List<TemplateItem> _items;
    private readonly Regex _tokenRegex = new Regex(@"\{\{(\w+)\.(\w+)(?::([^}]+))?\}\}", RegexOptions.Compiled);
    private readonly ModifierProcessor _modifierProcessor;

    /// <summary>
    /// Initializes a new instance of the SimpleTemplateEngine with JSON data.
    /// </summary>
    /// <param name="jsonData">JSON string containing an array of template items.</param>
    /// <param name="configureOptions">Optional action to configure JSON serialization options.</param>
    public SimpleTemplateEngine(string jsonData, Action<JsonSerializerOptions>? configureOptions = null)
    {
        _items = ParseJsonData(jsonData, configureOptions);
        _modifierProcessor = new ModifierProcessor();
    }

    /// <summary>
    /// Initializes a new instance of the SimpleTemplateEngine with a list of template items.
    /// </summary>
    /// <param name="items">The list of template items.</param>
    public SimpleTemplateEngine(List<TemplateItem> items)
    {
        _items = items;
        _modifierProcessor = new ModifierProcessor();
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
    /// Processes a value with the specified modifiers using the modifier processor.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="modifiers">The modifiers to apply (e.g., "convert(mph):round(2)").</param>
    /// <param name="unit">The current unit of the value.</param>
    /// <returns>The processed value as a string.</returns>
    private string ProcessValueWithModifiers(object? value, string modifiers, string? unit)
    {
        // Handle non-numeric values
        if (value is double numericValue)
            return _modifierProcessor.ProcessModifiers(numericValue, unit?.ToLowerInvariant() ?? string.Empty,
                modifiers);
        
        if (value is string || !double.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out numericValue))
        {
            return value?.ToString() ?? string.Empty;
        }

        return _modifierProcessor.ProcessModifiers(numericValue, unit?.ToLowerInvariant() ?? string.Empty, modifiers);
    }

    /// <summary>
    /// Registers a custom modifier with the template engine.
    /// </summary>
    /// <param name="modifier">The custom modifier to register.</param>
    public void RegisterModifier(IValueModifier modifier)
    {
        _modifierProcessor.RegisterModifier(modifier);
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
