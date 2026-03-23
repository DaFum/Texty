using System.Diagnostics;
using System.Text;
using Texty.App.Pages;
using Microsoft.UI.Xaml.Media;

namespace Texty.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            window ??= new Window();

            try
            {
                window.Content = new MainPage();
            }
            catch (Exception ex)
            {
#if DEBUG
                const bool showDiagnostics = true;
#else
                const bool showDiagnostics = false;
#endif
                Trace.TraceError(BuildStartupLogMessage(ex, showDiagnostics));
                var startupLogPath = WriteStartupExceptionLog(ex, showDiagnostics);
                window.Content = BuildStartupErrorView(ex, showDiagnostics, startupLogPath);
            }

            window.Activate();
        }

        private static UIElement BuildStartupErrorView(Exception ex, bool showDiagnostics, string? startupLogPath)
        {
            var panel = new StackPanel
            {
                Padding = new Thickness(16),
                Spacing = 10,
            };

            panel.Children.Add(new TextBlock
            {
                Text = "Texty konnte nicht gestartet werden.",
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.IndianRed),
            });

            panel.Children.Add(new TextBlock
            {
                Text = showDiagnostics
                    ? $"{ex.GetType().Name}: {ex.Message}"
                    : "Beim Start ist ein Fehler aufgetreten.",
                TextWrapping = TextWrapping.Wrap,
            });

            panel.Children.Add(new TextBox
            {
                Header = "Details",
                Text = showDiagnostics
                    ? ex.ToString()
                    : BuildGenericStartupMessage(startupLogPath),
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
            });

            return new ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            };
        }

        private static string BuildGenericStartupMessage(string? startupLogPath)
        {
            const string fallbackMessage = "Beim Start ist ein Fehler aufgetreten. Bitte Logs prüfen oder Support kontaktieren.";
            if (string.IsNullOrWhiteSpace(startupLogPath))
            {
                return fallbackMessage;
            }

            return $"{fallbackMessage}{Environment.NewLine}Logdatei: {startupLogPath}";
        }

        private static string? WriteStartupExceptionLog(Exception ex, bool includeDiagnostics)
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(localAppData))
                {
                    return null;
                }

                var logsDirectory = Path.Combine(localAppData, "Texty", "Logs");
                Directory.CreateDirectory(logsDirectory);
                var cutoff = DateTime.UtcNow.AddDays(-30);
                foreach (var file in Directory.EnumerateFiles(logsDirectory, "startup-*.log"))
                {
                    try
                    {
                        if (File.GetLastWriteTimeUtc(file) < cutoff)
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        // Ignore retention cleanup failures.
                    }
                }

                var logPath = Path.Combine(logsDirectory, $"startup-{DateTime.UtcNow:yyyyMMdd}.log");
                var builder = new StringBuilder();
                builder.AppendLine($"[{DateTimeOffset.UtcNow:O}] Startup exception");
                builder.AppendLine(BuildStartupLogMessage(ex, includeDiagnostics));
                builder.AppendLine(new string('-', 80));
                File.AppendAllText(logPath, builder.ToString(), Encoding.UTF8);
                return logPath;
            }
            catch
            {
                return null;
            }
        }

        private static string BuildStartupLogMessage(Exception ex, bool includeDiagnostics)
        {
            if (includeDiagnostics)
            {
                return ex.ToString();
            }

            return $"Type={ex.GetType().FullName}; HResult=0x{ex.HResult:X8}; Message={ex.Message}";
        }
    }
}
