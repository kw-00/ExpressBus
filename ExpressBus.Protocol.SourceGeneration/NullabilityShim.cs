#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>Shim for NullWhenAttribute on netstandard2.0.</summary>
	[System.AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	internal sealed class NullWhenAttribute : System.Attribute
	{
		public NullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
		public bool ReturnValue { get; }
	}
}
#endif
