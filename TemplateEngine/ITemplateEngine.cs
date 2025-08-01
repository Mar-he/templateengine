using TemplateEngine.Modifiers;

namespace TemplateEngine;

/// <summary>
/// Interface for the template engine that processes templates with token replacement.
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// Processes a template string, replacing tokens with actual values.
    /// </summary>
    /// <param name="template">The template string containing tokens to replace.</param>
    /// <returns>The processed template with tokens replaced by actual values.</returns>
    string ProcessTemplate(string template);

    /// <summary>
    /// Registers a custom modifier with the template engine.
    /// </summary>
    /// <param name="modifier">The custom modifier to register.</param>
    void RegisterModifier(IValueModifier modifier);

    /// <summary>
    /// Gets a copy of all template items.
    /// </summary>
    /// <returns>A list containing copies of all template items.</returns>
    List<TemplateItem> GetItems();
}
