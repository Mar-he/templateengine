# TemplateEngine

A powerful and flexible template engine for .NET that processes templates with token replacement, unit conversion, value formatting, and comprehensive culture support.

## Features

- **Token-based templating** with `{{item.property}}` syntax
- **Advanced variable-based templating** with TemplateDto structure
- **Value modifiers** for rounding and unit conversion
- **Culture-aware formatting** for internationalization
- **Unit conversion** between common measurement units
- **Dependency Injection support** with fluent configuration API
- **Extensible modifier system** for custom value transformations
- **JSON data loading** with configurable serialization options

## Installation

Add the package reference to your project:

```xml
<PackageReference Include="TemplateEngine" Version="1.0.0" />
```

## Quick Start

### Basic Token-Based Templates

```csharp
// JSON data with numeric and string values
var jsonData = """
[{
  "category": "speed",
  "numeric_value": 100,
  "unit": "km/h"
}, {
  "category": "type",
  "string_value": "electric vehicle"
}]
""";

var engine = new TemplateEngine(jsonData);
var template = "Vehicle: {{type.value}}, Speed: {{speed.value}} {{speed.unit}}";
var result = engine.ProcessTemplate(template);
// Output: "Vehicle: electric vehicle, Speed: 100 km/h"
```

### Advanced Variable-Based Templates (New)

The template engine now supports a more structured approach using `TemplateDto`:

```csharp
// Create template items
var items = new List<TemplateItem>
{
    new() { Category = "1.2.3", NumericValue = 25.5, Unit = "km/h" },
    new() { Category = "1.2.4", NumericValue = 100.7, Unit = "mph" }
};

var engine = new TemplateEngine(items);

// Define template with variable structure
var templateDto = new TemplateDto
{
    TemplateLiteral = "{{value1}} bis {{value2}} {{value2Unit}}",
    Variables = new Dictionary<string, TemplateVariable>
    {
        ["value1"] = new() 
        { 
            Id = "1.2.3", 
            Source = "number_value", 
            Round = "round(0)" 
        },
        ["value2"] = new() 
        { 
            Id = "1.2.4", 
            Source = "number_value", 
            Round = "round(1)" 
        },
        ["value2Unit"] = new() 
        { 
            Id = "1.2.4", 
            Source = "unit" 
        }
    }
};

var result = engine.ProcessTemplate(templateDto);
// Output: "26 bis 100.7 mph"
```

## Template Syntax

### Classic Token Format
```
{{item_name.property:modifier1:modifier2}}
```

### Variable Format (TemplateDto)
```
{{variable_name}}
```

## TemplateDto Structure

The new TemplateDto structure provides more control over template processing:

### TemplateDto Properties
- `TemplateLiteral`: The template string containing `{{variable_name}}` placeholders
- `Variables`: Dictionary mapping variable names to their configurations

### TemplateVariable Properties
- `Id`: Identifier for the data source (matches TemplateItem.Category)
- `Source`: Source type for the value:
  - `"number_value"` - Gets the numeric value
  - `"unit"` - Gets the unit string
  - `"name"` - Gets the category name
- `Round`: Optional rounding modifier (e.g., `"round(2)"`, `"round(0)"`)

### Example JSON Structure for TemplateDto
```json
{
    "template": "{{value1}} bis {{value2}} {{value2Unit}}",
    "variables": {
        "value1": {
            "id": "1.2.3",
            "source": "number_value",
            "round": "round(0)"
        },
        "value2": {
            "id": "1.2.4",
            "source": "number_value",
            "round": "round(1)"
        },
        "value2Unit": {
            "id": "1.2.4",
            "source": "unit"
        }
    }
}
```

## Available Properties (Classic Syntax)
- `value` - The numeric or string value
- `unit` - The unit of measurement
- `name` - The item name

## Available Modifiers

### Round Modifier
```csharp
{{speed.value:round(2)}}  // Rounds to 2 decimal places
{{speed.value:round(0)}}  // Rounds to integer

// In TemplateDto
Round = "round(2)"
```

