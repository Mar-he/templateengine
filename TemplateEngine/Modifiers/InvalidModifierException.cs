namespace TemplateEngine.Modifiers;

/// <summary>
/// Exception thrown when an invalid or unknown modifier is encountered during template processing.
/// </summary>
public class InvalidModifierException : Exception
{
    /// <summary>
    /// Gets the name of the invalid modifier that caused the exception.
    /// </summary>
    public string ModifierName { get; }

    /// <summary>
    /// Gets the full modifier string that was being processed.
    /// </summary>
    public string ModifierString { get; }

    /// <summary>
    /// Initializes a new instance of the InvalidModifierException class.
    /// </summary>
    /// <param name="modifierName">The name of the invalid modifier.</param>
    /// <param name="modifierString">The full modifier string that was being processed.</param>
    public InvalidModifierException(string modifierName, string modifierString)
        : base($"Invalid modifier '{modifierName}' in modifier string '{modifierString}'. This modifier is not recognized or supported.")
    {
        ModifierName = modifierName;
        ModifierString = modifierString;
    }

    /// <summary>
    /// Initializes a new instance of the InvalidModifierException class with a custom message.
    /// </summary>
    /// <param name="modifierName">The name of the invalid modifier.</param>
    /// <param name="modifierString">The full modifier string that was being processed.</param>
    /// <param name="message">A custom error message.</param>
    public InvalidModifierException(string modifierName, string modifierString, string message)
        : base(message)
    {
        ModifierName = modifierName;
        ModifierString = modifierString;
    }

    /// <summary>
    /// Initializes a new instance of the InvalidModifierException class with a custom message and inner exception.
    /// </summary>
    /// <param name="modifierName">The name of the invalid modifier.</param>
    /// <param name="modifierString">The full modifier string that was being processed.</param>
    /// <param name="message">A custom error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidModifierException(string modifierName, string modifierString, string message, Exception innerException)
        : base(message, innerException)
    {
        ModifierName = modifierName;
        ModifierString = modifierString;
    }
}
