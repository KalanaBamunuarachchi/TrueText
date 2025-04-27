using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace TrueText.ViewModels;

public class RibbonControlViewModel : INotifyPropertyChanged
{
    private string _greeting;
    public string Greeting
    {
        get => _greeting;
        set
        {
            _greeting = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Greeting)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public RibbonControlViewModel()
    {
        Greeting = GetGreetingMessage();
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