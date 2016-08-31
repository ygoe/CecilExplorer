using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Unclassified.UI
{
	/// <summary>
	/// Shows a ToolTip over a TextBlock when its text is trimmed.
	/// </summary>
	public class TextBlockAutoToolTip
	{
		/// <summary>
		/// The Enabled attached property.
		/// </summary>
		public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
			"Enabled",
			typeof(bool),
			typeof(TextBlockAutoToolTip),
			new FrameworkPropertyMetadata(new PropertyChangedCallback(OnAutoToolTipEnabledChanged)));

		/// <summary>
		/// Sets the Enabled attached property on a TextBlock control.
		/// </summary>
		/// <param name="dependencyObject">The TextBlock control.</param>
		/// <param name="enabled">The value.</param>
		public static void SetEnabled(DependencyObject dependencyObject, bool enabled)
		{
			dependencyObject.SetValue(EnabledProperty, enabled);
		}

		private static readonly TrimmedTextBlockVisibilityConverter ttbvc = new TrimmedTextBlockVisibilityConverter();

		private static void OnAutoToolTipEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			TextBlock textBlock = dependencyObject as TextBlock;
			if (textBlock != null)
			{
				bool enabled = (bool)args.NewValue;
				if (enabled)
				{
					var toolTip = new ToolTip
					{
						Placement = System.Windows.Controls.Primitives.PlacementMode.Relative,
						VerticalOffset = -3,
						HorizontalOffset = -5,
						Padding = new Thickness(4, 2, 4, 2),
						Background = Brushes.White
					};
					toolTip.SetBinding(UIElement.VisibilityProperty, new System.Windows.Data.Binding
					{
						RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.Self),
						Path = new PropertyPath("PlacementTarget"),
						Converter = ttbvc
					});
					toolTip.SetBinding(ContentControl.ContentProperty, new System.Windows.Data.Binding
					{
						RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.Self),
						Path = new PropertyPath("PlacementTarget.Text")
					});
					toolTip.SetBinding(Control.ForegroundProperty, new System.Windows.Data.Binding
					{
						RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.Self),
						Path = new PropertyPath("PlacementTarget.Foreground")
					});
					textBlock.ToolTip = toolTip;
					textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
				}
			}
		}

		private class TrimmedTextBlockVisibilityConverter : IValueConverter
		{
			// Source 1: http://stackoverflow.com/a/21863054
			// Source 2: http://stackoverflow.com/a/25436070

			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				var textBlock = value as TextBlock;
				if (textBlock == null)
					return Visibility.Collapsed;

				Typeface typeface = new Typeface(
					textBlock.FontFamily,
					textBlock.FontStyle,
					textBlock.FontWeight,
					textBlock.FontStretch);

				// FormattedText is used to measure the whole width of the text held up by TextBlock container
				FormattedText formattedText = new FormattedText(
					textBlock.Text,
					System.Threading.Thread.CurrentThread.CurrentCulture,
					textBlock.FlowDirection,
					typeface,
					textBlock.FontSize,
					textBlock.Foreground);

				formattedText.MaxTextWidth = textBlock.ActualWidth;

				// When the maximum text width of the FormattedText instance is set to the actual
				// width of the textBlock, if the textBlock is being trimmed to fit then the formatted
				// text will report a larger height than the textBlock. Should work whether the
				// textBlock is single or multi-line.
				// The width check detects if any single line is too long to fit within the text area,
				// this can only happen if there is a long span of text with no spaces.
				bool isTrimmed = formattedText.Height > textBlock.ActualHeight ||
					formattedText.MinWidth > formattedText.MaxTextWidth;

				return isTrimmed ? Visibility.Visible : Visibility.Collapsed;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				throw new NotImplementedException();
			}
		}
	}
}
