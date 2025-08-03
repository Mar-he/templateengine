using TemplateEngine;
using TemplateEngine.Modifiers;

// Quick test to see if InvalidModifierException is thrown
var items = new List<TemplateItem>
{
    new() { Name = "test", NumericValue = 42 }
};

var engine = new TemplateEngine(items);

try
{
    var result = engine.ProcessTemplate("{{test.value:invalidmodifier(abc)}}");
    Console.WriteLine($"No exception thrown! Result: {result}");
}
catch (InvalidModifierException ex)
{
    Console.WriteLine($"InvalidModifierException caught: {ex.Message}");
    Console.WriteLine($"ModifierName: {ex.ModifierName}");
    Console.WriteLine($"ModifierString: {ex.ModifierString}");
}
catch (Exception ex)
{
    Console.WriteLine($"Other exception caught: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine($"StackTrace: {ex.StackTrace}");
}
