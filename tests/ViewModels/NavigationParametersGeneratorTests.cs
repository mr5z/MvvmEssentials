using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nkraft.MvvmEssentials.SourceGenerator;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

[TestFixture]
public class NavigationParametersGeneratorTests
{
    [Test]
    public void Generator_ShouldGenerateRecord_WhenClassHasParameterAttribute()
    {
        var source = """
            using Nkraft.MvvmEssentials.ViewModels;
            using Nkraft.MvvmEssentials.Attributes;

            public class MyViewModel : PageViewModel
            {
                [NavigationParameter]
                public string Name { get; set; }
            }
""";
        
        var dotNetAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));
        
        var mvvmEssentialsAssembly = typeof(Nkraft.MvvmEssentials.ViewModels.PageViewModel).Assembly.Location;

        // 1. Create an EMPTY compilation baseline first
        var baselineCompilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: [
                ..dotNetAssemblies, 
                MetadataReference.CreateFromFile(mvvmEssentialsAssembly) // Ensure this is explicitly linked!
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var generator = new NavigationParametersGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()]);

        // 2. Run the driver on the empty baseline to initialize its internal cache
        driver = driver.RunGenerators(baselineCompilation);

        // 3. NOW add your source file to the compilation (creating a delta change)
        var updatedCompilation = baselineCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));

        // 4. Run the driver again. Roslyn sees the new tree and forces the predicate to run!
        driver = driver.RunGeneratorsAndUpdateCompilation(updatedCompilation, out var outputCompilation, out var diagnostics);

        // Verify results
        var runResult = driver.GetRunResult();
        Assert.That(runResult.GeneratedTrees.Length, Is.GreaterThan(0), "No source files were generated!");
    }
}