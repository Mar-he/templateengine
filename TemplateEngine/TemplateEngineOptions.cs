using System.Globalization;
using System.Text.Json;

namespace TemplateEngine;

/// <summary>
/// Configuration options for the TemplateEngine.
/// </summary>
public class TemplateEngineOptions
{
    /// <summary>
    /// Gets or sets the culture to use for formatting and parsing. Defaults to InvariantCulture.
    /// </summary>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>
    /// Gets or sets an action to configure JSON serialization options.
    /// </summary>
    public Action<JsonSerializerOptions>? ConfigureJsonOptions { get; set; }
}
