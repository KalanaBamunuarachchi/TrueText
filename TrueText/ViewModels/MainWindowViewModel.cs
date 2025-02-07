using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace TrueText.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private ViewModelBase _currentPage;

        private readonly DashboardPageViewModel _dashboardPageViewModel = new();
        private readonly ScanPageViewModel _scanPageViewModel = new();
        private readonly DevicesPageViewModel _devicesPageViewModel = new();
        private readonly HelpPageViewModel _helpPageViewModel = new();
        private readonly SettingsPageViewModel _settingsPageViewModel = new();

        

        public MainWindowViewModel()
        {
            CurrentPage = _dashboardPageViewModel;
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboard);
            NavigateToScanPageCommand = new RelayCommand(NavigateToScanPage);
            NavigateToDevicesPageCommand = new RelayCommand(NavigateToDevicesPage);
            NavigateToHelpPageCommand = new RelayCommand(NavigateToHelpPage);
            NavigateToSettingsPageCommand = new RelayCommand(NavigateToSettingsPage);

            Debug.WriteLine($"NavigateToDashboardCommand: {NavigateToDashboardCommand != null}");
            Debug.WriteLine($"NavigateToScanPageCommand: {NavigateToScanPageCommand != null}");
            Debug.WriteLine($"NavigateToDevicesPageCommand: {NavigateToDevicesPageCommand != null}");
            Debug.WriteLine($"NavigateToHelpPageCommand: {NavigateToHelpPageCommand != null}");
            Debug.WriteLine($"NavigateToSettingsPageCommand: {NavigateToSettingsPageCommand != null}");


        }

        public IRelayCommand NavigateToDashboardCommand { get; }
        public IRelayCommand NavigateToScanPageCommand { get; }

        public IRelayCommand NavigateToDevicesPageCommand { get; }

        public IRelayCommand NavigateToHelpPageCommand { get; }

        public IRelayCommand NavigateToSettingsPageCommand { get; }

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

    }
}
