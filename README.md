# TemplateEngine

A powerful and flexible template engine for .NET that processes templates with token replacement, unit conversion, value formatting, and comprehensive culture support.

## Features

- **Token-based templating** with `{{item.property}}` syntax
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

### Basic Usage

```csharp
// JSON data with numeric and string values
var jsonData = """
[{
  "name": "speed",
  "numeric_value": 100,
  "unit": "km/h"
}, {
  "name": "type",
  "string_value": "electric vehicle"
}]
""";

var engine = new TemplateEngine(jsonData);
var template = "Vehicle: {{type.value}}, Speed: {{speed.value}} {{speed.unit}}";

Console.WriteLine(result); // Output: Vehicle: electric vehicle, Speed: 100 km/h
// Output: "Vehicle: electric vehicle, Speed: 100 km/h"

### With Value Modifiers

```csharp
var template = "Speed: {{speed.value:convert(mph):round(1)}} mph";
var result = engine.ProcessTemplate(template);
// Output: "Speed: 62.1 mph"
```

### Dependency Injection

The TemplateEngine can be integrated into .NET's dependency injection system:
```
{{item_name.property:modifier1:modifier2}}
```

### Available Properties
- `value` - The numeric or string value
- `unit` - The unit of measurement
- `name` - The item name

### Available Modifiers

#### Round Modifier
```csharp
{{speed.value:round(2)}}  // Rounds to 2 decimal places
{{speed.value:round(0)}}  // Rounds to integer
```

#### Convert Modifier
```csharp
{{speed.value:convert(mph)}}           // km/h to mph
{{consumption.value:convert(mpg)}}     // l/100km to mpg
{{temperature.value:convert(fahrenheit)}} // celsius to fahrenheit
```

#### Chaining Modifiers
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
    };
});
```

### Using Items Directly

```csharp
var items = new List<TemplateItem>
{
    new() { Name = "speed", NumericValue = 120, Unit = "km/h" }
};

services.AddTemplateEngine(items, new CultureInfo("en-US"));
```

### Builder Pattern

```csharp
services.AddTemplateEngine(builder =>
{
    builder.UseJsonData(jsonData)
           .UseCulture(new CultureInfo("fr-FR"))
           .AddModifier<CustomModifier>()
           .ConfigureJsonOptions(options =>
           {
               options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
           });
});
```

### Using in Controllers

```csharp
[ApiController]
public class ReportController : ControllerBase
{
    private readonly ITemplateEngine _templateEngine;

    public ReportController(ITemplateEngine templateEngine)
    {
        _templateEngine = templateEngine;
    }

    [HttpGet]
    public string GenerateReport()
    {
        var template = "Speed: {{vehicle_speed.value:convert(mph):round(1)}} mph";
        return _templateEngine.ProcessTemplate(template);
    }
}
```

## Culture Support

The TemplateEngine supports culture-specific formatting:

```csharp
// German culture (comma as decimal separator)
var options = new TemplateEngineOptions
{
    Culture = new CultureInfo("de-DE")
};
var engine = new TemplateEngine(jsonData, options);

// French culture
services.AddTemplateEngine(jsonData, options =>
{
    options.Culture = new CultureInfo("fr-FR");
});
```

## Custom Modifiers

Create custom modifiers by implementing `IValueModifier`:

```csharp
public class MultiplyModifier : IValueModifier
{
    public bool CanHandle(string modifierString)
    {
        return modifierString.StartsWith("multiply(") && modifierString.EndsWith(")");
    }

    public void Apply(ModifierContext context, string modifierString)
    {
        var parameter = modifierString[9..^1]; // Extract parameter
        if (double.TryParse(parameter, out var multiplier))
        {
            context.Value *= multiplier;
        }
    }
}

// Register the modifier
engine.RegisterModifier(new MultiplyModifier());

// Or via DI
services.AddTemplateEngine(builder =>
{
    builder.UseJsonData(jsonData)
           .AddModifier<MultiplyModifier>();
});
```

## Supported Unit Conversions

### Speed
- `km/h` ↔ `mph`

### Fuel Consumption
- `l/100km` ↔ `mpg`

### Temperature
- `celsius` ↔ `fahrenheit`

## JSON Data Format

The template engine expects JSON data in the following format:

```json
[
  {
    "name": "item_name",
    "numeric_value": 123.45,
    "unit": "kg"
  },
  {
    "name": "another_item",
    "string_value": "text value"
  }
]
```

### Important Notes
- Each item must have a unique `name`
- Items can have either `numeric_value` OR `string_value`, not both
- `unit` is optional and only applicable to numeric values
- Property names are case-insensitive
- Snake_case naming is supported by default

## Configuration Options

### TemplateEngineOptions

```csharp
var options = new TemplateEngineOptions
{
    Culture = new CultureInfo("en-US"),           // Default: InvariantCulture
    ConfigureJsonOptions = jsonOptions =>         // Optional JSON configuration
    {
        jsonOptions.AllowTrailingCommas = true;
        jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }
};
```

### JSON Serialization Options

You can customize JSON deserialization:

```csharp
services.AddTemplateEngine(jsonData, options =>
{
    options.ConfigureJsonOptions = jsonOptions =>
    {
        jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonOptions.AllowTrailingCommas = true;
        jsonOptions.ReadCommentHandling = JsonCommentHandling.Skip;
    };
});
```

## Error Handling

- **Missing items**: Tokens for non-existent items remain unchanged
- **Invalid properties**: Tokens for invalid properties remain unchanged
- **Invalid modifiers**: Unknown modifiers are ignored
- **Invalid conversions**: Conversion failures return the original value
- **String values with numeric modifiers**: Modifiers are ignored for string values

## Examples

### Vehicle Dashboard

```csharp
var vehicleData = """
[{
  "name": "speed",
  "numeric_value": 95.5,
  "unit": "km/h"
}, {
  "name": "fuel_consumption",
  "numeric_value": 7.2,
  "unit": "l/100km"
}, {
  "name": "engine_temp",
  "numeric_value": 87.5,
  "unit": "celsius"
}]
""";

var template = """
Speed: {{speed.value:round(0)}} {{speed.unit}} ({{speed.value:convert(mph):round(1)}} mph)
Fuel: {{fuel_consumption.value:convert(mpg):round(1)}} mpg
Engine: {{engine_temp.value:convert(fahrenheit):round(0)}}°F
""";

var result = engine.ProcessTemplate(template);
// Output:
// Speed: 96 km/h (59.3 mph)
// Fuel: 32.7 mpg
// Engine: 190°F
```

### Multilingual Reports

```csharp
// German locale
var germanOptions = new TemplateEngineOptions
{
    Culture = new CultureInfo("de-DE")
};

var germanEngine = new TemplateEngine(data, germanOptions);
var result = germanEngine.ProcessTemplate("Preis: {{price.value}} EUR");
// Output: "Preis: 1234,56 EUR" (comma as decimal separator)
```

## Testing

The project includes comprehensive tests covering:
- Basic template processing
- Modifier functionality
- Culture support
- Dependency injection scenarios
- Error handling
- Custom modifiers

Run tests with:
```bash
dotnet test
```

## Requirements

- .NET 8.0 or later
- Microsoft.Extensions.DependencyInjection (for DI features)

## License

This project is licensed under the MIT License.
