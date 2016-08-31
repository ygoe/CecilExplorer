using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides a converter that safely accesses the error message of a ValidationError collection.
	/// </summary>
	/// <example>
	/// <code lang="XAML"><![CDATA[
	/// <Setter
	///     Property="ToolTip"
	///     Value="{Binding
	///         RelativeSource={RelativeSource Self},
	///         Path=(Validation.Errors),
	///         Converter={StaticResource ValidationErrorConverter}}"/>
	/// ]]></code>
	/// </example>
	public class ValidationErrorConverter : IValueConverter
	{
		/// <summary>
		/// Returns the error message of the first ValidationError item in the collection, if any;
		/// otherwise, an empty string.
		/// </summary>
		/// <param name="value">The ValidationError collection.</param>
		/// <param name="targetType">Unused.</param>
		/// <param name="parameter">Unused.</param>
		/// <param name="culture">Unused.</param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			ReadOnlyObservableCollection<ValidationError> errors = value as ReadOnlyObservableCollection<ValidationError>;
			if (errors == null)
			{
				return value;
			}
			if (errors.Count > 0)
			{
				return errors[0].ErrorContent;
			}
			return "";
		}

		/// <summary>
		/// Not implemented.
		/// </summary>
		/// <param name="value">Unused.</param>
		/// <param name="targetType">Unused.</param>
		/// <param name="parameter">Unused.</param>
		/// <param name="culture">Unused.</param>
		/// <returns>Never returns a value.</returns>
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException("This method should never be called");
		}
	}
}
