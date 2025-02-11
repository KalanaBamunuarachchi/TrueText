using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace TrueText;

public partial class RibbonControl : UserControl
{
    private TextBlock? _greetingTextBlock;

    public RibbonControl()
    {
        InitializeComponent();
        _greetingTextBlock = this.FindControl<TextBlock>("GreetingText");
        if (_greetingTextBlock != null)
        {
            _greetingTextBlock.Text = GetGreetingMessage();
        }
    }

    private string GetGreetingMessage()
    {
        var hour = DateTime.Now.Hour;
        if (hour >= 5 && hour < 12) return "Good Morning";
        if (hour >= 12 && hour < 17) return "Good Afternoon";
        if (hour >= 17 && hour < 21) return "Good Evening";
        return "Good Night";
    }
}