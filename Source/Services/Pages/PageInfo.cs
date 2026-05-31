namespace Nkraft.MvvmEssentials.Services.Pages;

internal sealed record PageInfo(
	Type PageType,
	Dictionary<string, object>? Parameters = null
);
