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
        /// <summary>
        /// Initialisiert die Anwendung und lädt die per XAML definierten Komponenten.
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <summary>
        /// Behandelt das Starten der Anwendung, initialisiert das Hauptfenster, setzt dessen Inhalt und aktiviert das Fenster.
        /// </summary>
        /// <param name="e">Start- und Aktivierungsinformationen für den Anwendungsstart.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            window ??= new Window();

            try
            {
                window.Content = new MainPage();
            }
            catch (Exception ex)
            {
                window.Content = BuildStartupErrorView(ex);
            }

            window.Activate();
        }
        /// <summary>
        /// Erstellt eine sichtbare Fehleransicht für Startfehler, die Meldung, Typ und vollständige Details der übergebenen Ausnahme anzeigt.
        /// </summary>
        /// <param name="ex">Die beim Start aufgetretene Ausnahme; ihr Typ und ihre Nachricht werden prominent gezeigt, der vollständige Stacktrace wird im Details-Feld angezeigt.</param>
        /// <returns>Ein UIElement (ScrollViewer), das die zusammengebaute Fehleransicht mit Nachricht und einem lesbaren Details-Text enthält.</returns>
        private static UIElement BuildStartupErrorView(Exception ex)
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
                Text = $"{ex.GetType().Name}: {ex.Message}",
                TextWrapping = TextWrapping.Wrap,
            });

            panel.Children.Add(new TextBox
            {
                Header = "Details",
                Text = ex.ToString(),
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
