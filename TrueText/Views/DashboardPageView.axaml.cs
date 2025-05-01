using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using TrueText.ViewModels;

namespace TrueText.Views;

public partial class DashboardPageView : UserControl
{
    public DashboardPageView()
    {
        InitializeComponent();
        DataContext = new DashboardPageViewModel();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }


}