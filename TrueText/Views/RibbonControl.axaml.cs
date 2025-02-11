using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using TrueText.ViewModels;  // Add this line


namespace TrueText;

public partial class RibbonControl : UserControl
{
    public RibbonControl()
    {
        InitializeComponent();
        DataContext = new RibbonControlViewModel(); // Set the ViewModel as DataContext
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
