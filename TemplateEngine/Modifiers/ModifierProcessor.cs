using System.Globalization;

namespace TemplateEngine.Modifiers;

/// <summary>
/// Event arguments for modifier processing events.
/// </summary>
public sealed record ModifierAppliedEventArgs
{
    /// <summary>
    /// The name of the modifier being applied.
    /// </summary>
    public required string ModifierName { get; init; }
    
    /// <summary>
    /// The parameters passed to the modifier.
    /// </summary>
    public string? Parameters { get; init; }
    
    /// <summary>
    /// The value before the modifier was applied.
    /// </summary>
    public required object InputValue { get; init; }
    
    /// <summary>
    /// The value after the modifier was applied.
    /// </summary>
    public object? OutputValue { get; init; }
    
    /// <summary>
    /// The unit before conversion (if applicable).
    /// </summary>
    public string? InputUnit { get; init; }
    
    /// <summary>
    /// The unit after conversion (if applicable).
    /// </summary>
    public string? OutputUnit { get; init; }
    
    /// <summary>
    /// Whether the modifier was successfully applied.
    /// </summary>
    public bool IsSuccessful { get; init; }
}

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
    /// Event raised when a modifier is applied.
    /// </summary>
    public event EventHandler<ModifierAppliedEventArgs>? ModifierApplied;

    /// <summary>
    /// Initializes a new instance of the ModifierProcessor with the specified culture.
    /// </summary>
    /// <param name="culture">The culture to use for formatting. Defaults to InvariantCulture if null.</param>
    public ModifierProcessor(CultureInfo? culture = null)
    {
        _culture = culture ?? CultureInfo.InvariantCulture;
    }

    /// <summary>
    /// Processes a single modifier for the given value and unit.
    /// </summary>
    /// <param name="value">The initial value to process.</param>
    /// <param name="unit">The initial unit of the value.</param>   
    /// <param name="modifierString">The modifier string (e.g., "round(2)" or "convert(mph)").</param>
    /// <returns>The processed value as a string.</returns>
    /// <exception cref="InvalidModifierException">Thrown when an unknown modifier is encountered.</exception>
    public string ProcessModifier(double value, string unit, string modifierString)
    {
        var context = new ModifierContext(value, unit);
        var trimmedModifier = modifierString.Trim();
        
        if (string.IsNullOrEmpty(trimmedModifier))
        {
            return context.Value.ToString(_culture);
        }

        var inputValue = context.Value;
        var inputUnit = context.Unit;
        
        var modifier = _modifiers.FirstOrDefault(m => m.CanHandle(trimmedModifier));
        
        if (modifier != null)
        {
            try
            {
                modifier.Apply(context, trimmedModifier);
                
                // Extract modifier name and parameters
                var (modifierName, parameters) = ParseModifierName(trimmedModifier);
                
                OnModifierApplied(new ModifierAppliedEventArgs
                {
                    ModifierName = modifierName,
                    Parameters = parameters,
                    InputValue = inputValue,
                    OutputValue = context.Value,
                    InputUnit = inputUnit,
                    OutputUnit = context.Unit,
                    IsSuccessful = true
                });
            }
            catch (Exception)
            {
                var (modifierName, parameters) = ParseModifierName(trimmedModifier);
                
                OnModifierApplied(new ModifierAppliedEventArgs
                {
                    ModifierName = modifierName,
                    Parameters = parameters,
                    InputValue = inputValue,
                    OutputValue = inputValue, // Keep original value on error
                    InputUnit = inputUnit,
                    OutputUnit = inputUnit,
                    IsSuccessful = false
                });
                
                throw; // Re-throw the exception
            }
        }
        else
        {
            // Extract modifier name for better error reporting
            var (modifierName, parameters) = ParseModifierName(trimmedModifier);
            
            // Raise event for failed modifier
            OnModifierApplied(new ModifierAppliedEventArgs
            {
                ModifierName = modifierName,
                Parameters = parameters,
                InputValue = inputValue,
                OutputValue = inputValue,
                InputUnit = inputUnit,
                OutputUnit = inputUnit,
                IsSuccessful = false
            });
            
            // Throw custom exception for unknown modifier
            throw new InvalidModifierException(modifierName, modifierString);
        }

        return context.Value.ToString(_culture);
    }

    /// <summary>
    /// Parses a modifier string to extract the name and parameters.
    /// </summary>
    /// <param name="modifierString">The modifier string (e.g., "round(2)" or "convert(mph)").</param>
    /// <returns>A tuple containing the modifier name and parameters.</returns>
    private static (string ModifierName, string? Parameters) ParseModifierName(string modifierString)
    {
        var openParenIndex = modifierString.IndexOf('(');
        if (openParenIndex == -1)
        {
            return (modifierString, null);
        }

        var modifierName = modifierString[..openParenIndex];
        var closeParenIndex = modifierString.LastIndexOf(')');
        var parameters = closeParenIndex > openParenIndex 
            ? modifierString[(openParenIndex + 1)..closeParenIndex] 
            : null;

        return (modifierName, parameters);
    }

    /// <summary>
    /// Raises the ModifierApplied event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnModifierApplied(ModifierAppliedEventArgs e)
    {
        ModifierApplied?.Invoke(this, e);
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
