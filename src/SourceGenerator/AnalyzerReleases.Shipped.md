; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
## Release 1.0
### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|------
MVE001  | Design   | Warning  | No app startup defined
MVE002  | Design   | Error    | Multiple IAppStartup implementations
MVE003  | Design   | Error    | ViewModel with navigation parameters without partial modifier
MVE004  | Design   | Error    | Invalid PreferredName on [NavigationParameter]
MVE005  | Design   | Error    | Duplicate With(...) parameter name
MVE006  | Design   | Error    | [NavigationParameter] requires PageViewModel

## Release 1.4.0
### Changed Rules
Rule ID | New Category          | New Severity | Old Category | Old Severity | Notes
--------|-----------------------|--------------|--------------|--------------|------
MVE003  | Nkraft.MvvmEssentials | Error        | Design       | Error        | Category changed
MVE004  | Nkraft.MvvmEssentials | Error        | Design       | Error        | Category changed
MVE005  | Nkraft.MvvmEssentials | Error        | Design       | Error        | Category changed
MVE006  | Nkraft.MvvmEssentials | Warning      | Design       | Error        | Category and severity changed

## Release 1.6.1
### New Rules
Rule ID | Category              | Severity | Notes
--------|-----------------------|----------|------
MVE007  | Nkraft.MvvmEssentials | Error    | IAppStartup implementation is not accessible