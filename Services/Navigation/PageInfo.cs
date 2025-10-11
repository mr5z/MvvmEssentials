namespace Nkraft.MvvmEssentials.Services.Navigation;

internal record PageInfo(
	Type PageType,
	Dictionary<string, object>? Parameters = null
);
