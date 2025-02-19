using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using TrueText.ViewModels;
using WIA;

namespace TrueText.Views;

public partial class DevicesPageView : UserControl
{
    

    public DevicesPageView()
    {
        InitializeComponent();
        DataContext = new DevicesPageViewModel();


    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }



}