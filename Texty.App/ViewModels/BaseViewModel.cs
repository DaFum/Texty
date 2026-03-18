namespace Texty.App.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="BaseViewModel"/>.
        /// </summary>
        public BaseViewModel()
        {

        }

        [ObservableProperty]
        private string _title = string.Empty;
    }
}
