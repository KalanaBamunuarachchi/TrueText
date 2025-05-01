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
        // Theme options for ComboBox
        public ObservableCollection<string> ThemeOptions { get; } = new()
        {
            "Light",
            "Dark"
        };

        // Selected theme
        [ObservableProperty]
        private string selectedTheme = "Light";

        // This runs automatically when SelectedTheme changes (CommunityToolkit MVVM)
        partial void OnSelectedThemeChanged(string value)
        {
            if (Application.Current == null)
                return;

            // Update global app theme
            Application.Current.RequestedThemeVariant = value switch
            {
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Light
            };

            // Force refresh all windows to apply new theme
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
