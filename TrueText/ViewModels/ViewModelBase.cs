using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using System.ComponentModel;

namespace TrueText.ViewModels
{
    

public class ViewModelBase : ObservableObject
    {
        // Use the 'new' keyword to explicitly hide the inherited member
        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
