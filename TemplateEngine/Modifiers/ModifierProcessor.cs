using System.Globalization;

namespace TemplateEngine.Modifiers;

/// <summary>
/// Processes value modifiers using a chain of responsibility pattern.
/// </summary>
public class ModifierProcessor
{
    private readonly List<IValueModifier> _modifiers = new()
    {
        new RoundModifier(),
        new ConvertModifier()
    };
    
    private readonly CultureInfo _culture;

    /// <summary>
    /// Initializes a new instance of the ModifierProcessor with the specified culture.
    /// </summary>
    /// <param name="culture">The culture to use for formatting. Defaults to InvariantCulture if null.</param>
    public ModifierProcessor(CultureInfo? culture = null)
    {
        _culture = culture ?? CultureInfo.InvariantCulture;
    }

    /// <summary>
    /// Processes all modifiers in sequence for the given value and unit.
    /// </summary>
    /// <param name="value">The initial value to process.</param>
    /// <param name="unit">The initial unit of the value.</param>
    /// <param name="modifierString">The modifier string containing all modifiers (e.g., "convert(mph):round(2)").</param>
    /// <returns>The processed value as a string.</returns>
    public string ProcessModifiers(double value, string unit, string modifierString)
    {
        var context = new ModifierContext(value, unit);
        var modifierParts = modifierString.Split(':');

        foreach (var modifierPart in modifierParts)
        {
            var trimmedModifier = modifierPart.Trim();
            if (string.IsNullOrEmpty(trimmedModifier)) continue;

            var modifier = _modifiers.FirstOrDefault(m => m.CanHandle(trimmedModifier));
            modifier?.Apply(context, trimmedModifier);
        }

        return context.Value.ToString(_culture);
    }

    /// <summary>
    /// Registers a new modifier to the processor.
    /// </summary>
    /// <param name="modifier">The modifier to register.</param>
    public void RegisterModifier(IValueModifier modifier)
    {
        _modifiers.Add(modifier);
    }

    /// <summary>
    /// Gets all registered modifiers.
    /// </summary>
    /// <returns>A read-only collection of registered modifiers.</returns>
    public IReadOnlyList<IValueModifier> GetModifiers() => _modifiers.AsReadOnly();
}
