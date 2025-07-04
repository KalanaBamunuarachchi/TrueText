﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using TrueText.Data;
using Avalonia.Controls;
using System.Reactive;

namespace TrueText.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private ViewModelBase _currentPage;

        private readonly DashboardPageViewModel _dashboardPageViewModel = new();
        private readonly ScanPageViewModel _scanPageViewModel;
        private readonly DevicesPageViewModel _devicesPageViewModel = new();
        private readonly HelpPageViewModel _helpPageViewModel = new();
        private readonly SettingsPageViewModel _settingsPageViewModel = new();

        public DashboardPageViewModel DashboardPageViewModel => _dashboardPageViewModel;







        public MainWindowViewModel()
        {
            _scanPageViewModel = new ScanPageViewModel();
            _scanPageViewModel.RecentOcrExported += () =>
            {
                _dashboardPageViewModel.LoadRecentScans();
            };

            //ClearAllAppData();


            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboard, () => true);
            NavigateToScanPageCommand = new RelayCommand(NavigateToScanPage, () => true);
            NavigateToDevicesPageCommand = new RelayCommand(NavigateToDevicesPage, () => true);
            NavigateToHelpPageCommand = new RelayCommand(NavigateToHelpPage, () => true);
            NavigateToSettingsPageCommand = new RelayCommand(NavigateToSettingsPage, () => true);

            CurrentPage = _dashboardPageViewModel; 

            Debug.WriteLine($"NavigateToDashboardCommand: {NavigateToDashboardCommand != null}");
            Debug.WriteLine($"NavigateToScanPageCommand: {NavigateToScanPageCommand != null}");
            Debug.WriteLine($"NavigateToDevicesPageCommand: {NavigateToDevicesPageCommand != null}");
            Debug.WriteLine($"NavigateToHelpPageCommand: {NavigateToHelpPageCommand != null}");
            Debug.WriteLine($"NavigateToSettingsPageCommand: {NavigateToSettingsPageCommand != null}");

            





        }

        
        public IRelayCommand NavigateToDashboardCommand { get; private set; } = null!;
        public IRelayCommand NavigateToScanPageCommand { get; private set; } = null!;
        public IRelayCommand NavigateToDevicesPageCommand { get; private set; } = null!;
        public IRelayCommand NavigateToHelpPageCommand { get; private set; } = null!;
        public IRelayCommand NavigateToSettingsPageCommand { get; private set; } = null!;

        public void NavigateToDashboard()
        {
            CurrentPage = _dashboardPageViewModel;
        }

        public void NavigateToScanPage()
        {
            CurrentPage = _scanPageViewModel;
        }

        public void NavigateToDevicesPage()
        {
            CurrentPage = _devicesPageViewModel;
        }

        public void NavigateToHelpPage()
        {
            CurrentPage = _helpPageViewModel;
        }

        public void NavigateToSettingsPage()
        {
            CurrentPage = _settingsPageViewModel;
        }
        /*public void ClearAllAppData()
        {
            using (var db = new AppDbContext())
            {
                db.Devices.RemoveRange(db.Devices);
                db.SaveChanges();
            }

            
        }*/


    }
}
