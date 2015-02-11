using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitRebaseOperation
    {
        internal git_rebase_operation type;
        internal GitOid id;
        internal IntPtr exec;
    }

    internal enum git_rebase_operation
    {
        Pick = 0,
        Reword,
        Edit,
        Squash,
        Fixup,
        Exec
    }
}
