using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SecondFolderBackup.Resources
{
	/// <summary>
	/// Implements a modern flat UI style that works consistently across all Windows platforms.
	/// </summary>
	partial class FlatStyle : ResourceDictionary
	{
		/// <summary>
		/// Initialises a new instance of the <see cref="FlatStyle"/> class.
		/// </summary>
		public FlatStyle()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Positions the ComboBox popup so that the selected element is directly over the ComboBox
		/// control and the popup extends to the top and bottom of the control rectangle.
		/// </summary>
		/// <param name="sender">The Popup instance.</param>
		/// <param name="args">Unused.</param>
		private void ComboBox_Popup_Opened(object sender, EventArgs args)
		{
			// WARNING: This method is unfinished! See NOTE comments below.

			Popup p = sender as Popup;
			if (p != null)
			{
				ComboBox cmb = p.TemplatedParent as ComboBox;
				if (cmb != null)
				{
					object o = p.FindName("PopupItems");
					StackPanel popupItems = o as StackPanel;
					if (popupItems != null &&
						cmb.SelectedIndex >= 0 &&
						popupItems.Children[cmb.SelectedIndex] is ComboBoxItem)
					{
						ComboBoxItem cbi = popupItems.Children[cmb.SelectedIndex] as ComboBoxItem;
						double itemHeight = cbi.ActualHeight;
						double desiredOffset = -itemHeight * (cmb.SelectedIndex + 1) - 2;
						p.VerticalOffset = desiredOffset;
						// NOTE: Fails when the popup is moved to fit on the screen
						// NOTE: Fails when the popup list has a scrollbar

						// Need the following call to somehow update the visuals immediately
						object child = System.Windows.Media.VisualTreeHelper.GetChild(cbi, 0);

						// Eat the first mouse release event. A list item is moved directly under
						// the mouse cursor and it doesn't check whether the mouse has been
						// pressed on it before reacting on the release event and closing the popup.
						cbi.PreviewMouseUp += cbi_PreviewMouseUp;

						// Ensure the event eater disappears when the popup is closed for a different reason
						p.Closed += delegate (object sender2, EventArgs args2)
						{
							cbi.PreviewMouseUp -= cbi_PreviewMouseUp;
						};
					}
				}
			}
		}

		private void cbi_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs args)
		{
			ComboBoxItem cbi = sender as ComboBoxItem;
			if (cbi != null)
			{
				args.Handled = true;
				cbi.PreviewMouseUp -= cbi_PreviewMouseUp;
			}
		}
	}
}
