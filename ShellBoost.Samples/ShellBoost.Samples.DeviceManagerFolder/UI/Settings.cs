using System.ComponentModel;

namespace ShellBoost.Samples.DeviceManagerFolder.UI
{
    public class Settings : INotifyPropertyChanged
    {
        private bool _showHiddenDevices;

        public event PropertyChangedEventHandler PropertyChanged;

        [DefaultValue(false)]
        [DisplayName("Show Hidden Devices")]
        public bool ShowHiddenDevices
        {
            get => _showHiddenDevices;
            set
            {
                if (_showHiddenDevices == value)
                    return;

                _showHiddenDevices = value;
                OnPropertyChanged(nameof(ShowHiddenDevices));
            }
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
