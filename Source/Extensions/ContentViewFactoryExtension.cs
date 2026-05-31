using Nkraft.MvvmEssentials.Services.Navigation;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials;

internal static class ContentViewFactoryExtension
{
    public static void AddContentViewFactory(this IServiceCollection services)
    {
        // View factory lifetime is bound to its host (mostly VM's page)
        services.AddTransient<IContentViewFactory, ContentViewFactory>();
    }
}