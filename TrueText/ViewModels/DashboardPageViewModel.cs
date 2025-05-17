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




        public void LoadRecentScans()
        {
            RecentScans.Clear();

            var logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TrueText", "recent_exports.txt");
            if (!File.Exists(logFile)) return;

            var entries = File.ReadAllLines(logFile)
                .Select(line => line.Split('|'))
                .Where(parts => parts.Length == 2 && File.Exists(parts[1]))
                .Select(parts => new
                {
                    Time = DateTime.Parse(parts[0]),
                    Path = parts[1]
                })
                .OrderByDescending(e => e.Time)
                .Take(5);

            foreach (var entry in entries)
            {
                RecentScans.Add(new RecentScan
                {
                    Name = Path.GetFileNameWithoutExtension(entry.Path),
                    Type = Path.GetExtension(entry.Path).TrimStart('.').ToUpperInvariant(),
                    FilePath = entry.Path
                });
            }
        }


        private void ShowInExplorer(RecentScan? scan)
        {
            if (scan == null) return;
            
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{scan.FilePath}\"",
                UseShellExecute = true
            });
        }

    }
}
