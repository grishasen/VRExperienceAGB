using System;
using System.IO;
using System.Runtime.InteropServices;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Unicode Windows file dialog; selected files use the existing validated importer.</summary>
    public static class WindowsFilePicker
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Native OPENFILENAMEW layout, including the reserved fields on 64-bit Windows.
        [StructLayout(LayoutKind.Sequential)]
        private struct OpenFileName
        {
            public int size;
            public IntPtr owner, instance, filter, customFilter;
            public int maxCustomFilter, filterIndex;
            public IntPtr file;
            public int maxFile;
            public IntPtr fileTitle;
            public int maxFileTitle;
            public IntPtr initialDirectory, title;
            public int flags;
            public ushort fileOffset, extensionOffset;
            public IntPtr defaultExtension, customData, hook, templateName, reserved;
            public int reservedValue, flagsEx;
        }
        [DllImport("comdlg32.dll", EntryPoint = "GetOpenFileNameW", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOpenFileName(ref OpenFileName dialog);
        [DllImport("comdlg32.dll", ExactSpelling = true)]
        private static extern uint CommDlgExtendedError();
        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetActiveWindow();
#endif
        public static string Pick(bool profiles)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            const int capacity = 32768;
            IntPtr file = IntPtr.Zero, filter = IntPtr.Zero, title = IntPtr.Zero;
            string directory = Directory.GetCurrentDirectory();
            try
            {
                file = Marshal.AllocHGlobal(capacity * sizeof(char));
                Marshal.WriteInt16(file, 0);
                filter = Marshal.StringToHGlobalUni(profiles ? "ZIP / JSON / JSONL\0*.zip;*.json;*.jsonl\0\0" : "ZIP / JSON\0*.zip;*.json\0\0");
                title = Marshal.StringToHGlobalUni(profiles ? "Import profiles or package" : "Import model or package");
                var dialog = new OpenFileName {
                    size = Marshal.SizeOf<OpenFileName>(), owner = GetActiveWindow(),
                    filter = filter, filterIndex = 1, file = file, maxFile = capacity, title = title,
                    // Explorer, existing file/path, preserve directory, hide read-only, omit recent-files entry.
                    flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008 | 0x00000004 | 0x02000000
                };
                if (GetOpenFileName(ref dialog)) return Marshal.PtrToStringUni(file);
                uint error = CommDlgExtendedError();
                if (error != 0) throw new IOException("Windows file dialog failed: 0x" + error.ToString("X"));
                return null;
            }
            finally
            {
                if (file != IntPtr.Zero) Marshal.FreeHGlobal(file);
                if (filter != IntPtr.Zero) Marshal.FreeHGlobal(filter);
                if (title != IntPtr.Zero) Marshal.FreeHGlobal(title);
                // GetOpenFileName does not consistently honor OFN_NOCHANGEDIR.
                if (Directory.GetCurrentDirectory() != directory) Directory.SetCurrentDirectory(directory);
            }
#else
            throw new PlatformNotSupportedException("Native Windows file selection requires Windows.");
#endif
        }
    }
}
