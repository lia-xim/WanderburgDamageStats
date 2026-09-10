// The game's interop assembly exports Unity's NullableAttribute wrapper.
// Provide the normal CLR compiler attribute locally instead of binding to that wrapper.
namespace System.Runtime.CompilerServices;
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
internal sealed class NullableAttribute : Attribute
{
    public NullableAttribute(byte value) { }
    public NullableAttribute(byte[] value) { }
}
