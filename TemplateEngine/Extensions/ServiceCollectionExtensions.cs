using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Globalization;
using TemplateEngine.Modifiers;

namespace TemplateEngine.Extensions;

/// <summary>
/// Extension methods for configuring TemplateEngine services in dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the TemplateEngine services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="jsonData">JSON string containing template data.</param>
    /// <param name="configure">Optional action to configure template engine options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTemplateEngine(
        this IServiceCollection services,
        string jsonData,
        Action<TemplateEngineOptions>? configure = null)
    {
        var options = new TemplateEngineOptions();
        configure?.Invoke(options);

        services.TryAddSingleton<ITemplateEngine>(provider =>
            new TemplateEngine(jsonData, options));

        return services;
    }

    /// <summary>
    /// Adds the TemplateEngine services to the dependency injection container with template items.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="items">List of template items.</param>
    /// <param name="culture">Optional culture for formatting. Defaults to InvariantCulture.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTemplateEngine(
        this IServiceCollection services,
        List<TemplateItem> items,
        CultureInfo? culture = null)
    {
        services.TryAddSingleton<ITemplateEngine>(provider =>
            new TemplateEngine(items, culture));

        return services;
    }

    /// <summary>
    /// Adds the TemplateEngine services to the dependency injection container with a factory function.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="factory">Factory function to create the template engine instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTemplateEngine(
        this IServiceCollection services,
        Func<IServiceProvider, ITemplateEngine> factory)
    {
        services.TryAddSingleton(factory);
        return services;
    }

    /// <summary>
    /// Adds the TemplateEngine services to the dependency injection container with a configuration builder.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configurationBuilder">Builder function to configure the template engine.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTemplateEngine(
        this IServiceCollection services,
        Action<TemplateEngineBuilder> configurationBuilder)
    {
        var builder = new TemplateEngineBuilder(services);
        configurationBuilder(builder);
        builder.Build();
        return services;
    }
}

/// <summary>
/// Builder class for configuring TemplateEngine in dependency injection.
/// </summary>
public class TemplateEngineBuilder
{
    internal readonly IServiceCollection Services;
    private string? _jsonData;
    private List<TemplateItem>? _items;
    private readonly TemplateEngineOptions _options = new();
    private readonly List<IValueModifier> _customModifiers = new();

    internal TemplateEngineBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Configures the template engine to use JSON data.
    /// </summary>
    /// <param name="jsonData">JSON string containing template data.</param>
    /// <returns>The builder for chaining.</returns>
    public TemplateEngineBuilder UseJsonData(string jsonData)
    {
        _jsonData = jsonData;
        _items = null; // Clear items if previously set
        return this;
    }

    /// <summary>
    /// Configures the template engine to use a list of template items.
    /// </summary>
    /// <param name="items">List of template items.</param>
    /// <returns>The builder for chaining.</returns>
    public TemplateEngineBuilder UseItems(List<TemplateItem> items)
    {
        _items = items;
        _jsonData = null; // Clear JSON data if previously set
        return this;
    }

    /// <summary>
    /// Configures the culture for number formatting.
    /// </summary>
    /// <param name="culture">The culture to use.</param>
    /// <returns>The builder for chaining.</returns>
    public TemplateEngineBuilder UseCulture(CultureInfo culture)
    {
        _options.Culture = culture;
        return this;
    }

    /// <summary>
    /// Configures JSON serialization options.
    /// </summary>
    /// <param name="configure">Action to configure JSON options.</param>
    /// <returns>The builder for chaining.</returns>
    public TemplateEngineBuilder ConfigureJsonOptions(Action<System.Text.Json.JsonSerializerOptions> configure)
    {
        _options.ConfigureJsonOptions = configure;
        return this;
    }

    /// <summary>
    /// Adds a custom modifier to the template engine.
    /// </summary>
    /// <param name="modifier">The custom modifier to add.</param>
    /// <returns>The builder for chaining.</returns>
    public TemplateEngineBuilder AddModifier(IValueModifier modifier)
    {
        _customModifiers.Add(modifier);
        return this;
    }

    /// <summary>
    /// Adds a custom modifier to the template engine using a factory.
    /// </summary>
    /// <typeparam name="T">The type of the modifier.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public TemplateEngineBuilder AddModifier<T>() where T : class, IValueModifier, new()
    {
        _customModifiers.Add(new T());
        return this;
    }

    /// <summary>
    /// Finalizes the configuration and registers the TemplateEngine in the service collection.
    /// </summary>
    public IServiceCollection Build()
    {
        Services.TryAddSingleton<ITemplateEngine>(provider =>
        {
            ITemplateEngine engine;

            if (!string.IsNullOrEmpty(_jsonData))
            {
                engine = new TemplateEngine(_jsonData, _options);
            }
            else if (_items != null)
            {
                engine = new TemplateEngine(_items, _options.Culture);
            }
            else
            {
                throw new InvalidOperationException(
                    "Either JSON data or template items must be provided to create a TemplateEngine.");
            }

            // Register custom modifiers
            foreach (var modifier in _customModifiers)
            {
                engine.RegisterModifier(modifier);
            }

            return engine;
        });

        return Services;
    }
}
