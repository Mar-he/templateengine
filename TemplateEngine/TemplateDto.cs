using System.Text.Json.Serialization;

namespace TemplateEngine;

/// <summary>
/// Represents a variable configuration in a template.
/// </summary>
public record TemplateVariable
{
    /// <summary>
    /// The identifier for the data source.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// The source type (e.g., "number_value", "unit").
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// Optional rounding modifier (e.g., "floor", "round(1)").
    /// </summary>
    [JsonPropertyName("round")]
    public string? Round { get; init; }
}

/// <summary>
/// Represents a template with its literal string and variable definitions.
/// </summary>
public record TemplateDto
{
    /// <summary>
    /// The template literal string containing variable placeholders.
    /// </summary>
    [JsonPropertyName("template")]
    public required string TemplateLiteral { get; init; }

    /// <summary>
    /// Dictionary of variable names to their configurations.
    /// </summary>
    [JsonPropertyName("variables")]
    public required Dictionary<string, TemplateVariable> Variables { get; init; }
}
