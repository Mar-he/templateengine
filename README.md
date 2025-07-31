# Simple Template Engine

A flexible and extensible template engine for .NET 8.0 that supports JSON data processing, token replacement, unit conversion, and value formatting with a clean modifier system.

## Features

- **JSON Data Processing**: Parse JSON arrays into template items with automatic property mapping
- **Token Replacement**: Replace template tokens with actual values using `{{name.property}}` syntax
- **Unit Conversion**: Convert between different units (speed, temperature, weight, distance, fuel consumption)
- **Value Formatting**: Round numeric values to specified decimal places
- **Extensible Modifier System**: Add custom modifiers using a clean Chain of Responsibility pattern
- **Type Safety**: Support for both numeric and string values with automatic type handling
- **Culture-Invariant**: Consistent number formatting regardless of system locale

## Quick Start

### Installation

Clone the repository and build the project:

```bash
git clone <repository-url>
cd TemplateEngine
dotnet build
```

### Running Tests

The project includes a comprehensive test suite with 40+ tests:

```bash
dotnet test
```

### Basic Usage

```csharp
using TemplateEngine;

// JSON data with template items
var jsonData = """
[{
  "name": "speed",
  "numeric_value": 100,
  "unit": "km/h"
}, {
  "name": "description", 
  "string_value": "high performance vehicle"
}]
""";

// Create template engine
var engine = new SimpleTemplateEngine(jsonData);

// Process templates
var result = engine.ProcessTemplate("Speed: {{speed.value}} {{speed.unit}}");
// Output: "Speed: 100 km/h"

var description = engine.ProcessTemplate("Type: {{description.value}}");
// Output: "Type: high performance vehicle"
```

## Template Syntax

### Basic Tokens

- `{{name.value}}` - Gets the value (numeric_value or string_value)
- `{{name.unit}}` - Gets the unit of measurement
- `{{name.name}}` - Gets the name of the item

### Modifiers

Modifiers can be chained using the `:` separator:

#### Rounding
```csharp
{{speed.value:round(2)}}        // Round to 2 decimal places
{{speed.value:round(0)}}        // Round to whole number
```

#### Unit Conversion
```csharp
{{speed.value:convert(mph)}}    // Convert km/h to mph
{{temp.value:convert(fahrenheit)}} // Convert celsius to fahrenheit
{{weight.value:convert(lbs)}}   // Convert kg to pounds
```

#### Chained Modifiers
```csharp
{{speed.value:convert(mph):round(1)}}  // Convert then round
{{temp.value:round(0):convert(fahrenheit)}} // Round then convert
```

## Supported Unit Conversions

| Category | From | To |
|----------|------|-----|
| **Speed** | km/h | mph, m/s, knots |
| **Temperature** | celsius | fahrenheit, kelvin |
| **Weight** | kg | lbs, oz |
| **Distance** | m | ft, inches, miles, km |
| **Fuel Consumption** | l/100km | mpg, mpg_uk, km/l |

## Advanced Features

### Custom JSON Options

```csharp
var engine = new SimpleTemplateEngine(jsonData, options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.AllowTrailingCommas = true;
});
```

### Custom Modifiers

Create your own modifiers by implementing `IValueModifier`:

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

// Register the custom modifier
engine.RegisterModifier(new MultiplyModifier());

// Use it in templates
var result = engine.ProcessTemplate("{{value.name:multiply(2):round(1)}}");
```

## JSON Data Format

The engine expects JSON arrays with the following structure:

```json
[{
  "name": "item_name",
  "numeric_value": 123.45,  // Optional: for numeric values
  "string_value": "text",   // Optional: for string values  
  "unit": "kg"             // Optional: unit of measurement
}]
```

**Important**: Only one of `numeric_value` or `string_value` should be present per item. The `value` property will return `string_value` if present, otherwise `numeric_value`.

## Architecture

The template engine follows clean code principles and design patterns:

- **Strategy Pattern**: Each modifier is a separate strategy
- **Chain of Responsibility**: Modifiers are processed in sequence
- **Open/Closed Principle**: Extensible for new modifiers without changing existing code
- **Single Responsibility**: Each class has a clear, focused purpose

### Project Structure

```
TemplateEngine/
├── TemplateEngine/
│   ├── SimpleTemplateEngine.cs     # Main template engine
│   ├── TemplateItem.cs            # Data model for template items
│   ├── UnitConverter.cs           # Unit conversion logic
│   └── Modifiers/
│       ├── IValueModifier.cs      # Modifier interface
│       ├── ModifierProcessor.cs   # Modifier coordination
│       ├── RoundModifier.cs       # Rounding functionality
│       ��── ConvertModifier.cs     # Unit conversion functionality
└── TemplateEngine.Tests/
    ├── SimpleTemplateEngineTests.cs  # Core template engine tests
    ├── TemplateItemTests.cs          # TemplateItem model tests
    ├── ModifierTests.cs              # Modifier system tests
    └── UnitConverterTests.cs         # Unit conversion tests
```

## Examples

### Complex Template Processing

```csharp
var jsonData = """
[{
  "name": "car_speed",
  "numeric_value": 120.5,
  "unit": "km/h"
}, {
  "name": "fuel_consumption",
  "numeric_value": 7.2,
  "unit": "l/100km"
}, {
  "name": "temperature",
  "numeric_value": 22.5,
  "unit": "celsius"
}]
""";

var engine = new SimpleTemplateEngine(jsonData);

var template = """
Vehicle Stats:
- Speed: {{car_speed.value:convert(mph):round(1)}} mph
- Consumption: {{fuel_consumption.value:convert(mpg):round(1)}} mpg  
- Temperature: {{temperature.value:convert(fahrenheit):round(0)}}°F
""";

var result = engine.ProcessTemplate(template);
```

Output:
```
Vehicle Stats:
- Speed: 74.9 mph
- Consumption: 32.7 mpg
- Temperature: 72°F
```

### Error Handling

The engine gracefully handles errors:

- **Missing items**: Unknown tokens remain unchanged in output
- **Invalid conversions**: Returns original value if conversion not supported
- **String values with numeric modifiers**: Ignores modifiers, returns string as-is
- **Malformed JSON**: Returns empty item list

## Testing

Run the comprehensive test suite:

```bash
dotnet test
```

The project includes 34+ tests covering:
- Basic token replacement
- All unit conversions
- Modifier chaining
- Error conditions
- Custom modifiers
- JSON parsing options

## Requirements

- .NET 8.0 or later
- System.Text.Json (included in .NET 8.0)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

This project is open source. See LICENSE file for details.
