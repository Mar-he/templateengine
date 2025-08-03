using TemplateEngine;
using TemplateEngine.Events;

// Erstelle Test-Daten
var items = new List<TemplateItem>
{
    new() { Name = "speed", NumericValue = 100, Unit = "km/h" }
};

// Erstelle Template Engine
var engine = new TemplateEngine(items);

// Event-Handler registrieren
engine.TemplateProcessingStarted += (sender, e) => 
    Console.WriteLine($"Processing started: {e.Template} (Tokens: {e.TokenCount})");

engine.TokenProcessing += (sender, e) => 
    Console.WriteLine($"Token: {e.Token} -> {e.ProcessedValue} (Success: {e.IsSuccessful})");

engine.ModifierProcessing += (sender, e) => 
    Console.WriteLine($"Modifier: {e.ModifierName}({e.Parameters}) -> {e.InputValue} to {e.OutputValue}");

engine.TemplateProcessingCompleted += (sender, e) => 
    Console.WriteLine($"Processing completed: {e.Result} (Duration: {e.Duration})");

engine.ErrorOccurred += (sender, e) => 
    Console.WriteLine($"Error in {e.Context}: {e.Exception.Message}");

// Template verarbeiten
Console.WriteLine("=== Test 1: Simple Template ===");
var result1 = engine.ProcessTemplate("Speed: {{speed.value}} {{speed.unit}}");
Console.WriteLine($"Final result: {result1}");

Console.WriteLine("\n=== Test 2: Template with Modifiers ===");
var result2 = engine.ProcessTemplate("Speed: {{speed.value:convert(mph):round(1)}} mph");
Console.WriteLine($"Final result: {result2}");

Console.WriteLine("\n=== Test 3: Template with Error ===");
try
{
    var result3 = engine.ProcessTemplate("Speed: {{speed.value:invalidmodifier(test)}}");
    Console.WriteLine($"Final result: {result3}");
}
catch (Exception ex)
{
    Console.WriteLine($"Exception caught: {ex.Message}");
}
