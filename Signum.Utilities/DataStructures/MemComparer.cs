using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Signum.Utilities.DataStructures
{
    public static class MemComparer
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable IDE1006 // Naming Styles
        static extern int memcmp(byte[] b1, byte[] b2, long count);
#pragma warning restore IDE1006 // Naming Styles

        public static bool Equals(byte[] b1, byte[] b2)
        {
            // Validate buffers are the same length.
            // This also ensures that the count does not exceed the length of either buffer.
            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }
    }
}
