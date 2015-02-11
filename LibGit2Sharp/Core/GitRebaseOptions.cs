using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    struct GitRebaseOptions
    {
        uint version;

        int quiet;

        IntPtr rewrite_notes_ref;
    }
}
