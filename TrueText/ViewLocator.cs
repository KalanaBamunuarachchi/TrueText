using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using TrueText.ViewModels;

namespace TrueText
{
    public class ViewLocator : IDataTemplate
    {

        public Control? Build(object? data)
        {
            if (data is null)
                return new ();

            var viewName = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.InvariantCulture);
            var type = Type.GetType(viewName);

            if (type != null)
            {
                var control = (Control)Activator.CreateInstance(type)!;
                control.DataContext = data;
                return control;
            }

            return new TextBlock { Text = "Not Found: " + viewName };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
