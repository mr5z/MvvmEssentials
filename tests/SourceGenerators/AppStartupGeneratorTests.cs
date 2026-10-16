using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.SourceGenerator;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.SourceGenerators;

/// <summary>
/// Simulates a solution with a ViewModel library ("Core") referenced by the app ("App"), where the
/// generator runs in both because both reference the package.
/// </summary>
[TestFixture]
public class AppStartupGeneratorTests
{
    private const string Usings = """
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using Nkraft.MvvmEssentials;
        using Nkraft.CrossUtility.Patterns;
        using Nkraft.MvvmEssentials.Services;

        """;

    // The call that marks a project as the composition root.
    private const string Root = """
        public static class MauiProgram
        {
            public static void Build(IServiceCollection services) => services.AddDiscoveredAppStartup();
        }

        """;

    private const string CorePublicStartup = """
        namespace Core
        {
            public class AppStartup : IAppStartup
            {
                public Task<IResult> OnInitializedAsync() => Task.FromResult<IResult>(Result.Ok());
            }
        }
        """;

    private const string CoreInternalStartup = """
        namespace Core
        {
            internal class AppStartup : IAppStartup
            {
                public Task<IResult> OnInitializedAsync() => Task.FromResult<IResult>(Result.Ok());
            }
        }
        """;

    private const string AppStartupInApp = """
        namespace AppNs
        {
            public class AppStartup : IAppStartup
            {
                public Task<IResult> OnInitializedAsync() => Task.FromResult<IResult>(Result.Ok());
            }
        }
        """;

    // -----------------------------------------------------------------------
    // Harness
    // -----------------------------------------------------------------------

    private static readonly MetadataReference[] BaseReferences = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
        .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
        .Append(MetadataReference.CreateFromFile(typeof(IAppStartup).Assembly.Location))
        // Not necessarily loaded into the AppDomain yet when the fixture starts.
        .Append(MetadataReference.CreateFromFile(typeof(ContentPage).Assembly.Location))
        .Append(MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location))
        .Append(MetadataReference.CreateFromFile(typeof(Nkraft.CrossUtility.Patterns.IResult).Assembly.Location))
        .ToArray();

    private static CSharpCompilation Compile(string assemblyName, string source, params MetadataReference[] references)
    {
        return CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(Usings + source)],
            BaseReferences.Concat(references),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static MetadataReference Emit(Compilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.That(result.Success, Is.True, $"{compilation.AssemblyName} failed to compile: "
            + string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static MetadataReference Core(string source) => Emit(Compile("Core", source));

    private static (string Generated, ImmutableArray<Diagnostic> Diagnostics) Run(Compilation compilation)
    {
        var driver = CSharpGeneratorDriver
            .Create(new AppStartupGenerator().AsSourceGenerator())
            .RunGenerators(compilation);

        var result = driver.GetRunResult().Results.Single();
        var generated = string.Join(
            Environment.NewLine,
            result.GeneratedSources.Select(s => s.SourceText.ToString()));

        return (generated, result.Diagnostics);
    }

    private static string[] Ids(ImmutableArray<Diagnostic> diagnostics) => [.. diagnostics.Select(d => d.Id)];

    // -----------------------------------------------------------------------
    // Composition root only
    // -----------------------------------------------------------------------

    [Test]
    public void Generator_WhenProjectDoesNotCallAddDiscoveredAppStartup_GeneratesNothing()
    {
        var (generated, diagnostics) = Run(Compile("Core", CorePublicStartup));

        Assert.That(generated, Is.Empty);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void Generator_WhenNothingDefined_ReportsMve001InCompositionRoot()
    {
        var (_, diagnostics) = Run(Compile("App", Root));

        Assert.That(Ids(diagnostics), Does.Contain("MVE001"));
    }

    // -----------------------------------------------------------------------
    // Startup location
    // -----------------------------------------------------------------------

    [Test]
    public void Generator_WhenStartupInApp_RegistersIt()
    {
        var (generated, diagnostics) = Run(Compile("App", Root + AppStartupInApp, Core("namespace Core { public class Vm {} }")));

        Assert.That(diagnostics, Is.Empty);
        Assert.That(generated, Does.Contain("AddTransient<IAppStartup, global::AppNs.AppStartup>"));
    }

    [Test]
    public void Generator_WhenPublicStartupInReferencedProject_RegistersIt()
    {
        var (generated, diagnostics) = Run(Compile("App", Root, Core(CorePublicStartup)));

        Assert.That(diagnostics, Is.Empty);
        Assert.That(generated, Does.Contain("AddTransient<IAppStartup, global::Core.AppStartup>"));
    }

    [Test]
    public void Generator_WhenInternalStartupInReferencedProject_ReportsMve007()
    {
        var (_, diagnostics) = Run(Compile("App", Root, Core(CoreInternalStartup)));

        Assert.That(Ids(diagnostics), Does.Contain("MVE007"));
    }

    [Test]
    public void Generator_WhenInternalStartupVisibleToApp_RegistersIt()
    {
        var core = Core("[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"App\")]\n" + CoreInternalStartup);

        var (generated, diagnostics) = Run(Compile("App", Root, core));

        Assert.That(diagnostics, Is.Empty);
        Assert.That(generated, Does.Contain("global::Core.AppStartup"));
    }

    [Test]
    public void Generator_WhenStartupInBothAppAndReferencedProject_ReportsMve002()
    {
        var (_, diagnostics) = Run(Compile("App", Root + AppStartupInApp, Core(CorePublicStartup)));

        Assert.That(Ids(diagnostics), Does.Contain("MVE002"));
    }

    // -----------------------------------------------------------------------
    // Default startup from isInitial
    // -----------------------------------------------------------------------

    [Test]
    public void Generator_WhenOnlyInitialPageDefined_GeneratesDefaultStartup()
    {
        var core = Core("namespace Core { public class LandingViewModel : Nkraft.MvvmEssentials.ViewModels.PageViewModel {} }");

        var (generated, diagnostics) = Run(Compile("App", Root + """
            public class LandingPage : Microsoft.Maui.Controls.ContentPage {}

            public static class Registration
            {
                public static void Configure(IPageRegistry registry)
                    => registry.MapPage<LandingPage, Core.LandingViewModel>(isInitial: true);
            }
            """, core));

        Assert.That(diagnostics, Is.Empty);
        Assert.That(generated, Does.Contain("GeneratedAppStartup"));
        Assert.That(generated, Does.Contain("Push<global::Core.LandingViewModel>"));
        // The navigation result is returned so AppStartupWindowHook can rethrow a failure's real cause.
        Assert.That(generated, Does.Contain("Task<global::Nkraft.CrossUtility.Patterns.IResult> IAppStartup.OnInitializedAsync()"));
    }
}