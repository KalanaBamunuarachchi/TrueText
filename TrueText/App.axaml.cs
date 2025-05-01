using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using TrueText.ViewModels;
using TrueText.Views;
namespace TrueText
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            DataTemplates.Add(new ViewLocator());

        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var splashWindow = new SplashScreen();
                splashWindow.Show();
                desktop.MainWindow = splashWindow;

                try
                {
                    await Task.Delay(3000);
                }
                catch
                {
                    splashWindow.Close();
                    return;
                }

                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel() // Use parameterless constructor
                };

                


                mainWindow.Show();
                splashWindow.Close();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}