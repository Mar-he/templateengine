using System.Diagnostics;

namespace TemplateEngine.Events;

/// <summary>
/// Base class for all template engine events.
/// </summary>
public abstract record TemplateEngineEventArgs
{
    /// <summary>
    /// The timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Optional correlation ID to track related events.
    /// </summary>
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Event arguments for token processing events.
/// </summary>
public sealed record TokenProcessingEventArgs : TemplateEngineEventArgs
{
    /// <summary>
    /// The original token that was found in the template.
    /// </summary>
    public required string Token { get; init; }
    
    /// <summary>
    /// The item name extracted from the token.
    /// </summary>
    public required string ItemName { get; init; }
    
    /// <summary>
    /// The property name extracted from the token.
    /// </summary>
    public required string PropertyName { get; init; }
    
    /// <summary>
    /// The modifiers string if present.
    /// </summary>
    public string? Modifiers { get; init; }
    
    /// <summary>
    /// The resolved value before processing.
    /// </summary>
    public object? RawValue { get; init; }
    
    /// <summary>
    /// The final processed value.
    /// </summary>
    public string? ProcessedValue { get; init; }
    
    /// <summary>
    /// Whether the token was successfully processed.
    /// </summary>
    public bool IsSuccessful { get; init; }
}

/// <summary>
/// Event arguments for modifier processing events.
/// </summary>
public sealed record ModifierProcessingEventArgs : TemplateEngineEventArgs
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
/// Event arguments for error events.
/// </summary>
public sealed record TemplateEngineErrorEventArgs : TemplateEngineEventArgs
{
    /// <summary>
    /// The exception that occurred.
    /// </summary>
    public required Exception Exception { get; init; }
    
    /// <summary>
    /// The context where the error occurred.
    /// </summary>
    public required string Context { get; init; }
    
    /// <summary>
    /// Additional details about the error.
    /// </summary>
    public string? Details { get; init; }
    
    /// <summary>
    /// The severity level of the error.
    /// </summary>
    public ErrorSeverity Severity { get; init; } = ErrorSeverity.Error;
}

/// <summary>
/// Event arguments for template processing start/completion events.
/// </summary>
public sealed record TemplateProcessingEventArgs : TemplateEngineEventArgs
{
    /// <summary>
    /// The original template string.
    /// </summary>
    public required string Template { get; init; }
    
    /// <summary>
    /// The processed result (only available in completion events).
    /// </summary>
    public string? Result { get; init; }
    
    /// <summary>
    /// The number of tokens found in the template.
    /// </summary>
    public int TokenCount { get; init; }
    
    /// <summary>
    /// The processing duration (only available in completion events).
    /// </summary>
    public TimeSpan? Duration { get; init; }
}

/// <summary>
/// Represents the severity level of an error.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Information,
    
    /// <summary>
    /// Warning that doesn't prevent processing.
    /// </summary>
    Warning,
    
    /// <summary>
    /// Error that affects processing but allows continuation.
    /// </summary>
    Error,
    
    /// <summary>
    /// Critical error that prevents further processing.
    /// </summary>
    Critical
}
