using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using WIA;
using TrueText.Data;
using TrueText.Models;
using MyDevice = TrueText.Models.Device;
using QuestPDF.Helpers;

namespace TrueText.ViewModels
{
    public partial class ScanPageViewModel : ViewModelBase
    {
        // ── Device selection ───────────────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<MyDevice> availableDevices = new();
        [ObservableProperty] private MyDevice selectedDevice;

        // ── Scan‑option pick‑lists ────────────────────────────────────────────────
        public ObservableCollection<string> ColorProfiles { get; } = new() { "Color", "Grayscale", "Black & White" };
        public ObservableCollection<string> Resolutions { get; } = new() { "150 DPI", "300 DPI", "600 DPI" };
        public ObservableCollection<string> AvailablePageSizes { get; } = new() { "A4", "Letter", "Legal" };   // <‑‑ renamed
        public ObservableCollection<string> FileTypes { get; } = new() { "Image", "Document" };

        [ObservableProperty] private string selectedColorProfile = "Color";
        [ObservableProperty] private string selectedResolution = "300 DPI";
        [ObservableProperty] private string selectedPageSize = "A4";
        public string SelectedFileType { get; set; } = "Image";

        // ── Scanned pages & navigation state ──────────────────────────────────────
        [ObservableProperty] private ObservableCollection<Bitmap> scannedPages = new();
        [ObservableProperty] private int currentPageIndex = 0;
        [ObservableProperty] private Bitmap? currentPage;

        // ── Commands ──────────────────────────────────────────────────────────────
        public IRelayCommand ScanCommand { get; }
        public IRelayCommand ExportCommand { get; }
        public IRelayCommand NextPageCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }
        public IRelayCommand DeletePageCommand { get; }

        // ───────────────────────────────────────────────────────────────────────────
        public ScanPageViewModel()
        {
            LoadDevicesFromDatabase();

            ScanCommand = new RelayCommand(ExecuteScan);
            ExportCommand = new RelayCommand(ExecuteExport, CanExport);
            NextPageCommand = new RelayCommand(NextPage, CanGoNext);
            PreviousPageCommand = new RelayCommand(PreviousPage, CanGoPrevious);
            DeletePageCommand = new RelayCommand(DeleteCurrentPage, CanDeletePage);
        }

        // keep CurrentPage in‑sync when the index moves
        partial void OnCurrentPageIndexChanged(int _) => UpdateCurrentPage();

        // ── DB helpers ────────────────────────────────────────────────────────────
        private void LoadDevicesFromDatabase()
        {
            AvailableDevices.Clear();
            foreach (var d in AppDbContext.GetDevices())
                AvailableDevices.Add(d);
        }

        // ── SCAN ──────────────────────────────────────────────────────────────────
        private void ExecuteScan()
        {
            try
            {
                if (SelectedDevice == null) return;

                var deviceInfo = new DeviceManager().DeviceInfos
                                   .Cast<DeviceInfo>()
                                   .FirstOrDefault(di => di.Properties["Name"].get_Value().ToString() == SelectedDevice.Name);

                if (deviceInfo == null) return;

                var scannerItem = deviceInfo.Connect().Items[1];

                // DPI / intent / page‑size
                SetScannerProperty(scannerItem, "6146", GetIntent());
                SetScannerProperty(scannerItem, "6147", GetDpi());
                SetScannerProperty(scannerItem, "6148", GetDpi());
                var (w, h) = GetPagePixelSize();
                SetScannerProperty(scannerItem, "6151", w);
                SetScannerProperty(scannerItem, "6152", h);

                if (scannerItem.Transfer(FormatID.wiaFormatJPEG) is ImageFile img)
                {
                    using var ms = new MemoryStream((byte[])img.FileData.get_BinaryData());
                    var bmp = new Bitmap(ms);

                    ScannedPages.Add(bmp);
                    CurrentPageIndex = ScannedPages.Count - 1;// will trigger UpdateCurrentPage()
                    UpdateCurrentPage(); 
                    ExportCommand.NotifyCanExecuteChanged();
                    DeletePageCommand.NotifyCanExecuteChanged();
                }
            }
            catch (Exception ex) { Console.WriteLine($"Scan error: {ex.Message}"); }
        }

