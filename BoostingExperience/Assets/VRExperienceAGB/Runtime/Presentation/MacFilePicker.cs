using System;
using System.Runtime.InteropServices;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Local native file selection; the existing importer still validates all input.</summary>
    public static class MacFilePicker
    {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        [DllImport("VRAGBDesktop")] private static extern IntPtr VRAGBPickFile(int profiles);
        [DllImport("VRAGBDesktop")] private static extern void VRAGBFreePath(IntPtr path);
#endif
        public static string Pick(bool profiles)
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            IntPtr path = VRAGBPickFile(profiles ? 1 : 0);
            try { return path == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(path); }
            finally { if (path != IntPtr.Zero) VRAGBFreePath(path); }
#else
            throw new PlatformNotSupportedException("Native file selection is available in the macOS player.");
#endif
        }
    }
}
