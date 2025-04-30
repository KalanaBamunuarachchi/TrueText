// ScanPageViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
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
using MyDevice = TrueText.Models.Device;
using OpenXmlDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using DocumentFormat.OpenXml;
using QuestPDF.Helpers;





namespace TrueText.ViewModels
{
    public partial class ScanPageViewModel : ViewModelBase
    {
        // ── Pick-lists & selection ───────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<MyDevice> availableDevices = new();
        [ObservableProperty] private MyDevice? selectedDevice;

        public ObservableCollection<string> ColorProfiles { get; } = new() { "Color", "Grayscale", "Black & White" };
        public ObservableCollection<string> Resolutions { get; } = new() { "150 DPI", "300 DPI", "600 DPI" };
        public ObservableCollection<string> AvailablePageSizes { get; } = new() { "A4", "Letter", "Legal" };
        public ObservableCollection<string> FileTypes { get; } = new() { "Image", "Document" };

        public ObservableCollection<string> OcrLanguages { get; } = new() { "English", "Sinhala" };

        [ObservableProperty] private string selectedColorProfile = "Color";
        [ObservableProperty] private string selectedResolution = "300 DPI";
        [ObservableProperty] private string selectedPageSize = "A4";
        public string SelectedFileType { get; set; } = "Image";

        // ── Scanned pages state ────────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<Bitmap> scannedPages = new();
        [ObservableProperty] private int currentPageIndex = 0;
        [ObservableProperty] private Bitmap? currentPage = null;

        // ── OCR state ───────────────────────────────────────────────────────────
        [ObservableProperty] private string selectedOcrLanguage = "English";


        [ObservableProperty] private string? ocrExtractedText = null;
        [ObservableProperty] private bool isOcrCompleted = false;


        [ObservableProperty] private bool isOcrInProgress;
        [ObservableProperty] private double ocrProgress;


        // ── Commands ───────────────────────────────────────────────────────────
        public IRelayCommand ScanCommand { get; }
        public IRelayCommand ExportCommand { get; }
        public IRelayCommand NextPageCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }
        public IRelayCommand DeletePageCommand { get; }
        public IRelayCommand ExportOcrCommand { get; }

        public IAsyncRelayCommand ProceedOcrCommand { get; }
       

        public ScanPageViewModel()
        {
            LoadDevicesFromDatabase();

            ScanCommand = new RelayCommand(ExecuteScan);
            ExportCommand = new RelayCommand(ExecuteExport, () => scannedPages.Any());
            NextPageCommand = new RelayCommand(NextPage, () => currentPageIndex < scannedPages.Count - 1);
            PreviousPageCommand = new RelayCommand(PreviousPage, () => currentPageIndex > 0);
            DeletePageCommand = new RelayCommand(DeleteCurrentPage, () => scannedPages.Any());
            ExportOcrCommand = new RelayCommand(ExportOcrToWord, () => isOcrCompleted);

            ProceedOcrCommand = new AsyncRelayCommand(
                ExecuteOcrAsync,
                () => ScannedPages.Any() && !IsOcrInProgress
            );

            // afterwards, whenever ScannedPages or IsOcrInProgress changes:
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
            AvailableDevices.Clear(); // Use the generated property instead of the field
            foreach (var d in AppDbContext.GetDevices())
                AvailableDevices.Add(d); // Use the generated property instead of the field
        }

        // ── SCAN ────────────────────────────────────────────────────────────────
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


        // ── SCAN-TAB EXPORT ────────────────────────────────────────────────────
        private void ExecuteExport()
        {
            if (SelectedFileType == "Image")
                ExportAsImages();
            else
                ExportAsPdf();
        }

        // Replace all direct references to the `scannedPages` field with the generated property `ScannedPages`.
        // Example 1: Fixing the loop in ExecuteExport
        private void ExportAsImages()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueTextExports");
            Directory.CreateDirectory(dir);

