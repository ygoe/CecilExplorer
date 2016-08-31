using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using Unclassified.UI;

namespace CecilExplorer.Views
{
	public partial class AboutDialog : Window
	{
		public AboutDialog()
		{
			InitializeComponent();

			this.HideIcon();

			versionText.Text = AssemblyInfo.Version;
			copyrightText.Text = AssemblyInfo.Copyright;
			cecilVersionText.Text = AssemblyInfo.CecilVersion;
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs args)
		{
			var hyperlink = (Hyperlink)sender;
			var run = (Run)hyperlink.Inlines.FirstInline;
			string url = run.Text;
			if (url.Contains("unclassified.software"))
			{
				url += "?ref=inapp-cecilexplorer";
			}
			Process.Start(url);
		}

		private void CloseButton_Click(object sender, RoutedEventArgs args)
		{
			Close();
		}
	}
}
