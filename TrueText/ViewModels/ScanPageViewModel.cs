using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using WIA;
using TrueText.Data;
using TrueText.Models; // Ensure this is included!
using MyDevice = TrueText.Models.Device; // Alias to avoid WIA.Device conflict
using CommunityToolkit.Mvvm.ComponentModel;

namespace TrueText.ViewModels
{
    public partial class ScanPageViewModel : ViewModelBase
    {
        private ObservableCollection<MyDevice> _availableDevices = new();
        public ObservableCollection<MyDevice> AvailableDevices
        {
            get => _availableDevices;
            set => SetProperty(ref _availableDevices, value);
        }

        private MyDevice _selectedDevice;
        public MyDevice SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        private Bitmap _scannedImage;
        public Bitmap ScannedImage
        {
            get => _scannedImage;
            set => SetProperty(ref _scannedImage, value);
        }

        public IRelayCommand ScanCommand { get; }

        public ScanPageViewModel()
        {
            LoadDevicesFromDatabase();
            ScanCommand = new RelayCommand(ExecuteScan);
        }

        private void LoadDevicesFromDatabase()
        {
            AvailableDevices.Clear();
            var devicesFromDb = AppDbContext.GetDevices();
            foreach (var d in devicesFromDb)
            {
                AvailableDevices.Add(d);
            }
        }

        private void ExecuteScan()
        {
            try
            {
                if (SelectedDevice == null)
                {
                    Console.WriteLine("Please select a device first.");
                    return;
                }

                DeviceManager dm = new DeviceManager();
                DeviceInfo deviceInfo = null;

                foreach (DeviceInfo info in dm.DeviceInfos)
                {
                    var prop = (Property)info.Properties["Name"];
                    string deviceName = prop != null ? prop.get_Value().ToString() : null;

                    if (deviceName == SelectedDevice.Name)
                    {
                        deviceInfo = info;
                        break;
                    }
                }

                if (deviceInfo == null)
                {
                    Console.WriteLine("No matching scanner device found.");
                    return;
                }

                var wiaDevice = deviceInfo.Connect();
                var scannerItem = wiaDevice.Items[1];

                object scanResult = scannerItem.Transfer(FormatID.wiaFormatJPEG);
                if (scanResult is ImageFile imageFile)
                {
                    byte[] imageBytes = (byte[])imageFile.FileData.get_BinaryData();
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        ScannedImage = new Bitmap(ms);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scan error: {ex.Message}");
            }
        }
    }
}
