namespace Nkraft.MvvmEssentials.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class NavigationParameterAttribute : Attribute
{
    public bool IsRequired { get; set; }

    public string? PreferredPropertyName { get; set; }
}
