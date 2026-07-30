using System;

namespace Nkraft.MvvmEssentials.SourceGenerator;

// arrays lack structural equality, which silently defeats incremental caching.
internal readonly struct EquatableArray<T>(T[] array) : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    private readonly T[]? _array = array;

    public T[] Array => _array ?? [];

    public bool Equals(EquatableArray<T> other)
    {
        var mine = Array;
        var theirs = other.Array;
        if (mine.Length != theirs.Length)
            return false;
        for (var i = 0; i < mine.Length; i++)
            if (mine[i].Equals(theirs[i]) == false)
                return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> o && Equals(o);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var item in Array)
                hash = hash * 31 + item.GetHashCode();
            return hash;
        }
    }
}
