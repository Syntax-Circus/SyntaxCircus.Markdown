using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SyntaxCircus.Markdown;

/// <summary>Dependency injection registration for <see cref="IMarkdownRenderer"/>.</summary>
public static class MarkdownServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="IMarkdownRenderer"/> built from <paramref name="profile"/> and then adjusted by <paramref name="configure"/>.
    /// Uses <c>TryAdd</c>, so a registration made earlier wins. The options are validated here, so a bad configuration fails at startup.
    /// </summary>
    public static IServiceCollection AddSyntaxCircusMarkdown(
        this IServiceCollection services,
        MarkdownProfile profile = MarkdownProfile.Restricted,
        Action<MarkdownOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var renderer = Create(profile, configure);
        services.TryAddSingleton<IMarkdownRenderer>(renderer);
        return services;
    }

    /// <summary>
    /// Registers a keyed singleton <see cref="IMarkdownRenderer"/>, for apps that need more than one configuration (for example Restricted
    /// legal copy and Article blog posts). Resolve with <c>[FromKeyedServices("article")]</c> or <c>GetRequiredKeyedService</c>.
    /// </summary>
    public static IServiceCollection AddKeyedSyntaxCircusMarkdown(
        this IServiceCollection services,
        object serviceKey,
        MarkdownProfile profile = MarkdownProfile.Restricted,
        Action<MarkdownOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(serviceKey);
        var renderer = Create(profile, configure);
        services.TryAddKeyedSingleton<IMarkdownRenderer>(serviceKey, renderer);
        return services;
    }

    private static MarkdownRenderer Create(MarkdownProfile profile, Action<MarkdownOptions>? configure)
    {
        var options = MarkdownOptions.For(profile);
        configure?.Invoke(options);
        return new MarkdownRenderer(options);
    }
}
