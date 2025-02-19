using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WIA;


namespace TrueText.ViewModels
{
    public class DevicesPageViewModel : ViewModelBase
    {
        private ObservableCollection<string> _devices;
        public ObservableCollection<string> Devices
        {
            get => _devices;
            set
            {
                _devices = value;
                OnPropertyChanged(nameof(Devices));
            }
        }

        public ICommand AddDeviceCommand { get; }

        public DevicesPageViewModel()
        {
            Devices = new ObservableCollection<string>();
            AddDeviceCommand = new RelayCommand(ScanForDevices);
        }

        private void ScanForDevices()
        {
            try
            {
                Console.WriteLine("ScanForDevices method called.");
                Devices.Clear();
                DeviceManager deviceManager = new DeviceManager();

                for (int i = 1; i <= deviceManager.DeviceInfos.Count; i++)
                {
                    DeviceInfo deviceInfo = deviceManager.DeviceInfos[i];
                    if (deviceInfo.Type == WiaDeviceType.ScannerDeviceType)
                    {
                        object nameProperty = "Name";
                        string deviceName = deviceInfo.Properties[nameProperty].get_Value().ToString();
                        Devices.Add(deviceName);
                        Console.WriteLine($"Device found: {deviceName}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle scanning error (e.g., log error or show message)
                Console.WriteLine($"Error scanning for devices: {ex.Message}");
            }
        }
    }
}
