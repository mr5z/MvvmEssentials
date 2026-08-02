using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Helpers;

[TestFixture]
public class PageHelperTests
{
    [Test]
    public void ToPageName_WithPagePattern_ReplacesViewModelSuffix()
    {
        var result = PageHelper.ToPageName<FakeViewModel>(PagePattern.Page);
        Assert.That(result, Is.EqualTo("FakePage"));
    }

    [Test]
    public void ToPageName_WithPopupPattern_ReplacesViewModelSuffix()
    {
        var result = PageHelper.ToPageName<FakeViewModel>(PagePattern.Popup);
        Assert.That(result, Is.EqualTo("FakePopup"));
    }

    [Test]
    public void ToPageName_WithMultiWordViewModelName_ReplacesOnlySuffix()
    {
        var result = PageHelper.ToPageName<FakeSecondViewModel>(PagePattern.Page);
        Assert.That(result, Is.EqualTo("FakeSecondPage"));
    }

    [Test]
    public void ToPageName_NonGenericOverload_ProducesSameResultAsGeneric()
    {
        var result = PageHelper.ToPageName(typeof(FakeViewModel), PagePattern.Popup);
        Assert.That(result, Is.EqualTo("FakePopup"));
    }

    [Test]
    public void ToViewModelName_ReplacesPageSuffix()
    {
        var result = PageHelper.ToViewModelName(typeof(FakePage));
        Assert.That(result, Is.EqualTo("FakeViewModel"));
    }
}
