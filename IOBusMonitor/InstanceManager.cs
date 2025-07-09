using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace IOBusMonitor
{
    /// <summary>
    /// Ensures that only a single instance of the application runs.
    /// If another instance is detected, its main window receives WM_CLOSE;  
    /// if that fails, the process is killed.
    /// </summary>
    public static class InstanceManager
    {
        [DllImport("user32.dll")]
        private static extern bool PostMessage(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam);

        private const uint WM_CLOSE = 0x0010;

        /// <summary>
        /// Finds any previous instance of the same executable and shuts it down.
        /// </summary>
        public static void EnsureSingleInstanceByClosingPrevious()
        {
            string processName = Process.GetCurrentProcess().ProcessName;
            int currentId = Process.GetCurrentProcess().Id;

            var other = Process
                .GetProcessesByName(processName)
                .FirstOrDefault(p => p.Id != currentId);

            if (other == null) return;

            Console.WriteLine("Closing previous instance…");

            try
            {
                // Politely ask the window to close
                if (other.MainWindowHandle != IntPtr.Zero)
                {
                    PostMessage(other.MainWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    Thread.Sleep(2000); // wait up to 2 s for graceful shutdown
                }

                // Force-kill if still running
                if (!other.HasExited)
                    other.Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Failed to terminate previous instance: " + ex.Message);
            }
        }
    }
}
