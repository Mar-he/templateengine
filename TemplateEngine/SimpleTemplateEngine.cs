using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics;
using TemplateEngine.Modifiers;
using TemplateEngine.Events;

namespace TemplateEngine;

/// <summary>
/// A template engine that processes templates with variable replacement using TemplateDto structure.
/// </summary>
public class TemplateEngine : ITemplateEngine
{
    private readonly List<TemplateItem> _items;
    private readonly Regex _variableRegex = new Regex(@"\{\{(\w+)\}\}", RegexOptions.Compiled);
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
    /// Processes a template DTO with variables, replacing variable placeholders with actual values.
    /// </summary>
    /// <param name="templateDto">The template DTO containing the template literal and variable definitions.</param>
    /// <returns>The processed template with variables replaced by actual values.</returns>
    public string ProcessTemplate(TemplateDto templateDto)
    {
        ArgumentNullException.ThrowIfNull(templateDto);
        if (string.IsNullOrEmpty(templateDto.TemplateLiteral)) throw new ArgumentException("Template literal cannot be null or empty.", nameof(templateDto));

        var correlationId = Guid.NewGuid().ToString();
        _currentCorrelationId = correlationId;
        var stopwatch = Stopwatch.StartNew();
        var template = templateDto.TemplateLiteral;
        var variableMatches = _variableRegex.Matches(template);
        
        // Raise template processing started event
        OnTemplateProcessingStarted(new TemplateProcessingEventArgs
        {
            Template = template,
            TokenCount = variableMatches.Count,
            CorrelationId = correlationId
        });

        try
        {
            ValidateTemplateDto(templateDto);
            
            var result = _variableRegex.Replace(template, match => ProcessVariable(match, templateDto.Variables, correlationId));
            
            stopwatch.Stop();
            
            // Raise template processing completed event
            OnTemplateProcessingCompleted(new TemplateProcessingEventArgs
            {
                Template = template,
                Result = result,
                TokenCount = variableMatches.Count,
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
                Context = "Template DTO Processing",
                Details = $"Failed to process template DTO: {template}",
                Severity = ErrorSeverity.Critical,
                CorrelationId = correlationId
            });
            
            throw;
        }
        finally
        {
            _currentCorrelationId = null;
        }
    }

    /// <summary>
    /// Validates that all variables in the template literal are defined in the Variables dictionary.
    /// </summary>
    /// <param name="templateDto">The template DTO to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the DTO is invalid.</exception>
    private void ValidateTemplateDto(TemplateDto templateDto)
    {
        var variableMatches = _variableRegex.Matches(templateDto.TemplateLiteral);
        var templateVariables = variableMatches.Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
        
        var undefinedVariables = templateVariables.Where(v => !templateDto.Variables.ContainsKey(v)).ToList();
        
        if (undefinedVariables.Any())
        {
            throw new ArgumentException($"Template DTO is invalid. The following variables are used in the template but not defined in Variables: {string.Join(", ", undefinedVariables)}");
        }
    }

    /// <summary>
    /// Processes a variable match from the template DTO structure.
    /// </summary>
    /// <param name="match">The regex match for the variable.</param>
    /// <param name="variables">The dictionary of variable definitions.</param>
    /// <param name="correlationId">The correlation ID for tracking related events.</param>
    /// <returns>The processed variable value.</returns>
    private string ProcessVariable(Match match, Dictionary<string, TemplateVariable> variables, string correlationId)
    {
        var variableName = match.Groups[1].Value;
        var variableToken = match.Value;

        try
        {
            // Since we validated the DTO, this should always exist
            var variable = variables[variableName];

            // Find the item by ID
            var item = _items.FirstOrDefault(i => i.Category.Equals(variable.Id, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                OnTokenProcessing(new TokenProcessingEventArgs
                {
                    Token = variableToken,
                    ItemName = variable.Id,
                    PropertyName = variable.Source,
                    Modifiers = variable.Round,
                    RawValue = null,
                    ProcessedValue = variableToken,
                    IsSuccessful = false,
                    CorrelationId = correlationId
                });
                
                return variableToken;
            }

            var rawValue = variable.Source.ToLowerInvariant() switch
            {
                "number_value" => (object?)item.NumericValue,
                "string_value" => (object?)item.StringValue,
                "unit" => (object?)(item.Unit ?? string.Empty),
                _ => null
            };

            if (rawValue == null)
            {
                OnTokenProcessing(new TokenProcessingEventArgs
                {
                    Token = variableToken,
                    ItemName = variable.Id,
                    PropertyName = variable.Source,
                    Modifiers = variable.Round,
                    RawValue = null,
                    ProcessedValue = variableToken,
                    IsSuccessful = false,
                    CorrelationId = correlationId
                });
                
                return variableToken;
            }

            var processedValue = FormatValue(rawValue);

            OnTokenProcessing(new TokenProcessingEventArgs
            {
                Token = variableToken,
                ItemName = variable.Id,
                PropertyName = variable.Source,
                Modifiers = variable.Round,
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
                Context = "Variable Processing",
                Details = $"Failed to process variable: {variableToken}",
                Severity = ErrorSeverity.Error,
                CorrelationId = correlationId
            });

            OnTokenProcessing(new TokenProcessingEventArgs
            {
                Token = variableToken,
                ItemName = variableName,
                PropertyName = "variable",
                Modifiers = null,
                RawValue = null,
                ProcessedValue = variableToken,
                IsSuccessful = false,
                CorrelationId = correlationId
            });

            throw;
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