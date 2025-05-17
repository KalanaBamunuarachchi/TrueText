using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WIA;
using TrueText.Data;
using TrueText.Models;
using System.Timers;

namespace TrueText.ViewModels
{
    public class DevicesPageViewModel : ViewModelBase
    {
        private ObservableCollection<TrueText.Models.Device> _devices = new ObservableCollection<TrueText.Models.Device>();
        public ObservableCollection<TrueText.Models.Device> Devices
        {
            get => _devices;
            set
            {
                _devices = value;
                OnPropertyChanged(nameof(Devices));
            }
        }

        public ICommand AddDeviceCommand { get; }
        private Timer _devicePollingTimer = new Timer();

        public DevicesPageViewModel()
        {
            Devices = new ObservableCollection<TrueText.Models.Device>();
            AddDeviceCommand = new RelayCommand(ScanForDevices);
            LoadDevices();
            //StartDevicePolling();
        }

        private void ScanForDevices()
        {
            try
            {
                Console.WriteLine("ScanForDevices method called.");
                DeviceManager deviceManager = new DeviceManager();
                using (var db = new AppDbContext())
                {
                    foreach (DeviceInfo deviceInfo in deviceManager.DeviceInfos.Cast<DeviceInfo>())
                    {
                        string? deviceName = deviceInfo.Properties["Name"]?.get_Value()?.ToString();
                        if (deviceName != null)
                        {
                            if (!db.Devices.Any(d => d.Name == deviceName))
                            {
                                var device = new TrueText.Models.Device
                                {
                                    Name = deviceName,
                                    Status = "Connected"
                                };

                                db.Devices.Add(device);
                                db.SaveChanges();
                                Devices.Add(device);
                                Console.WriteLine($"Device Added: {deviceName}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning for devices: {ex.Message}");
            }
        }

        private void LoadDevices()
        {
            using (var db = new AppDbContext())
            {
                Devices.Clear();
                var devices = db.Devices.ToList();
                foreach (var device in devices)
                {
                    Devices.Add(device);
                }
            }
        }

        private void StartDevicePolling()
        {
            _devicePollingTimer = new Timer(5000);
            _devicePollingTimer.Elapsed += async (sender, e) =>
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(CheckDeviceStatus);
            _devicePollingTimer.AutoReset = true;
            _devicePollingTimer.Start();
        }

        private void CheckDeviceStatus()
        {
            try
            {
                DeviceManager deviceManager = new DeviceManager();
                var connectedDevices = deviceManager.DeviceInfos
                    .Cast<DeviceInfo>()
                    .Where(d => d.Type == WiaDeviceType.ScannerDeviceType)
                    .Select(d => d.Properties["Name"].get_Value().ToString())
                    .ToList();

                using (var db = new AppDbContext())
                {
                    bool isStatusChanged = false;
                    foreach (var device in Devices.ToList())
                    {
                        bool isConnected = connectedDevices.Contains(device.Name);

                        if (isConnected && device.Status != "Connected")
                        {
                            device.Status = "Connected";
                            db.Devices.Update(device);
                            isStatusChanged = true;
                            Console.WriteLine($"Device {device.Name} Status Updated: Connected");
                        }
                        else if (!isConnected && device.Status != "Disconnected")
                        {
                            device.Status = "Disconnected";
                            db.Devices.Update(device);
                            isStatusChanged = true;
                            Console.WriteLine($"Device {device.Name} Status Updated: Disconnected");
                        }
                    }

                    if (isStatusChanged)
                    {
                        db.SaveChanges();
                        Avalonia.Threading.Dispatcher.UIThread.Invoke(() => OnPropertyChanged(nameof(Devices)));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking device status: {ex.Message}");
            }
        }
    }
}
