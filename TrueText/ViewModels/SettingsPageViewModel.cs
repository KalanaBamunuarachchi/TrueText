using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using Avalonia.Styling;
namespace TrueText.ViewModels
{
    public partial class SettingsPageViewModel : ViewModelBase
    {
        
        public ObservableCollection<string> ThemeOptions { get; } = new()
        {
            "Light",
            "Dark"
        };

        
        [ObservableProperty]
        private string selectedTheme = "Light";

        
        partial void OnSelectedThemeChanged(string value)
        {
            if (Application.Current == null)
                return;

            
            Application.Current.RequestedThemeVariant = value switch
            {
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Light
            };

            
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                foreach (var window in desktop.Windows)
                {
                    window.RequestedThemeVariant = Application.Current.RequestedThemeVariant;
                }
            }
        }
    }
}
