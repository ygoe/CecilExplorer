using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Unclassified.UI;

namespace CecilExplorer.Views
{
	public partial class FindDialog : Window
	{
		public FindDialog()
		{
			InitializeComponent();

			this.HideIcon();
		}

		public string SearchText
		{
			get
			{
				return searchText.Text;
			}
			set
			{
				searchText.Text = value;
			}
		}

		public bool CaseSensitive
		{
			get
			{
				return caseSensitive.IsChecked == true;
			}
			set
			{
				caseSensitive.IsChecked = value;
			}
		}

		public bool ExpandNodes
		{
			get
			{
				return expandNodes.IsChecked == true;
			}
			set
			{
				expandNodes.IsChecked = value;
			}
		}

		public bool DirectionForward { get; set; }

		private void Window_Loaded(object sender, RoutedEventArgs args)
		{
			searchText.SelectAll();
			searchText.Focus();
		}

		private void Window_KeyDown(object sender, KeyEventArgs args)
		{
			if (args.Key == Key.F3 && args.KeyboardDevice.Modifiers == ModifierKeys.None && !string.IsNullOrEmpty(SearchText))
			{
				FindNextButton_Click(null, null);
				args.Handled = true;
			}
			if (args.Key == Key.F3 && args.KeyboardDevice.Modifiers == ModifierKeys.Shift && !string.IsNullOrEmpty(SearchText))
			{
				FindPreviousButton_Click(null, null);
				args.Handled = true;
			}
		}

		private void FindPreviousButton_Click(object sender, RoutedEventArgs args)
		{
			DirectionForward = false;
			DialogResult = true;
			Close();
		}

		private void FindNextButton_Click(object sender, RoutedEventArgs args)
		{
			DirectionForward = true;
			DialogResult = true;
			Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs args)
		{
			Close();
		}
	}
}
