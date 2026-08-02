using System.ComponentModel;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

[TestFixture]
public class FlyoutHostViewModelTests
{
    private TrackableFlyoutMenuViewModel _menu = null!;
    private TrackableFlyoutMenuViewModel _detail = null!;
    private TestFlyoutViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _menu = new TrackableFlyoutMenuViewModel();
        _detail = new TrackableFlyoutMenuViewModel();
        _sut = new TestFlyoutViewModel(_menu, _detail);
    }

    // -----------------------------------------------------------------------
    // IsPresented — PropertyChanged notification
    // -----------------------------------------------------------------------

    [Test]
    public void IsPresented_WhenSetToNewValue_RaisesPropertyChanged()
    {
        // Given
        var raised = new List<string>();
        ((INotifyPropertyChanged)_sut).PropertyChanged += (_, e) => raised.Add(e.PropertyName!);

        // When
        _sut.IsPresented = true;

        // Then
        Assert.That(raised, Does.Contain(nameof(TestFlyoutViewModel.IsPresented)));
    }

    [Test]
    public void IsPresented_WhenSetToSameValue_DoesNotRaisePropertyChanged()
    {
        // Given
        var raised = new List<string>();
        ((INotifyPropertyChanged)_sut).PropertyChanged += (_, e) => raised.Add(e.PropertyName!);

        // When
        _sut.IsPresented = false;

        // Then
        Assert.That(raised, Is.Empty);
    }

    [Test]
    public void IsPresented_DefaultsToFalse()
    {
        Assert.That(_sut.IsPresented, Is.False);
    }
}