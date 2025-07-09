using IWshRuntimeLibrary;
using System;
using System.IO;
using System.Reflection;
using File = System.IO.File;

// using HandyControl.Controls; // Uncomment if using Growl for visual error messages

namespace ShortcutTool
{
    /// <summary>
    /// Utility for managing a startup shortcut for the IOBusMonitor application.
    /// Allows adding, removing, or checking the existence of a Windows startup link.
    /// </summary>
    internal class StartupShortcutManager
    {
        private static readonly string shortcutName = "IOBusMonitor.lnk";
        private static readonly string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        private static readonly string shortcutPath = Path.Combine(startupDir, shortcutName);

        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("=====================================");
                Console.WriteLine("Select an option:");
                Console.WriteLine("  add     - add IOBusMonitor to startup");
                Console.WriteLine("  remove  - remove IOBusMonitor from startup");
                Console.WriteLine("  check   - check startup folder contents");
                Console.WriteLine("  exit    - exit application");
                Console.Write("Enter your choice: ");

                string input = Console.ReadLine()?.Trim().ToLower();

                try
                {
                    switch (input)
                    {
                        case "add":
                            string exePath = Assembly.GetExecutingAssembly().Location;
                            string iconPath = Path.ChangeExtension(exePath, ".ico");
                            CreateStartupShortcut(exePath, iconPath);
                            break;

                        case "remove":
                            RemoveStartupShortcut();
                            break;

                        case "check":
                            CheckStartupFolder();
                            break;

                        case "exit":
                            return;

                        default:
                            Console.WriteLine("❌ Invalid choice.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    // Growl.Error($"Startup shortcut error: {ex.Message}");
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Creates a Windows shortcut to the executable in the user's Startup folder.
        /// </summary>
        /// <param name="targetPath">Path to the executable file.</param>
        /// <param name="iconPath">Path to the icon file (optional).</param>
        private static void CreateStartupShortcut(string targetPath, string iconPath)
        {
            try
            {
                if (File.Exists(shortcutPath))
                {
                    Console.WriteLine("⚠️  Shortcut already exists in Startup.");
                    return;
                }

                var shell = new WshShell();
                var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.Description = "IOBusMonitor Auto Startup";
                shortcut.TargetPath = targetPath;
                shortcut.IconLocation = File.Exists(iconPath) ? iconPath : targetPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                shortcut.Save();

                Console.WriteLine("✅ Shortcut added to Startup.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to create shortcut: {ex.Message}");
                // Growl.Error($"Failed to create startup shortcut: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes the startup shortcut if it exists.
        /// </summary>
        private static void RemoveStartupShortcut()
        {
            try
            {
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                    Console.WriteLine("🗑️  Shortcut removed from Startup.");
                }
                else
                {
                    Console.WriteLine("⚠️  No shortcut found in Startup.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to remove shortcut: {ex.Message}");
                // Growl.Error($"Failed to remove startup shortcut: {ex.Message}");
            }
        }

        /// <summary>
        /// Lists all .lnk files in the Startup folder and highlights the monitored one.
        /// </summary>
        private static void CheckStartupFolder()
        {
            try
            {
                Console.WriteLine("\n📂 Startup folder contents:");
                var lnkFiles = Directory.GetFiles(startupDir, "*.lnk");

                if (lnkFiles.Length == 0)
                {
                    Console.WriteLine(" (no shortcuts found)");
                    return;
                }

                foreach (var file in lnkFiles)
                {
                    string fileName = Path.GetFileName(file);
                    bool isMain = string.Equals(fileName, shortcutName, StringComparison.OrdinalIgnoreCase);

                    Console.WriteLine($" - {fileName}" + (isMain ? "  ✅ [IOBusMonitor]" : ""));
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to read Startup folder: {ex.Message}");
                // Growl.Error($"Failed to check startup folder: {ex.Message}");
            }
        }
    }
}
