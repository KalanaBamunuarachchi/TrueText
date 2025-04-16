using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using WIA;
using TrueText.Data;
using TrueText.Models;
using MyDevice = TrueText.Models.Device; 
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

        // Scan Options
        public ObservableCollection<string> ColorProfiles { get; } = new() { "Color", "Grayscale", "Black & White" };
        public string SelectedColorProfile { get; set; } = "Color";

        public ObservableCollection<string> Resolutions { get; } = new() { "150 DPI", "300 DPI", "600 DPI" };
        public string SelectedResolution { get; set; } = "300 DPI";

        public ObservableCollection<string> PageSizes { get; } = new() { "A4", "Letter", "Legal" };
        public string SelectedPageSize { get; set; } = "A4";

        public ObservableCollection<string> FileTypes { get; } = new() { "JPEG", "PNG", "PDF" };
        public string SelectedFileType { get; set; } = "JPEG";



        public Bitmap ScannedImage
        {
            get => _scannedImage;
            set => SetProperty(ref _scannedImage, value);
        }

        public IRelayCommand ScanCommand { get; }
        public IRelayCommand ExportCommand { get; }



        public ScanPageViewModel()
        {
            LoadDevicesFromDatabase();

            ScanCommand = new RelayCommand(ExecuteScan);
            ExportCommand = new RelayCommand(ExecuteExport, CanExport);
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

                int dpi = GetDpi();
                int intent = GetIntent();
                var (width, height) = GetPagePixelSize();

                SetScannerProperty(scannerItem, "6146", intent); // CurrentIntent
                SetScannerProperty(scannerItem, "6147", dpi);    // Horizontal Resolution
                SetScannerProperty(scannerItem, "6148", dpi);    // Vertical Resolution
                SetScannerProperty(scannerItem, "6151", width);  // Horizontal Extent
                SetScannerProperty(scannerItem, "6152", height); // Vertical Extent

                object scanResult = scannerItem.Transfer(GetFormatID());
                if (scanResult is ImageFile imageFile)
                {
                    byte[] imageBytes = (byte[])imageFile.FileData.get_BinaryData();
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        ScannedImage = new Bitmap(ms);
                        ExportCommand.NotifyCanExecuteChanged();

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scan error: {ex.Message}");
            }

        }

        private void ExecuteExport()
        {
            if (ScannedImage == null)
            {
                Console.WriteLine("No scanned image to export.");
                return;
            }

            try
            {
                string exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueTextExports");
                Directory.CreateDirectory(exportDir);

                string extension = SelectedFileType.ToLower(); // "jpeg" or "png"
                string filename = $"Scan_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
                string filePath = Path.Combine(exportDir, filename);

                using (FileStream stream = File.Create(filePath))
                {
                    ScannedImage.Save(stream); // Avalonia Bitmap.Save saves PNG by default
                }

                Console.WriteLine($"Exported to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export failed: {ex.Message}");
            }
        }

        private bool CanExport()
        {
            return ScannedImage != null;
        }


        // 📏 DPI Mapping
        private int GetDpi() => SelectedResolution switch
        {
            "150 DPI" => 150,
            "600 DPI" => 600,
            _ => 300
        };


        private int GetIntent() => SelectedColorProfile switch
        {
            "Grayscale" => 2,
            "Black & White" => 4,
            _ => 1 // Default to Color
        };


        private (int width, int height) GetPagePixelSize()
        {
            int dpi = GetDpi();
            return SelectedPageSize switch
            {
                "Letter" => ((int)(8.5 * dpi), (int)(11 * dpi)),
                "Legal" => ((int)(8.5 * dpi), (int)(14 * dpi)),
                _ => ((int)(8.27 * dpi), (int)(11.69 * dpi)) // A4
            };
        }

        private void SetScannerProperty(IItem item, string propertyId, object value)
        {
            try
            {
                var prop = item.Properties[propertyId];
                prop.set_Value(value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set property {propertyId}: {ex.Message}");
            }
        }

        private string GetFormatID() => SelectedFileType switch
        {
            "PNG" => FormatID.wiaFormatPNG,
            "JPEG" => FormatID.wiaFormatJPEG,
            _ => FormatID.wiaFormatBMP
        };

    }

}