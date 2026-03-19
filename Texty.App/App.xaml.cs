using System.Diagnostics;
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
                Trace.TraceError(ex.ToString());
#if DEBUG
                const bool showDiagnostics = true;
#else
                const bool showDiagnostics = false;
#endif
                window.Content = BuildStartupErrorView(ex, showDiagnostics);
            }

            window.Activate();
        }

        private static UIElement BuildStartupErrorView(Exception ex, bool showDiagnostics)
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
                    : "Beim Start ist ein Fehler aufgetreten. Bitte Logs prüfen oder Support kontaktieren.",
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
    }
}
