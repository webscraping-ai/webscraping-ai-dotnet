#if NETSTANDARD2_0
// Polyfill for the C# 9 `init` accessor on older runtimes. The compiler emits
// references to System.Runtime.CompilerServices.IsExternalInit when it sees
// `init` setters; netstandard2.0 doesn't ship the type, so we declare it here
// as an internal stub. Compiled into the assembly only when the target is
// netstandard2.0.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
