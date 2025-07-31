namespace TemplateEngine;

/// <summary>
/// Represents a template item with a name, value (either string or numeric), and optional unit.
/// </summary>
public class TemplateItem
{
    /// <summary>
    /// The name of the template item.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The numeric value of the item, if applicable.
    /// </summary>
    public double? NumericValue { get; set; }
    
    /// <summary>
    /// The string value of the item, if applicable.
    /// </summary>
    public string? StringValue { get; set; }
    
    /// <summary>
    /// The unit of measurement for the value, if applicable.
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Gets the value, prioritizing string value over numeric value.
    /// </summary>
    public object? Value => StringValue ?? (object?)NumericValue;
}