        // ── EXPORT ────────────────────────────────────────────────────────────────
        private void ExecuteExport()
        {
            if (SelectedFileType == "Image")
                ExportAsImages();
            else
                ExportAsPdf();
        }

        private void ExportAsImages()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueTextExports");
            Directory.CreateDirectory(dir);

            for (int i = 0; i < ScannedPages.Count; i++)
            {
                var path = Path.Combine(dir, $"Scan_Page_{i + 1}_{DateTime.Now:yyyyMMdd_HHmmss}.jpeg");
                using var fs = File.Create(path);
                ScannedPages[i].Save(fs);   // Avalonia saves PNG bytes – we just use .jpeg extension for now
            }
        }

        private void ExportAsPdf()
        {
            if (!ScannedPages.Any()) return;

            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueTextExports");
            Directory.CreateDirectory(dir);
            string pdfPath = Path.Combine(dir, $"Scan_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            var pageImages = ScannedPages
                             .Select(b =>
                             {
                                 using var ms = new MemoryStream();
                                 b.Save(ms);          // PNG bytes
                                 return ms.ToArray();
                             })
                             .ToList();

            var doc = Document.Create(c =>
            {
                foreach (var pageImg in pageImages)
                {
                    c.Page(p =>
                    {
                        p.Size(PageSizes.A4);   // ✅ enum from QuestPDF.Infrastructure
                        p.Margin(20);
                        p.Content().Image(pageImg);
                    });
                }
            });

            doc.GeneratePdf(pdfPath);
        }

        // ── PAGE NAV / DELETE helpers ────────────────────────────────────────────
        private void DeleteCurrentPage()
        {
            if (!ScannedPages.Any()) return;

            ScannedPages.RemoveAt(CurrentPageIndex);
            CurrentPageIndex = Math.Clamp(CurrentPageIndex, 0, ScannedPages.Count - 1);
            UpdateCurrentPage();

            ExportCommand.NotifyCanExecuteChanged();
            DeletePageCommand.NotifyCanExecuteChanged();
        }

        private void NextPage() => CurrentPageIndex++;
        private void PreviousPage() => CurrentPageIndex--;

        private void UpdateCurrentPage()
        {
            if (ScannedPages.Any())
                CurrentPage = ScannedPages[CurrentPageIndex];

            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();
        }

        // ── Can‑Execute shorthand ────────────────────────────────────────────────
        private bool CanGoNext() => CurrentPageIndex < ScannedPages.Count - 1;
        private bool CanGoPrevious() => CurrentPageIndex > 0;
        private bool CanExport() => ScannedPages.Any();
        private bool CanDeletePage() => ScannedPages.Any();

        // ── Utility / mapping helpers ────────────────────────────────────────────
        private int GetDpi() => SelectedResolution switch { "150 DPI" => 150, "600 DPI" => 600, _ => 300 };

        private int GetIntent() => SelectedColorProfile switch
        {
            "Grayscale" => 2,
            "Black & White" => 4,
            _ => 1
        };

        private (int w, int h) GetPagePixelSize()
        {
            int dpi = GetDpi();
            return SelectedPageSize switch
            {
                "Letter" => ((int)(8.5 * dpi), (int)(11 * dpi)),
                "Legal" => ((int)(8.5 * dpi), (int)(14 * dpi)),
                _ => ((int)(8.27 * dpi), (int)(11.69 * dpi))   // A4 default
            };
        }

        private void SetScannerProperty(IItem item, string id, object val)
        {
            try { item.Properties[id].set_Value(val); }
            catch (Exception ex) { Console.WriteLine($"Property {id} failed: {ex.Message}"); }
        }
    }
}
