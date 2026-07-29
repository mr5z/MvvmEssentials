namespace Nkraft.MvvmEssentials.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class NavigationParameterAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public bool IsOptional { get; set; }
    
    /// <summary>
    /// Overrides the generated <c>With(...)</c> parameter name for this property. Has no effect
    /// on the navigation-parameter dictionary key, which is always the property name — this is
    /// purely a call-site ergonomics option (e.g. exposing <c>ItemId</c> as <c>id</c>).
    /// </summary>
    public string? PreferredName { get; set; }
}
