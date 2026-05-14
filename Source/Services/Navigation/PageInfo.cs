namespace Nkraft.MvvmEssentials.Services.Navigation;

internal sealed record PageInfo(
	Type PageType,
	Dictionary<string, object>? Parameters = null
);
