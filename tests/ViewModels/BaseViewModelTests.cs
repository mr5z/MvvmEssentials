using System.ComponentModel;
using Nkraft.MvvmEssentials.ViewModels;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

[TestFixture]
public class BaseViewModelTests
{
    private class ExposedViewModel : BaseViewModel
    {
        public void TriggerPropertyChanged(string propertyName)
            => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        public string ExposedTypeName => TypeName;
        public string ExposedPageName => PageName;
        public string ExposedViewModelName => ViewModelName;
        public string ExposedNormalizedName => NormalizedName;
    }

    private ExposedViewModel _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new ExposedViewModel();

    // -----------------------------------------------------------------------
    // INotifyPropertyChanged
    // -----------------------------------------------------------------------

    [Test]
    public void PropertyChanged_WhenOnPropertyChangedCalled_FiresEventWithCorrectName()
    {
        // Given
        string? received = null;
        ((INotifyPropertyChanged)_sut).PropertyChanged += (_, e) => received = e.PropertyName;

        // When
        _sut.TriggerPropertyChanged("MyProperty");

        // Then
        Assert.That(received, Is.EqualTo("MyProperty"));
    }

    [Test]
    public void PropertyChanged_WhenNoSubscribers_DoesNotThrow()
    {
        // Then
        Assert.DoesNotThrow(() => _sut.TriggerPropertyChanged("Any"));
    }

    [Test]
    public void PropertyChanged_AfterHandlerRemoved_DoesNotFireForRemovedHandler()
    {
        // Given
        int callCount = 0;
        PropertyChangedEventHandler handler = (_, _) => callCount++;
        ((INotifyPropertyChanged)_sut).PropertyChanged += handler;
        ((INotifyPropertyChanged)_sut).PropertyChanged -= handler;

        // When
        _sut.TriggerPropertyChanged("Prop");

        // Then
        Assert.That(callCount, Is.EqualTo(0));
    }

    // -----------------------------------------------------------------------
    // Name helpers
    // -----------------------------------------------------------------------

    [Test]
    public void PageName_ReflectsClassNameWithPageSuffix()
    {
        // ExposedViewModel → "ExposedPage"
        Assert.That(_sut.ExposedPageName, Is.EqualTo("ExposedPage"));
    }

    [Test]
    public void NormalizedName_StripsViewModelSuffix()
    {
        // ExposedViewModel → "Exposed"
        Assert.That(_sut.ExposedNormalizedName, Is.EqualTo("Exposed"));
    }

    [Test]
    public void ViewModelName_ReturnsFullTypeName()
    {
        Assert.That(_sut.ExposedViewModelName, Is.EqualTo("ExposedViewModel"));
    }
}
