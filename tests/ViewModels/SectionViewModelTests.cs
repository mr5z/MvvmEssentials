using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

[TestFixture]
public class SectionViewModelTests
{
    private TrackableSectionViewModel _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new TrackableSectionViewModel();

    private void TriggerDispose() => ((IDisposable)_sut).Dispose();

    // -----------------------------------------------------------------------
    // INotifyPropertyChanged — inherited from BaseViewModel
    // -----------------------------------------------------------------------

    [Test]
    public void PropertyChanged_WhenOnPropertyChangedCalled_FiresEventWithCorrectName()
    {
        // Given
        string? received = null;
        ((INotifyPropertyChanged)_sut).PropertyChanged += (_, e) => received = e.PropertyName;

        // When
        _sut.SetText("hello");

        // Then
        Assert.That(received, Is.EqualTo(nameof(TrackableSectionViewModel.Text)));
    }

    // -----------------------------------------------------------------------
    // OnDispose
    // -----------------------------------------------------------------------

    [Test]
    public void Dispose_DelegatesToOnDispose()
    {
        // When
        TriggerDispose();

        // Then
        Assert.That(_sut.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void Dispose_WhenScopeEnds_IsCalledOnScopedSection()
    {
        // Given
        var services = new ServiceCollection();
        services.AddScoped<TrackableSectionViewModel>();
        using var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var section = scope.ServiceProvider.GetRequiredService<TrackableSectionViewModel>();

        // When
        scope.Dispose();

        // Then
        Assert.That(section.DisposeCount, Is.EqualTo(1));
    }
}
