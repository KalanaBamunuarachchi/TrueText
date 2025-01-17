namespace TrueText.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
#pragma warning disable CA1822 // Mark members as static
        public string Dashboard => "Dashboard";
#pragma warning restore CA1822 // Mark members as static

        private ViewModelBase _currentPage;

        // Correctly use DashboardViewModel here
        private readonly DashboardViewModel _dashboard = new DashboardViewModel();

        private readonly ScanPageViewModel _scan = new ScanPageViewModel();

        private readonly DevicePageViewModel _devices = new DevicePageViewModel();

        public MainWindowViewModel() {
            
        }
    }
}
