namespace TemplateEngine.Modifiers;

/// <summary>
/// Modifier that converts values from one unit to another using the UnitConverter.
/// </summary>
public class ConvertModifier : IValueModifier
{
    public bool CanHandle(string modifierString)
    {
        return modifierString.StartsWith("convert(", StringComparison.OrdinalIgnoreCase) && 
               modifierString.EndsWith(")");
    }

    public void Apply(ModifierContext context, string modifierString)
    {
        var targetUnit = ExtractParameter(modifierString);
        if (!string.IsNullOrEmpty(context.Unit) && !string.IsNullOrEmpty(targetUnit))
        {
            context.Value = UnitConverter.Convert(context.Value, context.Unit, targetUnit);
            context.Unit = targetUnit; // Update unit for potential chaining
        }
    }

    private static string ExtractParameter(string modifierString)
    {
        return modifierString[8..^1]; // Extract between "convert(" and ")"
    }
}
