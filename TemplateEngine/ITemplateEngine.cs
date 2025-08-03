using TemplateEngine.Modifiers;
using TemplateEngine.Events;

namespace TemplateEngine;

/// <summary>
/// Interface for the template engine that processes templates with token replacement.
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// Event raised when template processing starts.
    /// </summary>
    event EventHandler<TemplateProcessingEventArgs>? TemplateProcessingStarted;
    
    /// <summary>
    /// Event raised when template processing completes.
    /// </summary>
    event EventHandler<TemplateProcessingEventArgs>? TemplateProcessingCompleted;
    
    /// <summary>
    /// Event raised when a token is being processed.
    /// </summary>
    event EventHandler<TokenProcessingEventArgs>? TokenProcessing;
    
    /// <summary>
    /// Event raised when a modifier is being applied.
    /// </summary>
    event EventHandler<ModifierProcessingEventArgs>? ModifierProcessing;
    
    /// <summary>
    /// Event raised when an error occurs during processing.
    /// </summary>
    event EventHandler<TemplateEngineErrorEventArgs>? ErrorOccurred;

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
