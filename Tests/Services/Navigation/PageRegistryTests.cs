using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Navigation;

[TestFixture]
public class PageRegistryTests
{
    private ServiceCollection _services = null!;
    private IPageRegistry _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();
        _sut = new PageRegistry(_services);
    }

    // -----------------------------------------------------------------------
    // MapPage — happy paths
    // -----------------------------------------------------------------------

    [Test]
    public void MapPage_WhenPageNotYetRegistered_ReturnsRegistryForChaining()
    {
        // When
        var returned = _sut.MapPage<FakePage, FakeViewModel>();

        // Then
        Assert.That(returned, Is.SameAs(_sut));
    }

    [Test]
    public void MapPage_WhenPageRegistered_ViewModelIsResolvable()
    {
        // Given
        _sut.MapPage<FakePage, FakeViewModel>();

        // When
        var resolved = _sut.ResolveViewModelType(typeof(FakePage));

        // Then
        Assert.That(resolved, Is.EqualTo(typeof(FakeViewModel)));
    }

    [Test]
    public void MapPage_RegistersViewModelAsScopedInDiContainer()
    {
        // When
        _sut.MapPage<FakePage, FakeViewModel>();

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(FakeViewModel));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
    }

    [Test]
    public void MapPage_MultipleDifferentPages_AllResolvableIndependently()
    {
        // Given
        _sut.MapPage<FakePage, FakeViewModel>()
            .MapPage<FakeSecondPage, FakeSecondViewModel>();

        // Then
        Assert.That(_sut.ResolveViewModelType(typeof(FakePage)), Is.EqualTo(typeof(FakeViewModel)));
        Assert.That(_sut.ResolveViewModelType(typeof(FakeSecondPage)), Is.EqualTo(typeof(FakeSecondViewModel)));
    }

    // -----------------------------------------------------------------------
    // MapPage — guard: duplicate page
    // -----------------------------------------------------------------------

    [Test]
    public void MapPage_WhenSamePageRegisteredTwice_ThrowsInvalidOperationException()
    {
        // Given
        _sut.MapPage<FakePage, FakeViewModel>();

        // Then
        Assert.Throws<InvalidOperationException>(() =>
            _sut.MapPage<FakePage, FakeSecondViewModel>());
    }

    // -----------------------------------------------------------------------
    // isInitial flag
    // -----------------------------------------------------------------------

    [Test]
    public void MapPage_WithIsInitialTrue_SetsInitialViewModelType()
    {
        // When
        _sut.MapPage<FakePage, FakeViewModel>(isInitial: true);

        // Then
        Assert.That(_sut.InitialViewModelType, Is.EqualTo(typeof(FakeViewModel)));
    }

    [Test]
    public void MapPage_WithIsInitialFalse_LeavesInitialViewModelTypeNull()
    {
        // When
        _sut.MapPage<FakePage, FakeViewModel>(isInitial: false);

        // Then
        Assert.That(_sut.InitialViewModelType, Is.Null);
    }

    [Test]
    public void MapPage_WhenSecondPageMarkedIsInitial_ThrowsInvalidOperationException()
    {
        // Given
        _sut.MapPage<FakePage, FakeViewModel>(isInitial: true);

        // Then
        Assert.Throws<InvalidOperationException>(() =>
            _sut.MapPage<FakeSecondPage, FakeSecondViewModel>(isInitial: true));
    }

    // -----------------------------------------------------------------------
    // ResolveViewModelType — unknown page
    // -----------------------------------------------------------------------

    [Test]
    public void ResolveViewModelType_WhenPageNeverRegistered_ReturnsNull()
    {
        // Then
        Assert.That(_sut.ResolveViewModelType(typeof(FakePage)), Is.Null);
    }

    // -----------------------------------------------------------------------
    // RegisterPage
    // -----------------------------------------------------------------------

    [Test]
    public void RegisterPage_RegistersViewModelAsScopedWithoutMapping()
    {
        // When
        _sut.RegisterPage<FakeViewModel>();

        // Then — present in DI
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(FakeViewModel));
        Assert.That(descriptor, Is.Not.Null);

        // But not mapped to any page
        Assert.That(_sut.ResolveViewModelType(typeof(FakePage)), Is.Null);
    }
}
