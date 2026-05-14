namespace Nkraft.MvvmEssentials.Services;

public interface IApplicationContext
{
    Page? MainPage { get; set; }
    IReadOnlyList<Window> Windows { get; }
    void Quit();
}

internal sealed class ApplicationContext : IApplicationContext
{
    private static Application Current => Application.Current!;

    public Page? MainPage
    {
#pragma warning disable CS0618 // Type or member is obsolete
        get => Current.MainPage;
        set => Current.MainPage = value;
#pragma warning restore CS0618 // Type or member is obsolete
    }

    public IReadOnlyList<Window> Windows => Current.Windows;

    public void Quit() => Current.Quit();
}