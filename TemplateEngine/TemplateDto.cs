using System.Text.RegularExpressions;

namespace TemplateEngine;

/// <summary>
/// Represents the source property type for template variables.
/// </summary>
public enum VariableSource
{
    /// <summary>
    /// Use the numeric value property.
    /// </summary>
    NumberValue,
    
    /// <summary>
    /// Use the string value property.
    /// </summary>
    StringValue,
    
    /// <summary>
    /// Use the unit property.
    /// </summary>
    Unit
}

/// <summary>
/// Extensions for VariableSource enum to provide string conversion.
/// </summary>
public static class VariableSourceExtensions
{
    /// <summary>
    /// Converts VariableSource enum to its string representation.
    /// </summary>
    /// <param name="source">The source enum value.</param>
    /// <returns>The string representation.</returns>
    public static string ToStringValue(this VariableSource source) => source switch
    {
        VariableSource.NumberValue => "number_value",
        VariableSource.StringValue => "string_value", 
        VariableSource.Unit => "unit",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Invalid VariableSource value")
    };
    
    /// <summary>
    /// Parses a string value to VariableSource enum.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The corresponding VariableSource enum value.</returns>
    /// <exception cref="ArgumentException">Thrown when the string value is invalid.</exception>
    public static VariableSource FromStringValue(string value) => value?.ToLowerInvariant() switch
    {
        "number_value" => VariableSource.NumberValue,
        "string_value" => VariableSource.StringValue,
        "unit" => VariableSource.Unit,
        _ => throw new ArgumentException($"Invalid source value: {value}. Valid values are: number_value, string_value, unit", nameof(value))
    };
}

/// <summary>
/// Represents a template DTO with a template literal and variable definitions.
/// </summary>
public record TemplateDto
{
    private static readonly Regex _variableRegex = new Regex(@"\{\{(\w+)\}\}", RegexOptions.Compiled);
    
    public required string TemplateLiteral { get; init; }
    public required Dictionary<string, TemplateVariable> Variables { get; init; }

    private TemplateDto()
    {
        
    }
    
    public static TemplateDto Create(string literal, Dictionary<string, TemplateVariable> variables)
    {
        if (string.IsNullOrWhiteSpace(literal))
        {
            throw new ArgumentException("Template literal cannot be null or empty.", nameof(literal));
        }

        if (variables == null)
        {
            throw new ArgumentException("Variables dictionary cannot be null.", nameof(variables));
        }

        var dto = new TemplateDto
        {
            TemplateLiteral = literal,
            Variables = variables
        };

        dto.Validate(literal, variables);
        
        return dto;
    }
    
    private void Validate(string literal, Dictionary<string, TemplateVariable> variables)
    {
        var variableMatches = _variableRegex.Matches(TemplateLiteral);
        //these are all keys that are used in the template
        var templateVariables = variableMatches.Select(m => m.Groups[1].Value).Distinct().ToList();
        
        // these are all keys that are present in the template, but not in the dicionary.
        var undefinedVariables = templateVariables.Where(v => !variables.ContainsKey(v)).ToList();
        
        if (undefinedVariables.Count != 0)
        {
            throw new ArgumentException($"Template DTO is invalid. The following variables are used in the template but not defined in Variables: {string.Join(", ", undefinedVariables)}");
        }
    }
}

/// <summary>
/// Represents a variable definition within a template.
/// </summary>
public record TemplateVariable
{
    public required string Id { get; init; }
    public required VariableSource Source { get; init; }
    public string? Round { get; init; }
    public string? Convert { get; init; }
}
