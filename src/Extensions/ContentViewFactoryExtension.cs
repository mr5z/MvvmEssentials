using Nkraft.MvvmEssentials.Services;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials;

internal static class ContentViewFactoryExtension
{
    public static void AddContentViewFactory(this IServiceCollection services)
    {
        services.AddTransient<IContentViewFactory, ContentViewFactory>();
    }
}