using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace YaeBlog.Typst;

[NativeMarshalling(typeof(RustStringMarshaller))]
public struct RustString
{
    public string? Value;
}

/// <summary>
/// Customize string marshaller used to handle `CString` returned value in Rust side.
/// </summary>
[CustomMarshaller(typeof(RustString), MarshalMode.ManagedToUnmanagedOut, typeof(RustStringMarshaller))]
internal static unsafe partial class RustStringMarshaller
{
    public static RustString ConvertToManaged(byte* ptr)
    {
        return new RustString { Value = Utf8StringMarshaller.ConvertToManaged(ptr) };
    }

    [LibraryImport("yaeblog_typst", EntryPoint = "free_rust_string")]
    private static partial void FreeRustString(byte* ptr);

    public static void Free(byte* ptr) => FreeRustString(ptr);
}
