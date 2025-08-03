using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics;
using TemplateEngine.Modifiers;
using TemplateEngine.Events;

namespace TemplateEngine;

/// <summary>
/// A template engine that processes templates with token replacement, unit conversion, and value formatting.
/// </summary>
public class TemplateEngine : ITemplateEngine
{
    private readonly List<TemplateItem> _items;
    private readonly Regex _tokenRegex = new Regex(@"\{\{(\w+)\.(\w+)(?::([^}]+))?\}\}", RegexOptions.Compiled);
    private readonly ModifierProcessor _modifierProcessor;
    private readonly CultureInfo _culture;
    private string? _currentCorrelationId;

    /// <summary>
    /// Event raised when template processing starts.
    /// </summary>
    public event EventHandler<TemplateProcessingEventArgs>? TemplateProcessingStarted;
    
    /// <summary>
    /// Event raised when template processing completes.
    /// </summary>
    public event EventHandler<TemplateProcessingEventArgs>? TemplateProcessingCompleted;
    
    /// <summary>
    /// Event raised when a token is being processed.
    /// </summary>
    public event EventHandler<TokenProcessingEventArgs>? TokenProcessing;
    
    /// <summary>
    /// Event raised when a modifier is being applied.
    /// </summary>
    public event EventHandler<ModifierProcessingEventArgs>? ModifierProcessing;
    
    /// <summary>
    /// Event raised when an error occurs during processing.
    /// </summary>
    public event EventHandler<TemplateEngineErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// Initializes a new instance of the TemplateEngine with JSON data.
    /// </summary>
    /// <param name="jsonData">JSON string containing an array of template items.</param>
    /// <param name="options">Optional configuration options for the template engine.</param>
    public TemplateEngine(string jsonData, TemplateEngineOptions? options = null)
    {
        options ??= new TemplateEngineOptions();
        _culture = options.Culture;
        _items = ParseJsonData(jsonData, options.ConfigureJsonOptions);
        _modifierProcessor = new ModifierProcessor(_culture);
    }

    /// <summary>
    /// Initializes a new instance of the TemplateEngine with a list of template items.
    /// </summary>
    /// <param name="items">The list of template items.</param>
    /// <param name="culture">Optional culture for formatting. Defaults to InvariantCulture.</param>
    public TemplateEngine(List<TemplateItem> items, CultureInfo? culture = null)
    {
        _items = items;
        _culture = culture ?? CultureInfo.InvariantCulture;
        _modifierProcessor = new ModifierProcessor(_culture);
    }

    /// <summary>
    /// Parses JSON data into a list of template items.
    /// </summary>
    /// <param name="jsonData">The JSON data to parse.</param>
    /// <param name="configureOptions">Optional action to configure JSON serialization options.</param>
    /// <returns>A list of template items.</returns>
    private List<TemplateItem> ParseJsonData(string jsonData, Action<JsonSerializerOptions>? configureOptions = null)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Apply custom configuration if provided
        configureOptions?.Invoke(options);