            for (int i = 0; i < ScannedPages.Count; i++) // Use the generated property
            {
                var path = Path.Combine(dir, $"Scan_Page_{i + 1}_{DateTime.Now:yyyyMMdd_HHmmss}.jpeg");
                using var fs = File.Create(path);
                ScannedPages[i].Save(fs); // Use the generated property
            }
        }

        private void ExportAsPdf()
        {
            if (!ScannedPages.Any()) return; // Use the generated property instead of the field

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueTextExports");
            Directory.CreateDirectory(dir);
            var outPdf = Path.Combine(dir, $"Scan_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            var doc = QuestPDFDocument.Create(c =>
            {
                foreach (var bmp in ScannedPages) // Use the generated property instead of the field
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

            doc.GeneratePdf(outPdf);
        }

        // Example 2: Fixing the loop in ExecuteOcrAsync
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
                string lang = SelectedOcrLanguage switch
                {
                    "Sinhala" => "sin",
                    _ => "eng"
                };
                using var engine = new TesseractEngine(tessData, lang, EngineMode.Default);


                int total = ScannedPages.Count; // Use the generated property
                for (int i = 0; i < total; i++)
                {
                    await Task.Delay(1);

                    using var ms = new MemoryStream();
                    ScannedPages[i].Save(ms); // Use the generated property
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
                            var lines = p
                                .SelectNodes(".//span[@class='ocr_line']")
                                ?.Select(n => CleanText(n.InnerText));
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


        // ── OCR-TAB WORD EXPORT ─────────────────────────────────────────────────
        private void ExportOcrToWord()
        {
            if (string.IsNullOrWhiteSpace(OcrExtractedText)) return;

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueTextExports");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"OCR_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

            using var wordDoc = WordprocessingDocument.Create(file, WordprocessingDocumentType.Document);
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new OpenXmlDocument(new Body());
            var body = mainPart.Document.Body;

            if (body == null) // Ensure `body` is not null
            {
                body = new Body();
                mainPart.Document.AppendChild(body);
            }

            var paras = OcrExtractedText
                .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var p in paras)
            {
                var run = new Run(new Text(p.Trim()) { Space = SpaceProcessingModeValues.Preserve });
                body.AppendChild(new Paragraph(run));
            }}

            

        

        // ── NAV / DELETE ────────────────────────────────────────────────────────
        private void DeleteCurrentPage()
        {
            if (!ScannedPages.Any()) return; // Use the generated property instead of the field
            ScannedPages.RemoveAt(CurrentPageIndex); // Use the generated property instead of the field
            CurrentPageIndex = Math.Clamp(CurrentPageIndex, 0, ScannedPages.Count - 1); 
            UpdateCurrentPage();
            ExportCommand.NotifyCanExecuteChanged();
            DeletePageCommand.NotifyCanExecuteChanged();
            ProceedOcrCommand.NotifyCanExecuteChanged();
        }

        private void NextPage() => CurrentPageIndex++;
        private void PreviousPage() => CurrentPageIndex--;

        // ── in UpdateCurrentPage ─────────────────────────────────────────────
        private void UpdateCurrentPage()
        {
            if (ScannedPages.Count > 0)
            {
                CurrentPage = ScannedPages[CurrentPageIndex];
            }
            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();
        }


        // ── Helpers ─────────────────────────────────────────────────────────────
        private int GetDpi() => SelectedResolution switch { "150 DPI" => 150, "600 DPI" => 600, _ => 300 };
        private int GetIntent() => SelectedColorProfile switch { "Grayscale" => 2, "Black & White" => 4, _ => 1 };

        private (int w, int h) GetPagePixelSize()
        {
            var dpi = GetDpi();
            return SelectedPageSize switch // Use the generated property instead of the field
            {
                "Letter" => ((int)(8.5 * dpi), (int)(11 * dpi)),
                "Legal" => ((int)(8.5 * dpi), (int)(14 * dpi)),
                _ => ((int)(8.27 * dpi), (int)(11.69 * dpi)),
            };
        }

        private void SetScannerProperty(IItem item, string id, object val)
        {
            try { item.Properties[id].set_Value(val); }
            catch {}
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            
            var decoded = WebUtility.HtmlDecode(text);

            
            decoded = Regex.Replace(decoded, @"^[>\*\s]+", "");

            
            decoded = Regex.Replace(decoded, @"\s+", " ");

            
            return decoded.Trim();
        }
    }
}
