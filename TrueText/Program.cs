using System;
using Avalonia;
using Avalonia.ReactiveUI;
using TrueText.Data;
using QuestPDF.Infrastructure;

namespace TrueText
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            // Initialize Database Here
            using (var db = new AppDbContext())
            {
               
                db.Database.EnsureCreated(); 
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();


    }
}
