using System;
using Unclassified.Util;

namespace CecilExplorer
{
	public interface IAppSettings : ISettings
	{
		/// <summary>
		/// Provides settings for the main window state.
		/// </summary>
		IWindowStateSettings MainWindowState { get; }

		/// <summary>
		/// Gets or sets the name of the last opened assembly file.
		/// </summary>
		string LastFileName { get; set; }

		/// <summary>
		/// Gets or sets the grid view column widths.
		/// </summary>
		int[] ColumnWidths { get; set; }
	}
}
