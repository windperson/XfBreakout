using System;

namespace XfBreakout.Common
{
    public static class Util
    {
        public static void Log(string msg)
        {
#if DEBUG
#if WINDOWS_UWP
            System.Diagnostics.Debug.WriteLine(msg);       
#else
            Console.WriteLine(msg);
#endif
#endif
        }
    }
}