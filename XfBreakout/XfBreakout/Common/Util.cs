using System;
using System.Diagnostics;

namespace XfBreakout.Common
{
    public static class Util
    {
        public static void Log(string msg)
        {
#if DEBUG
#if !WINDOWS_UWP
            Debug.WriteLine(msg);
#else
			System.Diagnostics.Debug.WriteLine(msg);
#endif
#endif
        }
    }
}