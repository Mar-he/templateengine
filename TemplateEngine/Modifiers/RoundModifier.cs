using System.Globalization;

namespace TemplateEngine.Modifiers;

/// <summary>
/// Modifier that rounds numeric values to a specified number of decimal places.
/// </summary>
public class RoundModifier : IValueModifier
{
    public bool CanHandle(string modifierString)
    {
        return modifierString.StartsWith("round(", StringComparison.OrdinalIgnoreCase) && 
               modifierString.EndsWith(")");
    }

    public void Apply(ModifierContext context, string modifierString)
    {
        var parameter = ExtractParameter(modifierString);
        if (int.TryParse(parameter, out var decimals))
        {
            context.Value = Math.Round(context.Value, decimals);
        }
    }

    private static string ExtractParameter(string modifierString)
    {
        return modifierString[6..^1]; // Extract between "round(" and ")"
    }
}
