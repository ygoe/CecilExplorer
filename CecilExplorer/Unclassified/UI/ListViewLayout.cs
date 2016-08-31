// Source: http://www.codeproject.com/Articles/25058/ListView-Layout-Manager
// License: CPOL <http://www.codeproject.com/info/cpol10.aspx>
// Copyright © 2008-2012 Itenso GmbH, Switzerland
// Modified and improved by Yves Goergen

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Unclassified.UI
{
	#region ListViewLayoutManager class

	[Obfuscation(Exclude = true, Feature = "renaming", ApplyToMembers = false)]
	public class ListViewLayoutManager
	{
		#region Constants

		private const double zeroWidthRange = 0.1;

		#endregion Constants

		#region Private fields

		private readonly ListView listView;
		private INotifyCollectionChanged prevListViewItemsSource;
		private ScrollViewer scrollViewer;
		private bool loaded;
		private bool resizing;
		private Cursor resizeCursor;
		private ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Auto;
		private GridViewColumn autoSizedColumn;

		#endregion Private fields

		#region Attached properties

		public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
			"Enabled",
			typeof(bool),
			typeof(ListViewLayoutManager),
			new FrameworkPropertyMetadata(new PropertyChangedCallback(OnLayoutManagerEnabledChanged)));

		#endregion Attached properties

		#region Constructors

		public ListViewLayoutManager(ListView listView)
		{
			if (listView == null)
			{
				throw new ArgumentNullException("listView");
			}

			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(listView)) return;   // Do nothing in design mode (doesn't work anyway)

			this.listView = listView;
			this.listView.Loaded += new RoutedEventHandler(ListViewLoaded);
			this.listView.Unloaded += new RoutedEventHandler(ListViewUnloaded);

			DependencyPropertyDescriptor dpd =
				DependencyPropertyDescriptor.FromProperty(ListView.ItemsSourceProperty, typeof(ListView));
			dpd.AddValueChanged(this.listView, ListViewItemsSourceChanged);
			ListViewItemsSourceChanged(null, null);
		}

		#endregion Constructors

		#region Public properties

		public ListView ListView
		{
			get { return listView; }
		}

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get { return verticalScrollBarVisibility; }
			set { verticalScrollBarVisibility = value; }
		}

		#endregion Public properties

		#region Public methods

		public static void SetEnabled(DependencyObject dependencyObject, bool enabled)
		{
			dependencyObject.SetValue(EnabledProperty, enabled);
		}

		public void Refresh()
		{
			InitColumns();
			DoResizeColumns();
		}

		#endregion Public methods

		private void ListViewItemsSourceChanged(object sender, EventArgs args)
		{
			if (prevListViewItemsSource != null)
			{
				prevListViewItemsSource.CollectionChanged -= source_CollectionChanged;
			}
			var source = listView.ItemsSource as INotifyCollectionChanged;
			prevListViewItemsSource = source;
			if (source != null)
			{
				// Monitor property changes in all current list items
				var list = source as IEnumerable;
				if (list != null)
				{
					foreach (object o in list)
					{
						var item = o as INotifyPropertyChanged;
						if (item != null)
						{
							item.PropertyChanged += item_PropertyChanged;
						}
					}
				}
				// Monitor property changes in all future changes to list items
				source.CollectionChanged += source_CollectionChanged;
			}
		}

		private void source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			ResetColumnWidths();

			switch (args.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (object o in args.NewItems)
					{
						var item = o as INotifyPropertyChanged;
						if (item != null)
						{
							item.PropertyChanged += item_PropertyChanged;
						}
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					foreach (object o in args.OldItems)
					{
						var item = o as INotifyPropertyChanged;
						if (item != null)
						{
							item.PropertyChanged -= item_PropertyChanged;
						}
					}
					break;
				case NotifyCollectionChangedAction.Replace:
					foreach (object o in args.OldItems)
					{
						var item = o as INotifyPropertyChanged;
						if (item != null)
						{
							item.PropertyChanged -= item_PropertyChanged;
						}
					}
					foreach (object o in args.NewItems)
					{
						var item = o as INotifyPropertyChanged;
						if (item != null)
						{
							item.PropertyChanged += item_PropertyChanged;
						}
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					var list = sender as IEnumerable;
					if (list != null)
					{
						foreach (object o in list)
						{
							var item = o as INotifyPropertyChanged;
							if (item != null)
							{
								item.PropertyChanged += item_PropertyChanged;
							}
						}
					}
					break;
			}
		}

		private void item_PropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			ResetColumnWidths();
		}

		private bool resetColumnWidthsPending = false;

		private void ResetColumnWidths()
		{
			if (!resetColumnWidthsPending)
			{
				resetColumnWidthsPending = true;
				Dispatcher.CurrentDispatcher.BeginInvoke(
					(Action) (() =>
					{
						resetColumnWidthsPending = false;
						GridView view = listView.View as GridView;
						if (view != null)
						{
							listView.UpdateLayout();   // Prevent flickering on expanding columns (not collapsing though)
							foreach (GridViewColumn gridViewColumn in view.Columns)
							{
								bool? isFillColumn = RangeColumn.GetRangeIsFillColumn(gridViewColumn);
								if (!isFillColumn.HasValue || !isFillColumn.Value)
								{
									if (double.IsNaN(gridViewColumn.Width))
									{
										gridViewColumn.Width = gridViewColumn.ActualWidth;
										gridViewColumn.Width = double.NaN;
									}
								}
							}
						}
						Dispatcher.CurrentDispatcher.BeginInvoke((Action) Refresh, DispatcherPriority.Render);
					}),
					DispatcherPriority.Render);
			}
		}

		private void RegisterEvents(DependencyObject start)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(start); i++)
			{
				Visual childVisual = VisualTreeHelper.GetChild(start, i) as Visual;
				if (childVisual is Thumb)
				{
					GridViewColumn gridViewColumn = FindParentColumn(childVisual);
					if (gridViewColumn != null)
					{
						Thumb thumb = childVisual as Thumb;
						if (ProportionalColumn.IsProportionalColumn(gridViewColumn) ||
							FixedColumn.IsFixedColumn(gridViewColumn) || IsFillColumn(gridViewColumn))
						{
							thumb.IsHitTestVisible = false;
						}
						else
						{
							thumb.PreviewMouseMove += new MouseEventHandler(ThumbPreviewMouseMove);
							thumb.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ThumbPreviewMouseLeftButtonDown);
							DependencyPropertyDescriptor.FromProperty(
								GridViewColumn.WidthProperty,
								typeof(GridViewColumn)).AddValueChanged(gridViewColumn, GridColumnWidthChanged);
						}
					}
				}
				else if (childVisual is GridViewColumnHeader)
				{
					GridViewColumnHeader columnHeader = childVisual as GridViewColumnHeader;
					columnHeader.SizeChanged += new SizeChangedEventHandler(GridColumnHeaderSizeChanged);
				}
				else if (scrollViewer == null && childVisual is ScrollViewer)
				{
					scrollViewer = childVisual as ScrollViewer;
					scrollViewer.ScrollChanged += new ScrollChangedEventHandler(ScrollViewerScrollChanged);
					// assume we do the regulation of the horizontal scrollbar
					scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
					scrollViewer.VerticalScrollBarVisibility = verticalScrollBarVisibility;
				}

				RegisterEvents(childVisual);  // recursive
			}
		}

		private void UnregisterEvents(DependencyObject start)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(start); i++)
			{
				Visual childVisual = VisualTreeHelper.GetChild(start, i) as Visual;
				if (childVisual is Thumb)
				{
					GridViewColumn gridViewColumn = FindParentColumn(childVisual);
					if (gridViewColumn != null)
					{
						Thumb thumb = childVisual as Thumb;
						if (ProportionalColumn.IsProportionalColumn(gridViewColumn) ||
							FixedColumn.IsFixedColumn(gridViewColumn) || IsFillColumn(gridViewColumn))
						{
							thumb.IsHitTestVisible = true;
						}
						else
						{
							thumb.PreviewMouseMove -= new MouseEventHandler(ThumbPreviewMouseMove);
							thumb.PreviewMouseLeftButtonDown -= new MouseButtonEventHandler(ThumbPreviewMouseLeftButtonDown);
							DependencyPropertyDescriptor.FromProperty(
								GridViewColumn.WidthProperty,
								typeof(GridViewColumn)).RemoveValueChanged(gridViewColumn, GridColumnWidthChanged);
						}
					}
				}
				else if (childVisual is GridViewColumnHeader)
				{
					GridViewColumnHeader columnHeader = childVisual as GridViewColumnHeader;
					columnHeader.SizeChanged -= new SizeChangedEventHandler(GridColumnHeaderSizeChanged);
				}
				else if (scrollViewer == null && childVisual is ScrollViewer)
				{
					scrollViewer = childVisual as ScrollViewer;
					scrollViewer.ScrollChanged -= new ScrollChangedEventHandler(ScrollViewerScrollChanged);
				}

				UnregisterEvents(childVisual);  // recursive
			}
		}

		private GridViewColumn FindParentColumn(DependencyObject element)
		{
			if (element == null)
			{
				return null;
			}

			while (element != null)
			{
				GridViewColumnHeader gridViewColumnHeader = element as GridViewColumnHeader;
				if (gridViewColumnHeader != null)
				{
					return (gridViewColumnHeader).Column;
				}
				element = VisualTreeHelper.GetParent(element);
			}

			return null;
		}

		private GridViewColumnHeader FindColumnHeader(DependencyObject start, GridViewColumn gridViewColumn)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(start); i++)
			{
				Visual childVisual = VisualTreeHelper.GetChild(start, i) as Visual;
				if (childVisual is GridViewColumnHeader)
				{
					GridViewColumnHeader gridViewHeader = childVisual as GridViewColumnHeader;
					if (gridViewHeader.Column == gridViewColumn)
					{
						return gridViewHeader;
					}
				}
				GridViewColumnHeader childGridViewHeader = FindColumnHeader(childVisual, gridViewColumn);  // recursive
				if (childGridViewHeader != null)
				{
					return childGridViewHeader;
				}
			}
			return null;
		}

		private void InitColumns()
		{
			GridView view = listView.View as GridView;
			if (view == null)
			{
				return;
			}

			foreach (GridViewColumn gridViewColumn in view.Columns)
			{
				if (!RangeColumn.IsRangeColumn(gridViewColumn))
				{
					continue;
				}

				double? minWidth = RangeColumn.GetRangeMinWidth(gridViewColumn);
				double? maxWidth = RangeColumn.GetRangeMaxWidth(gridViewColumn);
				if (!minWidth.HasValue && !maxWidth.HasValue)
				{
					continue;
				}

				GridViewColumnHeader columnHeader = FindColumnHeader(listView, gridViewColumn);
				if (columnHeader == null)
				{
					continue;
				}

				double actualWidth = columnHeader.ActualWidth;
				if (minWidth.HasValue)
				{
					columnHeader.MinWidth = minWidth.Value;
					if (!double.IsInfinity(actualWidth) && actualWidth < columnHeader.MinWidth)
					{
						gridViewColumn.Width = columnHeader.MinWidth;
					}
				}
				if (maxWidth.HasValue)
				{
					columnHeader.MaxWidth = maxWidth.Value;
					if (!double.IsInfinity(actualWidth) && actualWidth > columnHeader.MaxWidth)
					{
						gridViewColumn.Width = columnHeader.MaxWidth;
					}
				}
			}
		}

		protected virtual void ResizeColumns()
		{
			GridView view = listView.View as GridView;
			if (view == null || view.Columns.Count == 0)
			{
				return;
			}

			// listview width
			double actualWidth = double.PositiveInfinity;
			if (scrollViewer != null)
			{
				actualWidth = scrollViewer.ViewportWidth - 4;
			}
			if (double.IsInfinity(actualWidth))
			{
				actualWidth = listView.ActualWidth;
			}
			if (double.IsInfinity(actualWidth) || actualWidth <= 0)
			{
				return;
			}

			double resizeableRegionCount = 0;
			double otherColumnsWidth = 0;
			// determine column sizes
			foreach (GridViewColumn gridViewColumn in view.Columns)
			{
				if (ProportionalColumn.IsProportionalColumn(gridViewColumn))
				{
					double? proportionalWidth = ProportionalColumn.GetProportionalWidth(gridViewColumn);
					if (proportionalWidth != null)
					{
						resizeableRegionCount += proportionalWidth.Value;
					}
				}
				else
				{
					otherColumnsWidth += gridViewColumn.ActualWidth;
				}
			}

			//if (DateTime.Now.Minute >= 4) System.Diagnostics.Debugger.Break();

			if (resizeableRegionCount <= 0)
			{
				// no proportional columns present: commit the regulation to the scroll viewer
				if (scrollViewer != null)
				{
					scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
				}

				// search the first fill column
				GridViewColumn fillColumn = null;
				for (int i = 0; i < view.Columns.Count; i++)
				{
					GridViewColumn gridViewColumn = view.Columns[i];
					if (IsFillColumn(gridViewColumn))
					{
						fillColumn = gridViewColumn;
						break;
					}
				}

				if (fillColumn != null)
				{
					double otherColumnsWithoutFillWidth = otherColumnsWidth - fillColumn.ActualWidth;
					double fillWidth = actualWidth - otherColumnsWithoutFillWidth;
					if (fillWidth > 0)
					{
						double? minWidth = RangeColumn.GetRangeMinWidth(fillColumn);
						double? maxWidth = RangeColumn.GetRangeMaxWidth(fillColumn);

						bool setWidth = !(minWidth.HasValue && fillWidth < minWidth.Value);
						if (maxWidth.HasValue && fillWidth > maxWidth.Value)
						{
							setWidth = false;
						}
						if (setWidth)
						{
							if (scrollViewer != null)
							{
								scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
							}
							fillColumn.Width = fillWidth;
						}
					}
					else
					{
						// There's not enough space for any fill columns to display.
						// We may need a horizontal scrollbar then.
						if (scrollViewer != null)
						{
							scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
						}
					}
				}
				return;
			}

			double resizeableColumnsWidth = actualWidth - otherColumnsWidth;
			if (resizeableColumnsWidth <= 0)
			{
				// There's not enough space for any fill columns to display.
				// We may need a horizontal scrollbar then.
				if (scrollViewer != null)
				{
					scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
				}
				resizeableColumnsWidth = 0;
			}

			// resize columns
			double resizeableRegionWidth = resizeableColumnsWidth / resizeableRegionCount;
			foreach (GridViewColumn gridViewColumn in view.Columns)
			{
				if (ProportionalColumn.IsProportionalColumn(gridViewColumn))
				{
					double? proportionalWidth = ProportionalColumn.GetProportionalWidth(gridViewColumn);
					if (proportionalWidth != null)
					{
						gridViewColumn.Width = proportionalWidth.Value * resizeableRegionWidth;
					}
				}
			}
		}

		// returns the delta
		private double SetRangeColumnToBounds(GridViewColumn gridViewColumn)
		{
			double startWidth = gridViewColumn.Width;

			double? minWidth = RangeColumn.GetRangeMinWidth(gridViewColumn);
			double? maxWidth = RangeColumn.GetRangeMaxWidth(gridViewColumn);

			if ((minWidth.HasValue && maxWidth.HasValue) && (minWidth > maxWidth))
			{
				return 0; // invalid case
			}

			if (minWidth.HasValue && gridViewColumn.Width < minWidth.Value)
			{
				gridViewColumn.Width = minWidth.Value;
			}
			else if (maxWidth.HasValue && gridViewColumn.Width > maxWidth.Value)
			{
				gridViewColumn.Width = maxWidth.Value;
			}

			return gridViewColumn.Width - startWidth;
		}

		private bool IsFillColumn(GridViewColumn gridViewColumn)
		{
			if (gridViewColumn == null)
			{
				return false;
			}

			GridView view = listView.View as GridView;
			if (view == null || view.Columns.Count == 0)
			{
				return false;
			}

			bool? isFillColumn = RangeColumn.GetRangeIsFillColumn(gridViewColumn);
			return isFillColumn.HasValue && isFillColumn.Value;
		}

		private void DoResizeColumns()
		{
			if (resizing)
			{
				return;
			}

			resizing = true;
			try
			{
				ResizeColumns();
			}
			finally
			{
				resizing = false;
			}
		}

		private void ListViewLoaded(object sender, RoutedEventArgs args)
		{
			RegisterEvents(listView);
			InitColumns();
			DoResizeColumns();
			loaded = true;
		}

		private void ListViewUnloaded(object sender, RoutedEventArgs args)
		{
			if (!loaded)
			{
				return;
			}
			UnregisterEvents(listView);
			loaded = false;
		}

		private void ThumbPreviewMouseMove(object sender, MouseEventArgs args)
		{
			Thumb thumb = sender as Thumb;
			if (thumb == null)
			{
				return;
			}
			GridViewColumn gridViewColumn = FindParentColumn(thumb);
			if (gridViewColumn == null)
			{
				return;
			}

			// suppress column resizing for proportional, fixed and range fill columns
			if (ProportionalColumn.IsProportionalColumn(gridViewColumn) ||
				FixedColumn.IsFixedColumn(gridViewColumn) ||
				IsFillColumn(gridViewColumn))
			{
				thumb.Cursor = null;
				return;
			}

			// check range column bounds
			if (thumb.IsMouseCaptured && RangeColumn.IsRangeColumn(gridViewColumn))
			{
				double? minWidth = RangeColumn.GetRangeMinWidth(gridViewColumn);
				double? maxWidth = RangeColumn.GetRangeMaxWidth(gridViewColumn);

				if ((minWidth.HasValue && maxWidth.HasValue) && (minWidth > maxWidth))
				{
					return; // invalid case
				}

				if (resizeCursor == null)
				{
					resizeCursor = thumb.Cursor; // save the resize cursor
				}

				if (minWidth.HasValue && gridViewColumn.Width <= minWidth.Value)
				{
					thumb.Cursor = Cursors.No;
				}
				else if (maxWidth.HasValue && gridViewColumn.Width >= maxWidth.Value)
				{
					thumb.Cursor = Cursors.No;
				}
				else
				{
					thumb.Cursor = resizeCursor; // between valid min/max
				}
			}
		}

		private void ThumbPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
		{
			Thumb thumb = sender as Thumb;
			GridViewColumn gridViewColumn = FindParentColumn(thumb);

			// suppress column resizing for proportional, fixed and range fill columns
			if (ProportionalColumn.IsProportionalColumn(gridViewColumn) ||
				FixedColumn.IsFixedColumn(gridViewColumn) ||
				IsFillColumn(gridViewColumn))
			{
				args.Handled = true;
			}
		}

		private void GridColumnWidthChanged(object sender, EventArgs args)
		{
			if (!loaded)
			{
				return;
			}

			GridViewColumn gridViewColumn = sender as GridViewColumn;

			// suppress column resizing for proportional and fixed columns
			if (ProportionalColumn.IsProportionalColumn(gridViewColumn) || FixedColumn.IsFixedColumn(gridViewColumn))
			{
				return;
			}

			// ensure range column within the bounds
			if (RangeColumn.IsRangeColumn(gridViewColumn))
			{
				// special case: auto column width - maybe conflicts with min/max range
				if (gridViewColumn != null && gridViewColumn.Width.Equals(double.NaN))
				{
					autoSizedColumn = gridViewColumn;
					return; // handled by the change header size event
				}

				// ensure column bounds
				if (Math.Abs(SetRangeColumnToBounds(gridViewColumn) - 0) > zeroWidthRange)
				{
					return;
				}
			}

			DoResizeColumns();
		}

		// handle autosized column
		private void GridColumnHeaderSizeChanged(object sender, SizeChangedEventArgs args)
		{
			if (autoSizedColumn == null)
			{
				return;
			}

			GridViewColumnHeader gridViewColumnHeader = sender as GridViewColumnHeader;
			if (gridViewColumnHeader != null && gridViewColumnHeader.Column == autoSizedColumn)
			{
				if (gridViewColumnHeader.Width.Equals(double.NaN))
				{
					// sync column with
					gridViewColumnHeader.Column.Width = gridViewColumnHeader.ActualWidth;
					DoResizeColumns();
				}

				autoSizedColumn = null;
			}
		}

		private void ScrollViewerScrollChanged(object sender, ScrollChangedEventArgs args)
		{
			if (loaded && Math.Abs(args.ViewportWidthChange - 0) > zeroWidthRange)
			{
				DoResizeColumns();
			}
		}

		private static void OnLayoutManagerEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			ListView listView = dependencyObject as ListView;
			if (listView != null)
			{
				bool enabled = (bool)args.NewValue;
				if (enabled)
				{
					new ListViewLayoutManager(listView);
				}
			}
		}
	}

	#endregion ListViewLayoutManager class

	#region Column type classes

	public abstract class ConverterGridViewColumn : GridViewColumn, IValueConverter
	{
		private readonly Type bindingType;

		protected ConverterGridViewColumn(Type bindingType)
		{
			if (bindingType == null)
			{
				throw new ArgumentNullException("bindingType");
			}

			this.bindingType = bindingType;

			Binding binding = new Binding();
			binding.Mode = BindingMode.OneWay;
			binding.Converter = this;
			DisplayMemberBinding = binding;
		}

		public Type BindingType
		{
			get { return bindingType; }
		}

		protected abstract object ConvertValue(object value);

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!bindingType.IsInstanceOfType(value))
			{
				throw new InvalidOperationException();
			}
			return ConvertValue(value);
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public abstract class LayoutColumn
	{
		protected static bool HasPropertyValue(GridViewColumn column, DependencyProperty dp)
		{
			if (column == null)
			{
				throw new ArgumentNullException("column");
			}
			object value = column.ReadLocalValue(dp);
			if (value != null && value.GetType() == dp.PropertyType)
			{
				return true;
			}
			return false;
		}

		protected static double? GetColumnWidth(GridViewColumn column, DependencyProperty dp)
		{
			if (column == null)
			{
				throw new ArgumentNullException("column");
			}
			object value = column.ReadLocalValue(dp);
			if (value != null && value.GetType() == dp.PropertyType)
			{
				return (double) value;
			}
			return null;
		}
	}

	[Obfuscation(Exclude = true, Feature = "renaming")]
	public sealed class FixedColumn : LayoutColumn
	{
		public static readonly DependencyProperty WidthProperty = DependencyProperty.RegisterAttached(
			"Width",
			typeof(double),
			typeof(FixedColumn));

		private FixedColumn()
		{
		}

		public static double GetWidth(DependencyObject obj)
		{
			return (double) obj.GetValue(WidthProperty);
		}

		public static void SetWidth(DependencyObject obj, double width)
		{
			obj.SetValue(WidthProperty, width);
		}

		public static bool IsFixedColumn(GridViewColumn column)
		{
			if (column == null)
			{
				return false;
			}
			return HasPropertyValue(column, WidthProperty);
		}

		public static double? GetFixedWidth(GridViewColumn column)
		{
			return GetColumnWidth(column, WidthProperty);
		}

		public static GridViewColumn ApplyWidth(GridViewColumn gridViewColumn, double width)
		{
			SetWidth(gridViewColumn, width);
			return gridViewColumn;
		}
	}

	[Obfuscation(Exclude = true, Feature = "renaming")]
	public abstract class ImageGridViewColumn : GridViewColumn, IValueConverter
	{
		protected ImageGridViewColumn() :
			this(Stretch.None)
		{
		}

		protected ImageGridViewColumn(Stretch imageStretch)
		{
			FrameworkElementFactory imageElement = new FrameworkElementFactory(typeof(Image));

			// image source
			Binding imageSourceBinding = new Binding();
			imageSourceBinding.Converter = this;
			imageSourceBinding.Mode = BindingMode.OneWay;
			imageElement.SetBinding(Image.SourceProperty, imageSourceBinding);

			// image stretching
			Binding imageStretchBinding = new Binding();
			imageStretchBinding.Source = imageStretch;
			imageElement.SetBinding(Image.StretchProperty, imageStretchBinding);

			DataTemplate template = new DataTemplate();
			template.VisualTree = imageElement;
			CellTemplate = template;
		}

		protected abstract ImageSource GetImageSource(object value);

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return GetImageSource(value);
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	[Obfuscation(Exclude = true, Feature = "renaming")]
	public sealed class ProportionalColumn : LayoutColumn
	{
		public static readonly DependencyProperty WidthProperty = DependencyProperty.RegisterAttached(
			"Width",
			typeof(double),
			typeof(ProportionalColumn));

		private ProportionalColumn()
		{
		}

		public static double GetWidth(DependencyObject obj)
		{
			return (double) obj.GetValue(WidthProperty);
		}

		public static void SetWidth(DependencyObject obj, double width)
		{
			obj.SetValue(WidthProperty, width);
		}

		public static bool IsProportionalColumn(GridViewColumn column)
		{
			if (column == null)
			{
				return false;
			}
			return HasPropertyValue(column, WidthProperty);
		}

		public static double? GetProportionalWidth(GridViewColumn column)
		{
			return GetColumnWidth(column, WidthProperty);
		}

		public static GridViewColumn ApplyWidth(GridViewColumn gridViewColumn, double width)
		{
			SetWidth(gridViewColumn, width);
			return gridViewColumn;
		}
	}

	[Obfuscation(Exclude = true, Feature = "renaming")]
	public sealed class RangeColumn : LayoutColumn
	{
		public static readonly DependencyProperty MinWidthProperty = DependencyProperty.RegisterAttached(
			"MinWidth",
			typeof(double),
			typeof(RangeColumn));

		public static readonly DependencyProperty MaxWidthProperty = DependencyProperty.RegisterAttached(
			"MaxWidth",
			typeof(double),
			typeof(RangeColumn));

		public static readonly DependencyProperty IsFillColumnProperty = DependencyProperty.RegisterAttached(
			"IsFillColumn",
			typeof(bool),
			typeof(RangeColumn));

		private RangeColumn()
		{
		}

		public static double GetMinWidth(DependencyObject obj)
		{
			return (double) obj.GetValue(MinWidthProperty);
		}

		public static void SetMinWidth(DependencyObject obj, double minWidth)
		{
			obj.SetValue(MinWidthProperty, minWidth);
		}

		public static double GetMaxWidth(DependencyObject obj)
		{
			return (double) obj.GetValue(MaxWidthProperty);
		}

		public static void SetMaxWidth(DependencyObject obj, double maxWidth)
		{
			obj.SetValue(MaxWidthProperty, maxWidth);
		}

		public static bool GetIsFillColumn(DependencyObject obj)
		{
			return (bool) obj.GetValue(IsFillColumnProperty);
		}

		public static void SetIsFillColumn(DependencyObject obj, bool isFillColumn)
		{
			obj.SetValue(IsFillColumnProperty, isFillColumn);
		}

		public static bool IsRangeColumn(GridViewColumn column)
		{
			if (column == null)
			{
				return false;
			}
			return
				HasPropertyValue(column, MinWidthProperty) ||
				HasPropertyValue(column, MaxWidthProperty) ||
				HasPropertyValue(column, IsFillColumnProperty);
		}

		public static double? GetRangeMinWidth(GridViewColumn column)
		{
			return GetColumnWidth(column, MinWidthProperty);
		}

		public static double? GetRangeMaxWidth(GridViewColumn column)
		{
			return GetColumnWidth(column, MaxWidthProperty);
		}

		public static bool? GetRangeIsFillColumn(GridViewColumn column)
		{
			if (column == null)
			{
				throw new ArgumentNullException("column");
			}
			object value = column.ReadLocalValue(IsFillColumnProperty);
			if (value != null && value.GetType() == IsFillColumnProperty.PropertyType)
			{
				return (bool) value;
			}
			return null;
		}

		public static GridViewColumn ApplyWidth(GridViewColumn gridViewColumn, double minWidth,
			double width, double maxWidth)
		{
			return ApplyWidth(gridViewColumn, minWidth, width, maxWidth, false);
		}

		public static GridViewColumn ApplyWidth(GridViewColumn gridViewColumn, double minWidth,
			double width, double maxWidth, bool isFillColumn)
		{
			SetMinWidth(gridViewColumn, minWidth);
			gridViewColumn.Width = width;
			SetMaxWidth(gridViewColumn, maxWidth);
			SetIsFillColumn(gridViewColumn, isFillColumn);
			return gridViewColumn;
		}
	}

	#endregion Column type classes
}
