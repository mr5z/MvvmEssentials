using Nkraft.MvvmEssentials.Services;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Navigation;

[TestFixture]
public class NavigationParametersTests
{
    private NavigationParameters _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new NavigationParameters();

    // -----------------------------------------------------------------------
    // IsEmpty
    // -----------------------------------------------------------------------

    [Test]
    public void IsEmpty_WhenNoEntriesAdded_ReturnsTrue()
    {
        // Then
        Assert.That(((INavigationParameters)_sut).IsEmpty, Is.True);
    }

    [Test]
    public void IsEmpty_AfterEntryAdded_ReturnsFalse()
    {
        // Given
        _sut.Add("key", "value");

        // Then
        Assert.That(((INavigationParameters)_sut).IsEmpty, Is.False);
    }

    // -----------------------------------------------------------------------
    // TryGetValue
    // -----------------------------------------------------------------------

    [Test]
    public void TryGetValue_WhenKeyExistsAndTypeMatches_ReturnsTrueAndValue()
    {
        // Given
        _sut.Add("userId", 42);

        // When
        var found = _sut.TryGetValue<int>("userId", out var value);

        // Then
        Assert.That(found, Is.True);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void TryGetValue_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // When
        var found = _sut.TryGetValue<string>("missing", out var value);

        // Then
        Assert.That(found, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void TryGetValue_WhenTypeMismatch_ReturnsFalse()
    {
        // Given — stored as int, requested as string
        _sut.Add("count", 99);

        // When
        var found = _sut.TryGetValue<string>("count", out _);

        // Then
        Assert.That(found, Is.False);
    }

    [Test]
    public void TryGetValue_WhenValueIsNull_ReturnsFalse()
    {
        // Given — null cannot satisfy [NotNullWhen(true)]
        _sut.Add("key", null);

        // When
        var found = _sut.TryGetValue<string>("key", out _);

        // Then
        Assert.That(found, Is.False);
    }

    [Test]
    public void TryGetValue_WithReferenceType_ReturnsCorrectInstance()
    {
        // Given
        var obj = new object();
        _sut.Add("ref", obj);

        // When
        var found = _sut.TryGetValue<object>("ref", out var value);

        // Then
        Assert.That(found, Is.True);
        Assert.That(value, Is.SameAs(obj));
    }

    // -----------------------------------------------------------------------
    // ContainsKey
    // -----------------------------------------------------------------------

    [Test]
    public void ContainsKey_WhenKeyWasAdded_ReturnsTrue()
    {
        // Given
        _sut.Add("token", "abc123");

        // Then
        Assert.That(_sut.ContainsKey("token"), Is.True);
    }

    [Test]
    public void ContainsKey_WhenKeyWasNotAdded_ReturnsFalse()
    {
        // Then
        Assert.That(_sut.ContainsKey("absent"), Is.False);
    }

    // -----------------------------------------------------------------------
    // Add
    // -----------------------------------------------------------------------

    [Test]
    public void Add_WithNullValue_StoresEntryAndContainsKeyReturnsTrue()
    {
        // When
        _sut.Add("nullable", null);

        // Then
        Assert.That(_sut.ContainsKey("nullable"), Is.True);
    }

    [Test]
    public void Add_DuplicateKey_ThrowsArgumentException()
    {
        // Given
        _sut.Add("key", "first");

        // Then
        Assert.Throws<ArgumentException>(() => _sut.Add("key", "second"));
    }

    // -----------------------------------------------------------------------
    // Enumeration
    // -----------------------------------------------------------------------

    [Test]
    public void GetEnumerator_ReturnsAllAddedKeyValuePairs()
    {
        // Given
        _sut.Add("a", 1);
        _sut.Add("b", "two");

        // When
        var pairs = _sut.ToList();

        // Then
        Assert.That(pairs, Has.Count.EqualTo(2));
        Assert.That(pairs.Select(p => p.Key), Is.EquivalentTo(new[] { "a", "b" }));
    }

    [Test]
    public void GetEnumerator_WhenEmpty_ReturnsEmptySequence()
    {
        // Then
        Assert.That(_sut.ToList(), Is.Empty);
    }
}
