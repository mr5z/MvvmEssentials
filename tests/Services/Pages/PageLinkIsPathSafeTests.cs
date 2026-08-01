using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Pages;

[TestFixture]
public class PageLinkIsPathSafeTests
{
    private enum SampleEnum
    {
        First
    }

    private INavigationService _navigationService = null!;

    [SetUp]
    public void SetUp()
    {
        _navigationService = Substitute.For<INavigationService>();
    }

    private IPageLink Relative() => _navigationService.Relative();

    // -----------------------------------------------------------------------
    // Path-safe types expressible as attribute literals
    // -----------------------------------------------------------------------

    [TestCase("text")]
    [TestCase(true)]
    [TestCase('a')]
    [TestCase(1)]
    [TestCase(1L)]
    [TestCase((short)1)]
    [TestCase((byte)1)]
    [TestCase(1u)]
    [TestCase(1ul)]
    [TestCase((ushort)1)]
    [TestCase((sbyte)1)]
    [TestCase(1f)]
    [TestCase(1d)]
    [TestCase(SampleEnum.First)]
    public void AppendSegment_WithPathSafeValue_DoesNotThrow(object value)
    {
        var parameters = new NavigationParameters { { "Value", value } };
        Assert.DoesNotThrow(() => Relative().AppendSegment("TestPage", parameters));
    }

    // -----------------------------------------------------------------------
    // Guid and decimal can't be [TestCase] literals — C# only allows bool, byte, char,
    // double, float, int, long, sbyte, short, string, uint, ulong, ushort, enums, Type,
    // and null as attribute arguments.
    // -----------------------------------------------------------------------

    [Test]
    public void AppendSegment_WithGuidParameter_DoesNotThrow()
    {
        var parameters = new NavigationParameters { { "Value", Guid.NewGuid() } };
        Assert.DoesNotThrow(() => Relative().AppendSegment("TestPage", parameters));
    }

    [Test]
    public void AppendSegment_WithDecimalParameter_DoesNotThrow()
    {
        var parameters = new NavigationParameters { { "Value", 1m } };
        Assert.DoesNotThrow(() => Relative().AppendSegment("TestPage", parameters));
    }

    // -----------------------------------------------------------------------
    // Nullable<T> — only reachable via the POCO reflection branch, since a boxed
    // nullable-with-a-value collapses to its underlying type (boxed 5 is typeof(int),
    // never typeof(int?)). A [TestCase]-driven object value can't represent this, so
    // these stay as dedicated tests with a genuinely nullable-typed anonymous property.
    // -----------------------------------------------------------------------

    [Test]
    public void AppendSegment_WithNullableIntParameter_DoesNotThrow()
    {
        int? value = 5;
        Assert.DoesNotThrow(() => Relative().AppendSegment("TestPage", new { Value = value }));
    }

    [Test]
    public void AppendSegment_WithNullableEnumParameter_DoesNotThrow()
    {
        SampleEnum? value = SampleEnum.First;
        Assert.DoesNotThrow(() => Relative().AppendSegment("TestPage", new { Value = value }));
    }

    // -----------------------------------------------------------------------
    // Not path-safe — falls through every comparison to the final `return false`
    // -----------------------------------------------------------------------

    [Test]
    public void AppendSegment_WithReferenceTypeParameter_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Relative().AppendSegment("TestPage", new { SomeValue = new object() }));
        Assert.That(ex!.Message, Does.Contain("'SomeValue'"));
    }
}