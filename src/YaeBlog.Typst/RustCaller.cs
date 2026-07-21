using System.Runtime.InteropServices;

namespace YaeBlog.Typst;

public static partial class RustCaller
{
    [LibraryImport("yaeblog_typst", EntryPoint = "process_string", StringMarshalling = StringMarshalling.Utf8)]
    public static partial RustString ProcessString(string value);
}
