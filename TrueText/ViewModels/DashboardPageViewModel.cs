using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrueText.Data;
using TrueText.Models;


namespace TrueText.ViewModels
{
    public class DashboardPageViewModel : ViewModelBase
    {
        public ObservableCollection<Device> Devices { get; } = new();

        public ObservableCollection<RecentScan> RecentScans { get; } = new();

        // command to open the folder where the file lives
        public IRelayCommand<RecentScan> OpenFileLocationCommand { get; }

        private readonly string _exportDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueTextExports");

        public DashboardPageViewModel()
        {
            LoadDevices();
            OpenFileLocationCommand = new RelayCommand<RecentScan>(ShowInExplorer);
            LoadRecentScans();
        }

        private void LoadDevices()
        {
            using var db = new AppDbContext();
            foreach (var dev in db.Devices.OrderBy(d => d.Name))
                Devices.Add(dev);
        }


       

        private void LoadRecentScans()
        {
            RecentScans.Clear();

            if (!Directory.Exists(_exportDir))
                return;

            // pick the 5 most-recently modified files
            var files = Directory
                .GetFiles(_exportDir)
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .Take(5);

            foreach (var file in files)
            {
                RecentScans.Add(new RecentScan
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Type = Path.GetExtension(file).TrimStart('.').ToUpperInvariant(),
                    FilePath = file
                });
            }
        }

        private void ShowInExplorer(RecentScan? scan)
        {
            if (scan == null) return;
            // open Windows Explorer at the file’s folder, select the file
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{scan.FilePath}\"",
                UseShellExecute = true
            });
        }

    }
}
