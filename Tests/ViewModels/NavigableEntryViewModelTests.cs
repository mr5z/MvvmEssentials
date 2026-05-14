using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

[TestFixture]
public class NavigableEntryViewModelTests
{
    private PropertiedViewModel _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new PropertiedViewModel();

    // -----------------------------------------------------------------------
    // SetNavigationParameter
    // -----------------------------------------------------------------------

    [Test]
    public void SetNavigationParameter_WhenPropertyExistsAndTypeMatches_SetsValue()
    {
        // When
        _sut.SetNavigationParameter("Name", "Alice");

        // Then
        Assert.That(_sut.Name, Is.EqualTo("Alice"));
    }

    [Test]
    public void SetNavigationParameter_WhenIntProperty_SetsValue()
    {
        // When
        _sut.SetNavigationParameter("Age", 30);

        // Then
        Assert.That(_sut.Age, Is.EqualTo(30));
    }

    [Test]
    public void SetNavigationParameter_WhenPropertyDoesNotExist_DoesNotThrow()
    {
        // Then
        Assert.DoesNotThrow(() => _sut.SetNavigationParameter("NonExistent", "value"));
    }

    [Test]
    public void SetNavigationParameter_WhenPropertyIsReadOnly_DoesNotThrow()
    {
        // Then
        Assert.DoesNotThrow(() => _sut.SetNavigationParameter("ReadOnlyProp", "new value"));
        Assert.That(_sut.ReadOnlyProp, Is.EqualTo("fixed"));
    }

    [Test]
    public void SetNavigationParameter_WithNullValue_SetsPropertyToNull()
    {
        // Given
        _sut.Name = "Before";

        // When
        _sut.SetNavigationParameter("Name", null);

        // Then
        Assert.That(_sut.Name, Is.Null);
    }

    [Test]
    public void SetNavigationParameter_WithNullableIntProperty_SetsValue()
    {
        // When
        _sut.SetNavigationParameter("NullableAge", 25);

        // Then
        Assert.That(_sut.NullableAge, Is.EqualTo(25));
    }

    [Test]
    public void SetNavigationParameter_WhenTypeMismatch_DoesNotSetValue()
    {
        // Given
        _sut.Age = 10;

        // When — passing a string into an int property
        _sut.SetNavigationParameter("Age", "not-an-int");

        // Then — value unchanged
        Assert.That(_sut.Age, Is.EqualTo(10));
    }

    // -----------------------------------------------------------------------
    // IParameterSetAware.OnParametersSet
    // -----------------------------------------------------------------------

    [Test]
    public void OnParametersSet_InvokedViaInterface_CallsOverridableMethod()
    {
        // Given
        var trackable = new TrackablePageViewModel();
        var parameters = new NavigationParameters();
        parameters.Add("x", 1);

        // When
        ((IParameterSetAware)trackable).OnParametersSet(parameters);

        // Then
        Assert.That(trackable.LastParameters, Is.SameAs(parameters));
    }
}
