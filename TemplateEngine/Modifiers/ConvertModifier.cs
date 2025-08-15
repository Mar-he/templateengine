namespace TemplateEngine.Modifiers;

/// <summary>
/// Modifier that converts values from one unit to another using the UnitConverter.
/// </summary>
public class ConvertModifier : IValueModifier
{
    public bool CanHandle(string modifierString)
    {
        if (!modifierString.StartsWith("convert(", StringComparison.OrdinalIgnoreCase) || 
            !modifierString.EndsWith(")"))
        {
            return false;
        }

        var targetUnit = ExtractParameter(modifierString);
        return !string.IsNullOrWhiteSpace(targetUnit);
    }

    public void Apply(ModifierContext context, string modifierString)
    {
        var targetUnit = ExtractParameter(modifierString);
        if (!string.IsNullOrEmpty(context.Unit) && !string.IsNullOrEmpty(targetUnit))
        {
            // Validate that the conversion is actually possible
            if (!UnitConverter.CanConvert(context.Unit, targetUnit))
            {
                throw new InvalidModifierException("convert", modifierString, 
                    $"Cannot convert from '{context.Unit}' to '{targetUnit}'. No conversion available.");
            }

            context.Value = UnitConverter.Convert(context.Value, context.Unit, targetUnit);
            context.Unit = targetUnit; // Update unit for potential chaining
        }
    }

    private static string ExtractParameter(string modifierString)
    {
        return modifierString[8..^1]; // Extract between "convert(" and ")"
    }
}
