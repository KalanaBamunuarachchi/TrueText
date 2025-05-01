using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;



namespace TrueText.ViewModels
{
    public partial class HelpPageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<string> feedbackCategories = new()
        {
            "Report Bug",
            "Feature Request"
            
        };

        [ObservableProperty]
        private string selectedCategory = "Report bug";

        [ObservableProperty]
        private string feedbackMessage = string.Empty;

        public IAsyncRelayCommand SubmitFeedbackCommand { get; }

        public HelpPageViewModel()
        {
            SubmitFeedbackCommand = new AsyncRelayCommand(SubmitFeedbackAsync);
        }

        private async Task SubmitFeedbackAsync()
        {
            var formUrl = "https://docs.google.com/forms/u/0/d/e/1FAIpQLSd7t60n6LVUm4vfJYAsAPnycsJHPz-CKkJy7N4_WKhZUXsisw/formResponse";

            var data = new Dictionary<string, string>
            {
                { "entry.2033041250", SelectedCategory },
                { "entry.75330526", FeedbackMessage }
            };

            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(data);
            await client.PostAsync(formUrl, content);

            // Optional: Clear after submit
            FeedbackMessage = string.Empty;
        }
    }
}
