using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nkraft.MvvmEssentials.SourceGenerator;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.SourceGenerators;

[TestFixture]
public class NavigationParametersGeneratorTests
{
    private const string Usings = """
        using System;
        using Nkraft.MvvmEssentials.ViewModels;
        using Nkraft.MvvmEssentials.Attributes;

        """;

    // -----------------------------------------------------------------------
    // Harness
    // -----------------------------------------------------------------------

    private static (string Generated, ImmutableArray<Diagnostic> Diagnostics) Run(string source)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Append(MetadataReference.CreateFromFile(
                typeof(Nkraft.MvvmEssentials.ViewModels.PageViewModel).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(Usings + source)],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver
            .Create(new NavigationParametersGenerator().AsSourceGenerator())
            .RunGenerators(compilation);

        var result = driver.GetRunResult().Results.Single();
        var generated = string.Join(
            Environment.NewLine,
            result.GeneratedSources.Select(s => s.SourceText.ToString()));

        return (generated, result.Diagnostics);
    }

    private static string RunExpectingSuccess(string source)
    {
        var (generated, diagnostics) = Run(source);
        Assert.That(
            diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning),
            Is.Empty,
            "Generator reported diagnostics");
        Assert.That(generated, Is.Not.Empty, "No source was generated");
        return generated;
    }

    // -----------------------------------------------------------------------
    // Emission basics
    // -----------------------------------------------------------------------

    [Test]
    public void Generator_WhenPageViewModelHasAttributedProperty_EmitsWithFactory()
    {
        var generated = RunExpectingSuccess("""
            public partial class MyViewModel : PageViewModel
            {
                [NavigationParameter]
                public string Name { get; set; }
            }
            """);

        Assert.That(generated, Does.Contain("public static"));
        Assert.That(generated, Does.Contain("PageDestination With("));
        Assert.That(generated, Does.Contain("string name"));
    }

    [Test]
    public void Generator_DerivesPageNameByReplacingViewModelSuffix()
    {
        var generated = RunExpectingSuccess("""
            public partial class MyViewModel : PageViewModel
            {
                [NavigationParameter]
                public int Id { get; set; }
            }
            """);

        Assert.That(generated, Does.Contain("\"MyPage\""));
    }

    [Test]
    public void Generator_WhenNoAttributedProperty_EmitsNothing()
    {
        var (generated, _) = Run("""
            public partial class MyViewModel : PageViewModel
            {
                public string Name { get; set; }
            }
            """);

        Assert.That(generated, Is.Empty);
    }

    [Test]
    public void Generator_WhenNotDerivedFromPageViewModel_EmitsNothing()
    {
        // NavigableEntryViewModel is deliberately out of scope — mirrors MapPage/RegisterPage
        var (generated, _) = Run("""
            public partial class MyViewModel : NavigableEntryViewModel
            {
                [NavigationParameter]
                public string Name { get; set; }
            }
            """);

        Assert.That(generated, Is.Empty);
    }

    [Test]
    public void Generator_WhenAbstract_EmitsNothing()
    {
        var (generated, _) = Run("""
            public abstract partial class MyViewModel : PageViewModel
            {
                [NavigationParameter]
                public string Name { get; set; }
            }
            """);

        Assert.That(generated, Is.Empty);
    }

    [Test]
    public void Generator_WithMultipleAttributedProperties_EmitsOneFactoryOnly()
    {
        // Regression: one attributed property = one pipeline item, so dedup must hold
        var generated = RunExpectingSuccess("""
            public partial class MyViewModel : PageViewModel
            {
                [NavigationParameter]
                public string Name { get; set; }

                [NavigationParameter]
                public int Age { get; set; }
            }
            """);

        Assert.That(
            generated.Split("With(").Length - 1,
            Is.EqualTo(1),
            "With(...) was emitted more than once");
    }

    // -----------------------------------------------------------------------
    // Required / optional
    // -----------------------------------------------------------------------

    [Test]
    public void Generator_ByDefault_TreatsParameterAsRequired()
    {
        var generated = RunExpectingSuccess("""
            public partial class MyViewModel : PageViewModel
            {
                [NavigationParameter]
                public int Id { get; set; }
            }
            """);

        Assert.That(generated, Does.Contain("int id"));
        Assert.That(generated, Does.Not.Contain("id = default"));
    }

    [Test]
    public void Generator_WhenIsOptionalTrue_EmitsNullableOptionalParameter()
    {
        var generated = RunExpectingSuccess("""
            public partial class MyViewModel : PageViewModel
            {
                [NavigationParameter(IsOptional = true)]
                public int Id { get; set; }
            }
            """);

        Assert.That(generated, Does.Contain("int? id = default"));
        Assert.That(generated, Does.Contain("if (id is not null)"));
    }

    [Test]
    public void Generator_OrdersRequiredParametersBeforeOptionalOnes()
    {
        var generated = RunExpectingSuccess("""
            public partial class MyViewModel : PageViewModel
            {
                [NavigationParameter(IsOptional = true)]
                public int Optional { get; set; }

                [NavigationParameter]
                public int Required { get; set; }
            }
            """);

        Assert.That(
            generated.IndexOf("int required", StringComparison.Ordinal),
            Is.LessThan(generated.IndexOf("int? optional", StringComparison.Ordinal)));
    }

    // -----------------------------------------------------------------------
    // Popups
    // -----------------------------------------------------------------------

    [Test]
    public void Generator_WhenPopupViewModel_EmitsPopupDestinationAndPopupSuffix()
    {
        var generated = RunExpectingSuccess("""
            public partial class MyViewModel : PopupViewModel<bool>
            {
                public MyViewModel(Nkraft.MvvmEssentials.Services.IPopupService s) : base(s) { }

                [NavigationParameter]
                public int Id { get; set; }
            }
            """);

        Assert.That(generated, Does.Contain("PopupDestination<bool>"));
        Assert.That(generated, Does.Contain("\"MyPopup\""));
    }

    // -----------------------------------------------------------------------
    // Hierarchy
    // -----------------------------------------------------------------------

    [Test]
    public void Generator_CollectsAttributedPropertiesFromIntermediateBase()
    {
        var generated = RunExpectingSuccess("""
            public abstract class BaseViewModel : PageViewModel
            {
                [NavigationParameter]
                public int InheritedId { get; set; }
            }

            public partial class MyViewModel : BaseViewModel
            {
                [NavigationParameter]
                public string Name { get; set; }
            }
            """);

        Assert.That(generated, Does.Contain("inheritedId"));
        Assert.That(generated, Does.Contain("name"));
    }

    [Test]
    public void Generator_WhenPropertyRedeclaredInDerived_EmitsItOnce()
    {
        var generated = RunExpectingSuccess("""
            public abstract class BaseViewModel : PageViewModel
            {
                [NavigationParameter]
                public virtual int Id { get; set; }
            }

            public partial class MyViewModel : BaseViewModel
            {
                [NavigationParameter]
                public override int Id { get; set; }
            }
            """);

        Assert.That(generated.Split("\"Id\"").Length - 1, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // Naming
    // -----------------------------------------------------------------------

    [Test]
    public void Generator_WhenPropertyNameIsKeyword_EscapesParameterName()
    {
        var generated = RunExpectingSuccess("""
            public partial class MyViewModel : PageViewModel
            {
                [NavigationParameter]
                public int Class { get; set; }
            }
            """);

        Assert.That(generated, Does.Contain("@class"));
    }

    [Test]
    public void Generator_WithPreferredName_UsesItAsTheGeneratedParameterName()
    {
        // PreferredName controls the C# parameter identifier at the With() call site only.
        var generated = RunExpectingSuccess("""
            public partial class MyViewModel : PageViewModel
            {
                [NavigationParameter(PreferredName = "id")]
                public int ItemId { get; set; }
            }
            """);

        Assert.That(generated, Does.Contain("int id"));
        Assert.That(generated, Does.Not.Contain("int itemId"));
    }

    [Test]
    public void Generator_WithPreferredName_DictionaryKeyStaysThePropertyName()
    {
        // PreferredName has no effect on the wire key — SetNavigationParameter matches on
        // property name, so the dictionary key must stay PropertyName regardless of alias.
        var generated = RunExpectingSuccess("""
            public partial class MyViewModel : PageViewModel
            {
                [NavigationParameter(PreferredName = "id")]
                public int ItemId { get; set; }
            }
            """);

        Assert.That(generated, Does.Contain("int id"));               // parameter name aliased
        Assert.That(generated, Does.Contain("__p.Add(\"ItemId\""));   // wire key unchanged
    }

    [Test]
    public void Generator_WithPreferredNameAndOptional_UsesAliasInGuardAndDictionaryAdd()
    {
        // Regression: GetParameterName is called independently for the signature and the
        // guard — make sure both use the alias, not the raw property name.
        var generated = RunExpectingSuccess("""
            public partial class MyViewModel : PageViewModel
            {
                [NavigationParameter(IsOptional = true, PreferredName = "id")]
                public int ItemId { get; set; }
            }
            """);

        Assert.That(generated, Does.Contain("int? id = default"));
        Assert.That(generated, Does.Contain("if (id is not null) __p.Add(\"ItemId\", id)"));
    }

    // -----------------------------------------------------------------------
    // Diagnostics
    // -----------------------------------------------------------------------

    [Test]
    public void Generator_WhenTypeIsNotPartial_ReportsMve003AndEmitsNothing()
    {
        var (generated, diagnostics) = Run("""
            public class MyViewModel : PageViewModel
            {
                [NavigationParameter]
                public string Name { get; set; }
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Does.Contain("MVE003"));
        Assert.That(generated, Is.Empty);
    }

    [Test]
    public void Generator_WhenPreferredNameIsInvalidIdentifier_ReportsMve004AndEmitsNothing()
    {
        var (generated, diagnostics) = Run("""
            public partial class MyViewModel : PageViewModel
            {
               [NavigationParameter(PreferredName = "item id")]
               public int ItemId { get; set; }
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Does.Contain("MVE004"));
        Assert.That(generated, Is.Empty);
    }

    [Test]
    public void Generator_WhenPreferredNameIsWhitespace_ReportsMve004()
    {
        var (_, diagnostics) = Run("""
            public partial class MyViewModel : PageViewModel
            {
               [NavigationParameter(PreferredName = "   ")]
               public int ItemId { get; set; }
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Does.Contain("MVE004"));
    }

    [Test]
    public void Generator_WhenPreferredNameCollidesWithAnotherProperty_ReportsMve005()
    {
        var (generated, diagnostics) = Run("""
            public partial class MyViewModel : PageViewModel
            {
               [NavigationParameter(PreferredName = "id")]
               public int ItemId { get; set; }

               [NavigationParameter]
               public int Id { get; set; }
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Does.Contain("MVE005"));
        Assert.That(generated, Is.Empty);
    }

    [Test]
    public void Generator_WhenNotPartialAndPreferredNameAlsoInvalid_ReportsOnlyMve003()
    {
        // Gate ordering: IsPartial is checked first and returns early, so a VM that is both
        // non-partial and has a bad PreferredName should report exactly one diagnostic.
        var (generated, diagnostics) = Run("""
            public class MyViewModel : PageViewModel
            {
               [NavigationParameter(PreferredName = "item id")]
               public int ItemId { get; set; }
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id).Single(), Is.EqualTo("MVE003"));
        Assert.That(generated, Is.Empty);
    }
}
