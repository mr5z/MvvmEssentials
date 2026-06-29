namespace Nkraft.MvvmEssentials.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ParameterAttribute : Attribute
{
    public bool IsRequired { get; set; }

    public string? PreferredPropertyName { get; set; }
}
