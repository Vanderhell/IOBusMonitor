using IOBusMonitorLib;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace IOBusMonitor
{
    public sealed class ScreenshotCaptureOptions
    {
        public string OutputDir { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool ResetDemoData { get; set; }
        public bool ResetDemoArchives { get; set; }
    }

    /// <summary>
    /// Non-interactive screenshot mode for documentation builds.
    /// Usage: IOBusMonitor.exe --screenshots [--out &lt;dir&gt;] [--size 1600x900] [--demo-reset] [--demo-reset-archives]
    /// </summary>
    public static class ScreenshotCapture
    {
        public static bool TryParseArgs(string[] args, out ScreenshotCaptureOptions options)
        {
            options = null;
            if (args == null || args.Length == 0)
                return false;

            bool enabled = false;
            var parsed = new ScreenshotCaptureOptions
            {
                OutputDir = Path.Combine("docs", "assets", "screenshots"),
                Width = 1600,
                Height = 900,
                ResetDemoData = true,
                ResetDemoArchives = true
            };

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i] ?? string.Empty;
                if (arg.Equals("--screenshots", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--screenshot", StringComparison.OrdinalIgnoreCase))
                {
                    enabled = true;
                    continue;
                }

                if (arg.Equals("--out", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    parsed.OutputDir = args[++i];
                    continue;
                }

                if (arg.Equals("--size", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    var size = (args[++i] ?? string.Empty).Split('x');
                    if (size.Length == 2 &&
                        int.TryParse(size[0], out int w) &&
                        int.TryParse(size[1], out int h) &&
                        w > 0 && h > 0)
                    {
                        parsed.Width = w;
                        parsed.Height = h;
                    }
                    continue;
                }

                if (arg.Equals("--demo-reset", StringComparison.OrdinalIgnoreCase))
                {
                    parsed.ResetDemoData = true;
                    continue;
                }

                if (arg.Equals("--no-demo-reset", StringComparison.OrdinalIgnoreCase))
                {
                    parsed.ResetDemoData = false;
                    continue;
                }

                if (arg.Equals("--demo-reset-archives", StringComparison.OrdinalIgnoreCase))
                {
                    parsed.ResetDemoArchives = true;
                    continue;
                }

                if (arg.Equals("--no-demo-reset-archives", StringComparison.OrdinalIgnoreCase))
                {
                    parsed.ResetDemoArchives = false;
                    continue;
                }
            }

            if (!enabled)
                return false;

            options = parsed;
            return true;
        }

        public static async Task RunAsync(MainWindow window, ScreenshotCaptureOptions options)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (options == null) throw new ArgumentNullException(nameof(options));

            ConfigureWindow(window, options);
            await WaitForLoadedAsync(window).ConfigureAwait(true);

            if (!(window.DataContext is MainViewModel vm))
                throw new InvalidOperationException("MainWindow DataContext is not MainViewModel.");

            PrepareDemoState(options);

            // Ensure MainViewModel picks up new settings & seeded demo configuration.
            vm.RefreshConfiguration();
            vm.StartMonitoring();
            await WaitForUiIdleAsync(window.Dispatcher, 1200).ConfigureAwait(true);

            string outputDir = Path.GetFullPath(options.OutputDir);
            Directory.CreateDirectory(outputDir);

            using (var log = new StreamWriter(Path.Combine(outputDir, "screenshots.log"), append: false))
            {
                await CaptureSafeAsync(window, outputDir, "home-overview.png", () => vm.ShowHomeCommand.Execute(null), log).ConfigureAwait(true);
                await CaptureSafeAsync(window, outputDir, "dashboard.png", () => vm.ShowDashboardCommand.Execute(null), log).ConfigureAwait(true);
                await CaptureSafeAsync(window, outputDir, "history.png", () => vm.ShowHistoryCommand.Execute(null), log).ConfigureAwait(true);
                await CaptureSafeAsync(window, outputDir, "settings.png", () => vm.ShowAppSettingsCommand.Execute(null), log).ConfigureAwait(true);

                await CaptureSafeAsync(window, outputDir, "modbus-tcp-admin.png", () => vm.ShowModbusTCPDevicesCommand.Execute(null), log).ConfigureAwait(true);
                await CaptureSafeAsync(window, outputDir, "modbus-rtu-admin.png", () => vm.ShowModbusRTUDevicesCommand.Execute(null), log).ConfigureAwait(true);
                await CaptureSafeAsync(window, outputDir, "siemens-s7-admin.png", () => vm.ShowS7DevicesCommand.Execute(null), log).ConfigureAwait(true);
            }
        }

        private static async Task CaptureSafeAsync(MainWindow window, string outputDir, string fileName, Action navigate, StreamWriter log)
        {
            try
            {
                await CaptureAsync(window, outputDir, fileName, navigate).ConfigureAwait(true);
                log?.WriteLine("OK  " + fileName);
            }
            catch (Exception ex)
            {
                log?.WriteLine("ERR " + fileName + " :: " + ex);
            }
        }

        private static void ConfigureWindow(MainWindow window, ScreenshotCaptureOptions options)
        {
            window.WindowState = WindowState.Normal;
            window.Width = options.Width;
            window.Height = options.Height;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Topmost = true;
            window.Activate();
            window.Topmost = false;
        }

        private static async Task CaptureAsync(MainWindow window, string outputDir, string fileName, Action navigate)
        {
            navigate?.Invoke();
            await WaitForUiIdleAsync(window.Dispatcher, 700).ConfigureAwait(true);

            window.UpdateLayout();

            string path = Path.Combine(outputDir, fileName);
            SaveWindowPng(window, path);
        }

        private static void PrepareDemoState(ScreenshotCaptureOptions options)
        {
            var settingsService = new SettingsService();
            var settings = settingsService.LoadSettings();
            settings.AutoStart = false;
            settings.DemoModeEnabled = true;
            settingsService.SaveSettings(settings);

            var demoModeService = new DemoModeService();
            demoModeService.EnsureDemoConfiguration(options.ResetDemoData);
            demoModeService.SetDemoDeviceActiveState(true);

            if (options.ResetDemoArchives)
                demoModeService.ResetDemoDataArchives(settings.PathData);
        }

        private static Task WaitForLoadedAsync(Window window)
        {
            if (window.IsLoaded)
                return WaitForUiIdleAsync(window.Dispatcher, 200);

            var tcs = new TaskCompletionSource<object>();
            RoutedEventHandler handler = null;
            handler = (_, __) =>
            {
                window.Loaded -= handler;
                tcs.TrySetResult(null);
            };
            window.Loaded += handler;
            return tcs.Task.ContinueWith(_ => WaitForUiIdleAsync(window.Dispatcher, 200)).Unwrap();
        }

        private static async Task WaitForUiIdleAsync(Dispatcher dispatcher, int minDelayMs)
        {
            await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            if (minDelayMs > 0)
                await Task.Delay(minDelayMs).ConfigureAwait(true);
            await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static void SaveWindowPng(Window window, string path)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Invalid path.", nameof(path));

            var source = PresentationSource.FromVisual(window);
            var m = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
            int pixelWidth = (int)Math.Round(window.ActualWidth * m.M11);
            int pixelHeight = (int)Math.Round(window.ActualHeight * m.M22);
            if (pixelWidth <= 0 || pixelHeight <= 0)
                throw new InvalidOperationException("Window has invalid render size.");

            var rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, 96.0 * m.M11, 96.0 * m.M22, PixelFormats.Pbgra32);
            rtb.Render(window);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                encoder.Save(fs);
            }
        }
    }
}
