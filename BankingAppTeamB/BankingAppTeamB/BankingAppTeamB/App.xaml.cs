using Microsoft.UI.Xaml;
using BankingAppTeamB.Data;
using BankingAppTeamB.Configuration;
using System;

namespace BankingAppTeamB
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                DatabaseInitializer.Initialize();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB error: {ex.Message}");
                throw new InvalidOperationException($"Database initialization failed: {ex.Message}", ex);
            }

            try
            {
                ServiceLocator.Initialize();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServiceLocator error: {ex.Message}");
                throw new InvalidOperationException($"ServiceLocator initialization failed: {ex.Message}", ex); 
            }

            _window = new MainWindow();
            _window.Activate();
        }
    }
}