using System;

namespace ExpressBus.Protocol.Sourcegen.SharedDependencies;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class GenerateTypeIdAttribute : Attribute
{
}
