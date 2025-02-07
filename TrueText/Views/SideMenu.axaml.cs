using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Diagnostics;

namespace TrueText;

public partial class SideMenu : UserControl
{
    public SideMenu()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (_, __) =>
        {
            Debug.WriteLine($"SideMenu DataContext: {DataContext?.GetType().Name}");
        };
    }
}