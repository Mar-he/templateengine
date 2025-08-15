# TemplateEngine

A powerful and flexible .NET template engine that processes templates with variable replacement, conversion, and rounding capabilities.

## Features

- **DTO-Based Templates**: Define templates using structured DTOs with variable definitions
- **Type-Safe Source Properties**: Use strongly-typed enums instead of magic strings
- **Value Conversion**: Convert between different units (e.g., km/h to mph, Celsius to Fahrenheit)
- **Rounding Support**: Apply rounding with configurable precision
- **Event System**: Monitor template processing with comprehensive events
- **Dependency Injection**: Full support for .NET DI container
- **Culture Support**: Localized number formatting
- **Extensible Modifiers**: Add custom value modifiers

## Quick Start

### Basic Usage

```csharp
// Create template items
var items = new List<TemplateItem>
{
    new() { Category = "speed", NumericValue = 100.0, Unit = "km/h" },
    new() { Category = "vehicle", StringValue = "Tesla Model 3" }
};

// Initialize the engine
var engine = new TemplateEngine(items);

// Define template with variables
var templateDto = new TemplateDto
{
    TemplateLiteral = "{{vehicleName}} travels at {{speedValue}} {{speedUnit}}",
    Variables = new Dictionary<string, TemplateVariable>
    {
        ["vehicleName"] = new() { Id = "vehicle", Source = VariableSource.StringValue },
        ["speedValue"] = new() { Id = "speed", Source = VariableSource.NumberValue },
        ["speedUnit"] = new() { Id = "speed", Source = VariableSource.Unit }
    }
};

// Process the template
var result = engine.ProcessTemplate(templateDto);
// Output: "Tesla Model 3 travels at 100 km/h"
```

### With Conversion and Rounding

```csharp
var templateDto = new TemplateDto
{
    TemplateLiteral = "Speed: {{speedValue}} {{speedUnit}}",
    Variables = new Dictionary<string, TemplateVariable>
    {
        ["speedValue"] = new() 
        { 
            Id = "speed", 
            Source = VariableSource.NumberValue,
            Convert = "mph",      // Convert km/h to mph
            Round = "round(1)"    // Round to 1 decimal place
        },
        ["speedUnit"] = new() { Id = "speed", Source = VariableSource.Unit }
    }
};

var result = engine.ProcessTemplate(templateDto);
// Output: "Speed: 62.1 km/h" (100 km/h converted to mph and rounded)
```

## Template Structure

### TemplateDto

The main template structure consists of:

```csharp
public record TemplateDto
{
    public required string TemplateLiteral { get; init; }
    public required Dictionary<string, TemplateVariable> Variables { get; init; }
}
```

- **TemplateLiteral**: The template string with variables in `{{variableName}}` format
- **Variables**: Dictionary mapping variable names to their definitions

### TemplateVariable

Each variable is defined with:

```csharp
public record TemplateVariable
{
    public required string Id { get; init; }           // Item category/ID
    public required VariableSource Source { get; init; } // Type-safe source property
    public string? Round { get; init; }                // Optional rounding (e.g., "round(2)")
    public string? Convert { get; init; }              // Optional conversion (e.g., "mph")
}
```

### VariableSource Enum

Type-safe source property specification:

```csharp
public enum VariableSource
{
    NumberValue,    // Use the numeric_value property
    StringValue,    // Use the string_value property  
    Unit           // Use the unit property
}
```

**Benefits:**
- ✅ Compile-time validation
- ✅ IntelliSense support
- ✅ Refactoring-safe
- ✅ No more magic strings

## Data Structure

Template items follow this structure:

```csharp
public class TemplateItem
{
    public string Category { get; set; }      // Unique identifier
    public double? NumericValue { get; set; } // Numeric data
    public string? StringValue { get; set; }  // Text data
    public string? Unit { get; set; }         // Unit of measurement
}
```

### JSON Format

```json
[
  {
    "category": "speed",
    "numeric_value": 100.0,
    "unit": "km/h"
  },
  {
    "category": "vehicle",
    "string_value": "Tesla Model 3"
  }
]
```

## Conversion and Rounding

### Supported Conversions

**Speed:**
- km/h ↔ mph
- m/s ↔ km/h ↔ mph

**Temperature:**
- Celsius ↔ Fahrenheit
- Celsius ↔ Kelvin

**Length:**
- m ↔ ft
- km ↔ miles

### Rounding Options

```csharp
"round(2)"    // Round to 2 decimal places
"round(0)"    // Round to nearest integer
"floor"       // Round down
"ceil"        // Round up
```

### Processing Order

1. **Conversion first** (if specified)
2. **Rounding second** (if specified)
3. **Formatting** with culture settings

## Events

Monitor template processing with events:

```csharp
engine.TemplateProcessingStarted += (sender, e) => 
{
    Console.WriteLine($"Processing template: {e.Template}");
};

engine.TemplateProcessingCompleted += (sender, e) => 
{
    Console.WriteLine($"Result: {e.Result} (took {e.Duration.TotalMilliseconds}ms)");
};

engine.TokenProcessing += (sender, e) => 
{
    Console.WriteLine($"Processing token {e.Token}: {e.RawValue} → {e.ProcessedValue}");
};
```

## Dependency Injection

### Registration

```csharp
// With JSON data
services.AddTemplateEngine(jsonData);

// With JSON data and options
services.AddTemplateEngine(jsonData, options =>
{
    options.Culture = new CultureInfo("de-DE");
});

// With template items
services.AddTemplateEngine(items);

// With template items and culture
services.AddTemplateEngine(items, new CultureInfo("fr-FR"));
```

### Usage

```csharp
public class MyService
{
    private readonly ITemplateEngine _templateEngine;
    
    public MyService(ITemplateEngine templateEngine)
    {
        _templateEngine = templateEngine;
    }
    
    public string GenerateReport(TemplateDto template)
    {
        return _templateEngine.ProcessTemplate(template);
    }
}
```

## Culture Support

Format numbers according to different cultures:

```csharp
var options = new TemplateEngineOptions
{
    Culture = new CultureInfo("de-DE") // German: uses comma for decimals
};

var engine = new TemplateEngine(jsonData, options);
// Numbers will be formatted as: 1234,56 instead of 1234.56
```

## Custom Modifiers

Extend the engine with custom value modifiers:

```csharp
public class CustomModifier : IValueModifier
{
    public bool CanHandle(string modifierString) => modifierString == "double";
    
    public void Apply(ModifierContext context, string modifierString)
    {
        context.Value = context.Value * 2;
    }
}

// Register the modifier
engine.RegisterModifier(new CustomModifier());
```

## Error Handling

The engine provides detailed error information:

- **ArgumentException**: Invalid template DTO (missing variables)
- **InvalidModifierException**: Unknown or invalid modifiers
- **Conversion errors**: Handled gracefully with fallback to original values

## Migration from String-Based System

If migrating from the old string-based system:

### Before (Old System)
```csharp
Source = "number_value"  // Magic strings - error prone
```

### After (New System)
```csharp
Source = VariableSource.NumberValue  // Type-safe enum
```

Use the extension methods for compatibility:
```csharp
// Convert enum to string
var stringValue = VariableSource.NumberValue.ToStringValue(); // "number_value"

// Parse string to enum
var enumValue = VariableSourceExtensions.FromStringValue("number_value");
```

## Requirements

- .NET 8.0 or later
- C# 12 language features

## License

This project is licensed under the MIT License.
