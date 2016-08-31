using System;
using System.Windows;
using CecilExplorer.ViewModels;
using CecilExplorer.Views;
using Unclassified.Util;

namespace CecilExplorer
{
	public partial class App : Application
	{
		#region Startup

		protected override void OnStartup(StartupEventArgs args)
		{
			base.OnStartup(args);

			// Create main window and view model
			var view = new MainWindow();
			var viewModel = new MainViewModel();
			view.DataContext = viewModel;

			// Show the main window
			view.Show();
		}

		#endregion Startup

		#region Settings

		/// <summary>
		/// Provides properties to access the application settings.
		/// </summary>
		public static IAppSettings Settings { get; private set; }

		/// <summary>
		/// Initialises the application settings.
		/// </summary>
		public static void InitializeSettings()
		{
			if (Settings != null) return;   // Already done

			Settings = SettingsAdapterFactory.New<IAppSettings>(
				new FileSettingsStore(
					SettingsHelper.GetAppDataPath(@"Unclassified\CecilExplorer", "CecilExplorer.conf")));
		}

		#endregion Settings
	}
}
