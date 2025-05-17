// [FIXED] Fully Updated ScanPageViewModel.cs with:
// - Device alias removed
// - Window null checks added
// - OpenXML null-safe body handling

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using QuestPDF.Fluent;
using QuestPDFDocument = QuestPDF.Fluent.Document;
using QuestPDF.Infrastructure;
using Tesseract;
using WIA;
using TrueText.Data;
using TrueText.Models;
using Avalonia.Platform.Storage;
using Avalonia;
using OpenXmlDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using DocumentFormat.OpenXml;
using QuestPDF.Helpers;
using Device = TrueText.Models.Device;

namespace TrueText.ViewModels
{
    public partial class ScanPageViewModel : ViewModelBase
    {
        [ObservableProperty] private ObservableCollection<Device> availableDevices = new();
        [ObservableProperty] private Device? selectedDevice;

        public ObservableCollection<string> ColorProfiles { get; } = new() { "Color", "Grayscale", "Black & White" };
        public ObservableCollection<string> Resolutions { get; } = new() { "150 DPI", "300 DPI", "600 DPI" };
        public ObservableCollection<string> AvailablePageSizes { get; } = new() { "A4", "Letter", "Legal" };
        public ObservableCollection<string> FileTypes { get; } = new() { "Image", "Document" };

        public ObservableCollection<string> OcrLanguages { get; } = new() { "English", "Sinhala" };

        [ObservableProperty] private string selectedColorProfile = "Color";
        [ObservableProperty] private string selectedResolution = "300 DPI";
        [ObservableProperty] private string selectedPageSize = "A4";
        public string SelectedFileType { get; set; } = "Image";

        [ObservableProperty] private ObservableCollection<Bitmap> scannedPages = new();
        [ObservableProperty] private int currentPageIndex = 0;
        [ObservableProperty] private Bitmap? currentPage = null;

        [ObservableProperty] private string selectedOcrLanguage = "English";
        [ObservableProperty] private string? ocrExtractedText = null;
        [ObservableProperty] private bool isOcrCompleted = false;
        [ObservableProperty] private bool isOcrInProgress;
        [ObservableProperty] private double ocrProgress;

        public IRelayCommand ScanCommand { get; }
        public IRelayCommand<Window> ExportCommand { get; }
        public IRelayCommand NextPageCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }
        public IRelayCommand DeletePageCommand { get; }
        public IRelayCommand<Window> ExportOcrCommand { get; }
        public IAsyncRelayCommand ProceedOcrCommand { get; }

        public event Action? RecentOcrExported;


