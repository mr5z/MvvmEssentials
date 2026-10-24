using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Services.Presentations;

/// <summary>
/// The caller's side of <c>PresentAsync&lt;TViewModel, TResult&gt;</c>, shared by popups and modals: opens a
/// <see cref="Presentation{TResult}"/>, presents through the service, and awaits the result.
/// </summary>
internal static class Presenting
{
    public static async Task<Result<TResult>> PresentForResultAsync<TResult>(
        Func<INavigationParameters, Task<IResult>> present, INavigationParameters parameters, string name)
    {
        // The presenting service opens the presentation and hands it to the pages it creates
        var presentation = new Presentation<TResult>();
        parameters[NavigationHints.Presentation] = presentation;

        var presented = await present(parameters);
        if (presented.IsFailure)
            return Result.Fail<TResult>(ErrorCode.InvalidState, presented.Exception, "Failed to present '{Name}'.", name);

        try
        {
            return Result.Ok(await presentation.Result);
        }
        catch (TaskCanceledException)
        {
            // The page is already gone, so nothing is dismissed here: that would remove whatever is on top now
            const string message = "'{Name}' has been cancelled.";
            return Result.Fail<TResult>(ErrorCode.Cancelled, message, name);
        }
    }
}
