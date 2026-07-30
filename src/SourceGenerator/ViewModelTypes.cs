namespace Nkraft.MvvmEssentials.SourceGenerator;

internal sealed record ViewModelParameter(
    string PropertyName,
    string Type,
    bool IsOptional,
    string? PreferredName);

internal sealed record ViewModelTarget(
    string Namespace,
    string ClassName,
    string TypeKeyword,
    string Suffix,
    string? ResultType,
    string Accessibility,
    bool IsPartial,
    LocationInfo? Location,
    EquatableArray<ViewModelParameter> Parameters);