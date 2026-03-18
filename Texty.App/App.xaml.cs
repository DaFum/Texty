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
                window.Content = BuildStartupErrorView(ex);
            }

            window.Activate();
        }
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
