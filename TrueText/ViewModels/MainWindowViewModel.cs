using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TrueText.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private ViewModelBase _currentPage;

        private readonly DashboardPageViewModel _dashboardPageViewModel = new();
        private readonly ScanPageViewModel _scanPageViewModel = new();

        public MainWindowViewModel()
        {
            CurrentPage = _dashboardPageViewModel;
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboard);
            NavigateToScanPageCommand = new RelayCommand(NavigateToScanPage);
        }

        public IRelayCommand NavigateToDashboardCommand { get; }
        public IRelayCommand NavigateToScanPageCommand { get; }

        public void NavigateToDashboard()
        {
            CurrentPage = _dashboardPageViewModel;
        }

        public void NavigateToScanPage()
        {
            CurrentPage = _scanPageViewModel;
        }
    }
}
