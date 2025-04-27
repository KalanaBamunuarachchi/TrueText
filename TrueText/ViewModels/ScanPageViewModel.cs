using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Tesseract;
using WIA;
using TrueText.Data;
using TrueText.Models;
using MyDevice = TrueText.Models.Device;
using DocumentFormat.OpenXml;
using QuestPDFDocument = QuestPDF.Fluent.Document;
using OpenXmlDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using QuestPDF.Helpers;
using System.Threading;
using System.Threading.Tasks;


namespace TrueText.ViewModels
{
    public partial class ScanPageViewModel : ViewModelBase
    {
        //  Pick-lists & selection
        [ObservableProperty] private ObservableCollection<MyDevice> availableDevices = new();
        [ObservableProperty] private MyDevice? selectedDevice; 

        public ObservableCollection<string> ColorProfiles { get; } = new() { "Color", "Grayscale", "Black & White" };
        public ObservableCollection<string> Resolutions { get; } = new() { "150 DPI", "300 DPI", "600 DPI" };
        public ObservableCollection<string> AvailablePageSizes { get; } = new() { "A4", "Letter", "Legal" };
        public ObservableCollection<string> FileTypes { get; } = new() { "Image", "Document" };

        [ObservableProperty] private string selectedColorProfile = "Color";
        [ObservableProperty] private string selectedResolution = "300 DPI";
        [ObservableProperty] private string selectedPageSize = "A4";
        public string SelectedFileType { get; set; } = "Image";

        //  Scanned pages state 
        [ObservableProperty] private ObservableCollection<Bitmap> scannedPages = new();
        [ObservableProperty] private int currentPageIndex = 0;
        [ObservableProperty] private Bitmap? currentPage = null;

        //  OCR state 
        [ObservableProperty] private string? ocrExtractedText = null;
        [ObservableProperty] private bool isOcrCompleted = false;

        //progress
        [ObservableProperty] private double ocrProgress;
        [ObservableProperty] private bool isOcrInProgress;

        // Commands
        public IRelayCommand ScanCommand { get; }
        public IRelayCommand ExportCommand { get; }
        public IRelayCommand NextPageCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }
        public IRelayCommand DeletePageCommand { get; }
        public IRelayCommand ProceedOcrCommand { get; }
        public IRelayCommand ExportOcrCommand { get; }
        

        public ScanPageViewModel()
        {
            LoadDevicesFromDatabase();

            ScanCommand = new RelayCommand(ExecuteScan);
            ExportCommand = new RelayCommand(ExecuteExport, () => scannedPages.Any());
            NextPageCommand = new RelayCommand(NextPage, () => currentPageIndex < scannedPages.Count - 1);
            PreviousPageCommand = new RelayCommand(PreviousPage, () => currentPageIndex > 0);
            DeletePageCommand = new RelayCommand(DeleteCurrentPage, () => scannedPages.Any());
            ProceedOcrCommand = new RelayCommand(ExecuteOcr, () => scannedPages.Any() && !isOcrCompleted);
            ExportOcrCommand = new RelayCommand(ExportOcrToWord, () => isOcrCompleted);

         
        }

        partial void OnCurrentPageIndexChanged(int value) => UpdateCurrentPage();

        private void LoadDevicesFromDatabase()
        {
            AvailableDevices.Clear(); 
            foreach (var d in AppDbContext.GetDevices())
                AvailableDevices.Add(d); 
        }

        // Scan
        private void ExecuteScan()
        {
            if (SelectedDevice is null) return; 
            var di = new DeviceManager().DeviceInfos
                      .Cast<DeviceInfo>()
                      .FirstOrDefault(x => x.Properties["Name"].get_Value()?.ToString() == SelectedDevice.Name); 
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

        // SCAN TAB EXPORT 
        private void ExecuteExport()
        {
            if (SelectedFileType == "Image") ExportAsImages();
            else ExportAsPdf();
        }

        private void ExportAsImages()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueTextExports");
            Directory.CreateDirectory(dir);

            for (int i = 0; i < ScannedPages.Count; i++) 
            {
                var path = Path.Combine(dir, $"Scan_Page_{i + 1}_{DateTime.Now:yyyyMMdd_HHmmss}.jpeg");
                using var fs = File.Create(path);
                ScannedPages[i].Save(fs); 
            }
        }

        // PDF EXPORT
        private void ExportAsPdf()
        {
            if (!ScannedPages.Any()) return; 
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueTextExports");
            Directory.CreateDirectory(dir);
            var outPdf = Path.Combine(dir, $"Scan_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

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

            doc.GeneratePdf(outPdf);
        }

        // OCR
        private void ExecuteOcr()
        {
            var tessData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            var sb = new StringBuilder();

            using var engine = new TesseractEngine(tessData, "eng", EngineMode.Default);
            foreach (var bmp in ScannedPages) 
            {
                using var ms = new MemoryStream();
                bmp.Save(ms);
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
                        
                        var lines = p.SelectNodes(".//span[@class='ocr_line']")
                                     ?.Select(n => n.InnerText.Trim());
                        if (lines != null)
                        {
                            sb.AppendLine(string.Join(" ", lines));
                            sb.AppendLine(); 
                        }
                    }
                }
                else
                {
                    
                    var txt = page.GetText().Trim();
                    if (!string.IsNullOrWhiteSpace(txt))
                    {
                        sb.AppendLine(txt);
                        sb.AppendLine();
                    }
                }
            }

            OcrExtractedText = sb.ToString().TrimEnd();
            IsOcrCompleted = true; 
            ProceedOcrCommand.NotifyCanExecuteChanged();
            ExportOcrCommand.NotifyCanExecuteChanged();
        }

        // ── OCR-TAB EXPORT
        // ── OCR-TAB WORD EXPORT 
        private void ExportOcrToWord()
        {
            if (string.IsNullOrWhiteSpace(OcrExtractedText)) return;

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueTextExports");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"OCR_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

            using var wordDoc = WordprocessingDocument.Create(file, WordprocessingDocumentType.Document);
            var mainPart = wordDoc.AddMainDocumentPart();
            // **use our alias here**
            mainPart.Document = new OpenXmlDocument(new Body());
            var body = mainPart.Document.Body;

            var paras = OcrExtractedText
                .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var p in paras)
            {
                var run = new Run(new Text(p.Trim()) { Space = SpaceProcessingModeValues.Preserve });
                body.Append(new Paragraph(run));
            }
        }

        // NAV & DELETE 
        private void DeleteCurrentPage()
        {
            if (!ScannedPages.Any()) return; 
            ScannedPages.RemoveAt(CurrentPageIndex); 
            CurrentPageIndex = Math.Clamp(CurrentPageIndex, 0, ScannedPages.Count - 1); 
            UpdateCurrentPage();
            ExportCommand.NotifyCanExecuteChanged();
            DeletePageCommand.NotifyCanExecuteChanged();
            ProceedOcrCommand.NotifyCanExecuteChanged();
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
            try { item.Properties[id].set_Value(val); }
            catch {  }
        }
    }
}