        return JsonSerializer.Deserialize<List<TemplateItem>>(jsonData, options) ?? new List<TemplateItem>();
    }

    /// <summary>
    /// Processes a template string, replacing tokens with actual values.
    /// </summary>
    /// <param name="template">The template string containing tokens to replace.</param>
    /// <returns>The processed template with tokens replaced by actual values.</returns>
    public string ProcessTemplate(string template)
    {
        var correlationId = Guid.NewGuid().ToString();
        _currentCorrelationId = correlationId; // Set for modifier events
        var stopwatch = Stopwatch.StartNew();
        var tokenMatches = _tokenRegex.Matches(template);
        
        // Raise template processing started event
        OnTemplateProcessingStarted(new TemplateProcessingEventArgs
        {
            Template = template,
            TokenCount = tokenMatches.Count,
            CorrelationId = correlationId
        });

        try
        {
            var result = _tokenRegex.Replace(template, match => ProcessToken(match, correlationId));
            
            stopwatch.Stop();
            
            // Raise template processing completed event
            OnTemplateProcessingCompleted(new TemplateProcessingEventArgs
            {
                Template = template,
                Result = result,
                TokenCount = tokenMatches.Count,
                Duration = stopwatch.Elapsed,
                CorrelationId = correlationId
            });
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            OnErrorOccurred(new TemplateEngineErrorEventArgs
            {
                Exception = ex,
                Context = "Template Processing",
                Details = $"Failed to process template: {template}",
                Severity = ErrorSeverity.Critical,
                CorrelationId = correlationId
            });
            
            throw;
        }
        finally
        {
            _currentCorrelationId = null; // Clear after processing
        }
    }

    /// <summary>
    /// Processes a single token match and raises appropriate events.
    /// </summary>
    /// <param name="match">The regex match for the token.</param>
    /// <param name="correlationId">The correlation ID for tracking related events.</param>
    /// <returns>The processed token value.</returns>
    private string ProcessToken(Match match, string correlationId)
    {
        var token = match.Value;
        var itemName = match.Groups[1].Value;
        var propertyName = match.Groups[2].Value.ToLowerInvariant();
        var modifiers = match.Groups[3].Success ? match.Groups[3].Value : null;

        try
        {
            var item = _items.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                OnTokenProcessing(new TokenProcessingEventArgs
                {
                    Token = token,
                    ItemName = itemName,
                    PropertyName = propertyName,
                    Modifiers = modifiers,
                    RawValue = null,
                    ProcessedValue = token,
                    IsSuccessful = false,
                    CorrelationId = correlationId
                });
                
                return token; // Return original token if item not found
            }

            var rawValue = propertyName switch
            {
                "value" => item.Value,
                "unit" => item.Unit ?? string.Empty,
                "name" => item.Name,
                _ => null
            };

            if (rawValue == null)
            {
                OnTokenProcessing(new TokenProcessingEventArgs
                {
                    Token = token,
                    ItemName = itemName,
                    PropertyName = propertyName,
                    Modifiers = modifiers,
                    RawValue = null,
                    ProcessedValue = token,
                    IsSuccessful = false,
                    CorrelationId = correlationId
                });
                
                return token;
            }

            var processedValue = propertyName == "value" && !string.IsNullOrEmpty(modifiers)
                ? ProcessValueWithModifiers(rawValue, modifiers, item.Unit, correlationId)
                : FormatValue(rawValue);

            OnTokenProcessing(new TokenProcessingEventArgs
            {
                Token = token,
                ItemName = itemName,
                PropertyName = propertyName,
                Modifiers = modifiers,
                RawValue = rawValue,
                ProcessedValue = processedValue,
                IsSuccessful = true,
                CorrelationId = correlationId
            });

            return processedValue;
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new TemplateEngineErrorEventArgs
            {
                Exception = ex,
                Context = "Token Processing",
                Details = $"Failed to process token: {token}",
                Severity = ErrorSeverity.Error,
                CorrelationId = correlationId
            });

            OnTokenProcessing(new TokenProcessingEventArgs
            {
                Token = token,
                ItemName = itemName,
                PropertyName = propertyName,
                Modifiers = modifiers,
                RawValue = null,
                ProcessedValue = token,
                IsSuccessful = false,
                CorrelationId = correlationId
            });

            // Re-throw the exception after logging events
            throw;
        }
    }

    /// <summary>
    /// Processes a value with the specified modifiers using the modifier processor.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="modifiers">The modifiers to apply (e.g., "convert(mph):round(2)").</param>
    /// <param name="unit">The current unit of the value.</param>
    /// <param name="correlationId">The correlation ID for tracking related events.</param>
    /// <returns>The processed value as a string.</returns>
    private string ProcessValueWithModifiers(object? value, string modifiers, string? unit, string correlationId)
    {
        // Handle non-numeric values
        if (value is double numericValue)
        {
            // Set up event forwarding for this specific call
            EventHandler<ModifierAppliedEventArgs> handler = (sender, e) => OnModifierProcessing(new ModifierProcessingEventArgs
            {
                ModifierName = e.ModifierName,
                Parameters = e.Parameters,
                InputValue = e.InputValue,
                OutputValue = e.OutputValue,
                InputUnit = e.InputUnit,
                OutputUnit = e.OutputUnit,
                IsSuccessful = e.IsSuccessful,
                CorrelationId = correlationId
            });

            _modifierProcessor.ModifierApplied += handler;
            try
            {
                return _modifierProcessor.ProcessModifiers(numericValue, unit?.ToLowerInvariant() ?? string.Empty, modifiers);
            }
            finally
            {
                _modifierProcessor.ModifierApplied -= handler;
            }
        }
        
        if (value is string || !double.TryParse(value?.ToString(), NumberStyles.Float, _culture, out numericValue))
        {
            return value?.ToString() ?? string.Empty;
        }

        // Set up event forwarding for this specific call
        EventHandler<ModifierAppliedEventArgs> handler2 = (sender, e) => OnModifierProcessing(new ModifierProcessingEventArgs
        {
            ModifierName = e.ModifierName,
            Parameters = e.Parameters,
            InputValue = e.InputValue,
            OutputValue = e.OutputValue,
            InputUnit = e.InputUnit,
            OutputUnit = e.OutputUnit,
            IsSuccessful = e.IsSuccessful,
            CorrelationId = correlationId
        });

        _modifierProcessor.ModifierApplied += handler2;
        try
        {
            return _modifierProcessor.ProcessModifiers(numericValue, unit?.ToLowerInvariant() ?? string.Empty, modifiers);
        }
        finally
        {
            _modifierProcessor.ModifierApplied -= handler2;
        }
    }

    /// <summary>
    /// Registers a custom modifier with the template engine.
    /// </summary>
    /// <param name="modifier">The custom modifier to register.</param>
    public void RegisterModifier(IValueModifier modifier)
    {
        _modifierProcessor.RegisterModifier(modifier);
    }

    /// <summary>
    /// Formats a value for display, using the configured culture for number formatting.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted value as a string.</returns>
    private string FormatValue(object? value)
    {
        return value switch
        {
            double d => d.ToString(_culture),
            float f => f.ToString(_culture),
            decimal dec => dec.ToString(_culture),
            _ => value?.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Gets a copy of all template items.
    /// </summary>
    /// <returns>A list containing copies of all template items.</returns>
    public List<TemplateItem> GetItems() => _items.ToList();

    /// <summary>
    /// Raises the TemplateProcessingStarted event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnTemplateProcessingStarted(TemplateProcessingEventArgs e)
    {
        TemplateProcessingStarted?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the TemplateProcessingCompleted event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnTemplateProcessingCompleted(TemplateProcessingEventArgs e)
    {
        TemplateProcessingCompleted?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the TokenProcessing event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnTokenProcessing(TokenProcessingEventArgs e)
    {
        TokenProcessing?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the ModifierProcessing event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnModifierProcessing(ModifierProcessingEventArgs e)
    {
        ModifierProcessing?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the ErrorOccurred event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnErrorOccurred(TemplateEngineErrorEventArgs e)
    {
        ErrorOccurred?.Invoke(this, e);
    }
}
