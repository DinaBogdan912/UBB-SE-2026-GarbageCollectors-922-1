using System;
using BankingAppTeamB.Configuration;
using BankingAppTeamB.Data;
using Microsoft.UI.Xaml;

namespace BankingAppTeamB
{
    public partial class App : Application
    {
        private Window? window;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs launchActivatedEventArgs)
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

            window = new MainWindow();
            window.Activate();
        }
    }
}