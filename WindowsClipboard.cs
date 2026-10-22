using System;
using System.Runtime.InteropServices;

namespace SagesOfOzvaram
{
    /// <summary>
    /// Minimal Win32 clipboard text reader - used by the in-game dev console (Ctrl+V, see
    /// Game1.HandleConsoleInput) so a command can be pasted instead of retyped. Deliberately a
    /// raw P/Invoke rather than System.Windows.Forms.Clipboard, which requires the calling
    /// thread to be STA (Single-Threaded Apartment) - this MonoGame project's entry point
    /// (Program.cs) isn't marked [STAThread] and changing that risked unrelated side effects, so
    /// talking to user32/kernel32 directly avoids the whole question.
    /// </summary>
    public static class WindowsClipboard
    {
        private const uint CF_UNICODETEXT = 13;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        /// <summary>The clipboard's current text, or null if it's empty, holds something other than text, or couldn't be opened (e.g. another process has it locked right now - transient, just means this particular paste attempt gets nothing).</summary>
        public static string GetText()
        {
            if (!OpenClipboard(IntPtr.Zero))
                return null;

            try
            {
                IntPtr handle = GetClipboardData(CF_UNICODETEXT);
                if (handle == IntPtr.Zero)
                    return null;

                IntPtr pointer = GlobalLock(handle);
                if (pointer == IntPtr.Zero)
                    return null;

                try
                {
                    return Marshal.PtrToStringUni(pointer);
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
    }
}