### Convert Modifier
```csharp
{{speed.value:convert(mph)}}           // km/h to mph
{{consumption.value:convert(mpg)}}     // l/100km to mpg
{{temperature.value:convert(fahrenheit)}} // celsius to fahrenheit
```

### Chaining Modifiers (Classic Syntax Only)
```csharp
{{speed.value:convert(mph):round(1)}}  // Convert then round
{{fuel.value:round(2):convert(mpg)}}   // Round then convert
```

## Dependency Injection

### Basic Registration

```csharp
// In Program.cs or Startup.cs
services.AddTemplateEngine(jsonData);

// With options
services.AddTemplateEngine(jsonData, options =>
{
    options.Culture = new CultureInfo("de-DE");
    options.ConfigureJsonOptions = jsonOptions =>
    {
        jsonOptions.AllowTrailingCommas = true;
        jsonOptions.ReadCommentHandling = JsonCommentHandling.Skip;
    };
});
```

### Usage in Controllers/Services

```csharp
public class TemplateController : ControllerBase
{
    private readonly ITemplateEngine _templateEngine;
    
    public TemplateController(ITemplateEngine templateEngine)
    {
        _templateEngine = templateEngine;
    }
    
    [HttpPost("process")]
    public IActionResult ProcessTemplate([FromBody] string template)
    {
        var result = _templateEngine.ProcessTemplate(template);
        return Ok(result);
    }
    
    [HttpPost("process-dto")]
    public IActionResult ProcessTemplateDto([FromBody] TemplateDto templateDto)
    {
        var result = _templateEngine.ProcessTemplate(templateDto);
        return Ok(result);
    }
}
```

## Culture Support

The template engine supports culture-specific formatting:

```csharp
var germanCulture = new CultureInfo("de-DE");
var engine = new TemplateEngine(items, germanCulture);

var template = "Wert: {{value.value}}";
// Output with German formatting: "Wert: 1234,56"
```

## Custom Modifiers

Create custom value modifiers by implementing `IValueModifier`:

```csharp
public class CustomModifier : IValueModifier
{
    public bool CanHandle(string modifierString)
    {
        return modifierString.StartsWith("custom:");
    }

    public void Apply(ModifierContext context, string modifierString)
    {
        // Custom transformation logic
        var parameter = modifierString.Substring(7); // Remove "custom:"
        context.Value = context.Value * double.Parse(parameter);
    }
}

// Register the modifier
engine.RegisterModifier(new CustomModifier());
```

## Events and Monitoring

The template engine provides comprehensive event support for monitoring:

```csharp
engine.TemplateProcessingStarted += (sender, e) => 
{
    Console.WriteLine($"Processing template with {e.TokenCount} tokens");
};

engine.TemplateProcessingCompleted += (sender, e) => 
{
    Console.WriteLine($"Completed in {e.Duration.TotalMilliseconds}ms");
};

engine.TokenProcessing += (sender, e) => 
{
    Console.WriteLine($"Processing token: {e.Token} -> {e.ProcessedValue}");
};

engine.ErrorOccurred += (sender, e) => 
{
    Console.WriteLine($"Error: {e.Exception.Message}");
};
```

## Migration from Classic to TemplateDto

If you're using the classic `{{item.property}}` syntax, you can migrate to the new TemplateDto structure:

### Before (Classic)
```csharp
var template = "Speed: {{speed.value:round(1)}} {{speed.unit}}";
```

### After (TemplateDto)
```csharp
var templateDto = new TemplateDto
{
    TemplateLiteral = "Speed: {{speedValue}} {{speedUnit}}",
    Variables = new Dictionary<string, TemplateVariable>
    {
        ["speedValue"] = new() 
        { 
            Id = "speed", 
            Source = "number_value", 
            Round = "round(1)" 
        },
        ["speedUnit"] = new() 
        { 
            Id = "speed", 
            Source = "unit" 
        }
    }
};
```

## Performance Considerations

- Template compilation is cached using compiled regular expressions
- Event handlers are temporarily attached during processing to avoid memory leaks
- Culture formatting is applied once during initialization

## License

This project is licensed under the MIT License.
