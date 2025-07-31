namespace TemplateEngine.Modifiers;

/// <summary>
/// Represents the context for modifier processing, containing the current value and unit.
/// </summary>
public class ModifierContext
{
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;

    public ModifierContext(double value, string unit = "")
    {
        Value = value;
        Unit = unit;
    }
}

/// <summary>
/// Base interface for value modifiers that can be applied to template values.
/// </summary>
public interface IValueModifier
{
    /// <summary>
    /// Checks if this modifier can handle the given modifier string.
    /// </summary>
    /// <param name="modifierString">The modifier string to check.</param>
    /// <returns>True if this modifier can handle the string, false otherwise.</returns>
    bool CanHandle(string modifierString);

    /// <summary>
    /// Applies the modification to the context.
    /// </summary>
    /// <param name="context">The context containing the value and unit to modify.</param>
    /// <param name="modifierString">The modifier string containing parameters.</param>
    void Apply(ModifierContext context, string modifierString);
}