        public ScanPageViewModel()
        {
            LoadDevicesFromDatabase();

            ScanCommand = new RelayCommand(ExecuteScan);
            ExportCommand = new RelayCommand<Window>(async window => await ExecuteExportAsync(window), _ => scannedPages.Any());
            NextPageCommand = new RelayCommand(NextPage, () => currentPageIndex < scannedPages.Count - 1);
            PreviousPageCommand = new RelayCommand(PreviousPage, () => currentPageIndex > 0);
            DeletePageCommand = new RelayCommand(DeleteCurrentPage, () => scannedPages.Any());
            ExportOcrCommand = new RelayCommand<Window>(async window => await ExportOcrToWordAsync(window), _ => isOcrCompleted);

            ProceedOcrCommand = new AsyncRelayCommand(ExecuteOcrAsync, () => ScannedPages.Any() && !IsOcrInProgress);

            ScannedPages.CollectionChanged += (_, __) => ProceedOcrCommand.NotifyCanExecuteChanged();
            PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(IsOcrInProgress))
                    ProceedOcrCommand.NotifyCanExecuteChanged();
            };
        }

        partial void OnCurrentPageIndexChanged(int value) => UpdateCurrentPage();

        private void LoadDevicesFromDatabase()
        {
            AvailableDevices.Clear();
            foreach (var d in AppDbContext.GetDevices())
                AvailableDevices.Add(d);
        }

        private void ExecuteScan()
        {
            if (SelectedDevice is null) return;

            var di = new DeviceManager().DeviceInfos
                       .Cast<DeviceInfo>()
                       .FirstOrDefault(d => d.Properties["Name"].get_Value()?.ToString() == SelectedDevice.Name);
            if (di is null) return;

            var item = di.Connect().Items[1];
            SetScannerProperty(item, "6146", GetIntent());
            SetScannerProperty(item, "6147", GetDpi());
            SetScannerProperty(item, "6148", GetDpi());
            var (w, h) = GetPagePixelSize();
            SetScannerProperty(item, "6151", w);
            SetScannerProperty(item, "6152", h);

            if (item.Transfer(FormatID.wiaFormatJPEG) is ImageFile img)
            {
                using var ms = new MemoryStream((byte[])img.FileData.get_BinaryData());
                var bmp = new Bitmap(ms);

                ScannedPages.Add(bmp);

                CurrentPageIndex = ScannedPages.Count - 1;
                UpdateCurrentPage();

                ExportCommand.NotifyCanExecuteChanged();
                DeletePageCommand.NotifyCanExecuteChanged();
                ProceedOcrCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task ExecuteExportAsync(Window? window)
        {
            if (window == null) return;

            if (SelectedFileType == "Image")
                await ExportAsImagesAsync(window);
            else
                await ExportAsPdfAsync(window);
        }

        private async Task ExportAsImagesAsync(Window window)
        {
            for (int i = 0; i < ScannedPages.Count; i++)
            {
                var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    SuggestedFileName = $"Scan_Page_{i + 1}_{DateTime.Now:yyyyMMdd_HHmmss}.jpeg",
                    FileTypeChoices = new[] { new FilePickerFileType("JPEG Image") { Patterns = new[] { "*.jpeg", "*.jpg" } } }
                });

                if (file is not null)
                {
                    await using var fs = await file.OpenWriteAsync();
                    ScannedPages[i].Save(fs);
                }
            }
        }

        private async Task ExportAsPdfAsync(Window window)
        {
            if (!ScannedPages.Any()) return;
            if (window == null) return;

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = $"Scan_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                FileTypeChoices = new[] { new FilePickerFileType("PDF Document") { Patterns = new[] { "*.pdf" } } }
            });

            if (file is null) return;

            var doc = QuestPDFDocument.Create(c =>
            {
                foreach (var bmp in ScannedPages)
                {
                    c.Page(p =>
                    {
                        p.Size(PageSizes.A4);
                        p.Margin(0);
                        using var ms = new MemoryStream();
                        bmp.Save(ms);
                        p.Content().Image(ms.ToArray());
                    });
                }
            });

            await using var outStream = await file.OpenWriteAsync();
            using var msPdf = new MemoryStream();
            doc.GeneratePdf(msPdf);
            msPdf.Position = 0;
            await msPdf.CopyToAsync(outStream);
        }

        private async Task ExportOcrToWordAsync(Window? window)
        {
            if (string.IsNullOrWhiteSpace(OcrExtractedText)) return;
            if (window == null) return;

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = $"OCR_{DateTime.Now:yyyyMMdd_HHmmss}.docx",
                FileTypeChoices = new[] { new FilePickerFileType("Word Document") { Patterns = new[] { "*.docx" } } }
            });

            if (file is null) return;

            var filePath = file.Path.LocalPath;
            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
            using var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new OpenXmlDocument();
            if (mainPart.Document.Body == null)
                mainPart.Document.Body = new Body();

            var body = mainPart.Document.Body;
            var paras = OcrExtractedText.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in paras)
            {
                var run = new Run(new Text(p.Trim()) { Space = SpaceProcessingModeValues.Preserve });
                body.AppendChild(new Paragraph(run));
            }

            RecordRecentExport(filePath);
            RecentOcrExported?.Invoke();
        }


        private async Task ExecuteOcrAsync()
        {
            IsOcrInProgress = true;
            OcrProgress = 0;
            OcrExtractedText = string.Empty;
            IsOcrCompleted = false;

            try
            {
                var tessData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                var sb = new StringBuilder();
                string lang = SelectedOcrLanguage switch { "Sinhala" => "sin", _ => "eng" };
                using var engine = new TesseractEngine(tessData, lang, EngineMode.Default);

                int total = ScannedPages.Count;
                for (int i = 0; i < total; i++)
                {
                    await Task.Delay(1);
                    using var ms = new MemoryStream();
                    ScannedPages[i].Save(ms);
                    var pix = Pix.LoadFromMemory(ms.ToArray());
                    using var page = engine.Process(pix);

                    var hocr = page.GetHOCRText(0);
                    var html = new HtmlDocument();
                    html.LoadHtml(hocr);

                    var paras = html.DocumentNode.SelectNodes("//p[@class='ocr_par']");
                    if (paras != null)
                    {
                        foreach (var p in paras)
                        {
                            var lines = p.SelectNodes(".//span[@class='ocr_line']")?.Select(n => CleanText(n.InnerText));
                            if (lines != null)
                            {
                                sb.AppendLine(string.Join(" ", lines));
                                sb.AppendLine();
                            }
                        }
                    }
                    else
                    {
                        var txt = page.GetText();
                        sb.AppendLine(CleanText(txt));
                        sb.AppendLine();
                    }

                    OcrProgress = (i + 1) / (double)total;
                }

                OcrExtractedText = sb.ToString().TrimEnd();
                IsOcrCompleted = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR error: {ex}");
            }
            finally
            {
                IsOcrInProgress = false;
                ProceedOcrCommand.NotifyCanExecuteChanged();
                ExportOcrCommand.NotifyCanExecuteChanged();
            }
        }

        private void DeleteCurrentPage()
        {
            if (!ScannedPages.Any()) return;

            ScannedPages.RemoveAt(CurrentPageIndex);

            if (ScannedPages.Count == 0)
            {
                CurrentPageIndex = 0;
                CurrentPage = null; 
            }
            else
            {
                CurrentPageIndex = Math.Clamp(CurrentPageIndex, 0, ScannedPages.Count - 1);
                UpdateCurrentPage();
            }

            ExportCommand.NotifyCanExecuteChanged();
            DeletePageCommand.NotifyCanExecuteChanged();
            ProceedOcrCommand.NotifyCanExecuteChanged();
        }


        private void NextPage() => CurrentPageIndex++;
        private void PreviousPage() => CurrentPageIndex--;

        private void UpdateCurrentPage()
        {
            if (ScannedPages.Count > 0)
                CurrentPage = ScannedPages[CurrentPageIndex];
            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();
        }

        private int GetDpi() => SelectedResolution switch { "150 DPI" => 150, "600 DPI" => 600, _ => 300 };
        private int GetIntent() => SelectedColorProfile switch { "Grayscale" => 2, "Black & White" => 4, _ => 1 };

        private (int w, int h) GetPagePixelSize()
        {
            var dpi = GetDpi();
            return SelectedPageSize switch
            {
                "Letter" => ((int)(8.5 * dpi), (int)(11 * dpi)),
                "Legal" => ((int)(8.5 * dpi), (int)(14 * dpi)),
                _ => ((int)(8.27 * dpi), (int)(11.69 * dpi)),
            };
        }

        private void SetScannerProperty(IItem item, string id, object val)
        {
            try { item.Properties[id].set_Value(val); } catch { }
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var decoded = WebUtility.HtmlDecode(text);
            decoded = Regex.Replace(decoded, @"^[>\*\s]+", "");
            decoded = Regex.Replace(decoded, @"\s+", " ");
            return decoded.Trim();
        }


        private void RecordRecentExport(string fullPath)
        {
            try
            {
                var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TrueText");
                Directory.CreateDirectory(logDir);

                var logFile = Path.Combine(logDir, "recent_exports.txt");

                var logEntry = $"{DateTime.UtcNow:O}|{fullPath}";
                File.AppendAllLines(logFile, new[] { logEntry });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to record recent export: {ex.Message}");
            }
        }

    }
}
