using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CecilExplorer.ViewModels;
using ICSharpCode.TreeView;
using Microsoft.Win32;
using Unclassified.Util;

namespace CecilExplorer.Views
{
	public partial class MainWindow : Window
	{
		#region Private data

		private double prevTreeColumnsWidth;
		private double prevColumn0Width;
		private string searchText = "";
		private bool searchCaseSensitive;
		private bool searchExpandNodes = true;
		private bool searchDirectionForward = true;

		#endregion Private data

		#region Constructors

		public MainWindow()
		{
			InitializeComponent();

			Width = 800;
			Height = 500;
			SettingsHelper.BindWindowState(this, App.Settings.MainWindowState);
		}

		#endregion Constructors

		#region Window event handlers

		private void Window_DragEnter(object sender, DragEventArgs args)
		{
			if (!args.Data.GetDataPresent(DataFormats.FileDrop))
			{
				args.Effects = DragDropEffects.None;
			}
		}

		private void Window_Drop(object sender, DragEventArgs args)
		{
			string[] fileNames = args.Data.GetData(DataFormats.FileDrop) as string[];
			if (fileNames != null && fileNames.Length > 0)
			{
				((MainViewModel)DataContext).ReadAssembly(fileNames[0]);
			}
		}

		private void Window_KeyDown(object sender, KeyEventArgs args)
		{
			if (args.Key == Key.F1 && args.KeyboardDevice.Modifiers == ModifierKeys.None)
			{
				var dlg = new AboutDialog { Owner = this };
				dlg.ShowDialog();
				args.Handled = true;
			}
			if (args.Key == Key.O && args.KeyboardDevice.Modifiers == ModifierKeys.Control)
			{
				var dlg = new OpenFileDialog
				{
					Filter = "Assembly files|*.exe;*.dll|All files|*.*",
					Title = "Load assembly file"
				};
				if (dlg.ShowDialog() == true)
				{
					((MainViewModel)DataContext).ReadAssembly(dlg.FileName);
				}
				args.Handled = true;
			}
			if (args.Key == Key.F4 && args.KeyboardDevice.Modifiers == ModifierKeys.None)
			{
				var selectedItem = treeView.SelectedItem as ObjectViewModel;
				if (selectedItem != null &&
					selectedItem.TheObject != null)
				{
					int index = treeView.SelectedIndex;
					do
					{
						index++;
						if (index >= treeView.Items.Count)
							index = 0;
						var otherObject = (treeView.Items[index] as ObjectViewModel)?.TheObject;
						if (selectedItem.TheObject.GetType().IsValueType && selectedItem.TheObject.Equals(otherObject) || selectedItem.TheObject == otherObject)
						{
							treeView.SelectedIndex = index;
							treeView.ScrollIntoView(treeView.Items[index]);
							break;
						}
					}
					while (index != treeView.SelectedIndex);
				}
				args.Handled = true;
			}
			if (args.Key == Key.F4 && args.KeyboardDevice.Modifiers == ModifierKeys.Shift)
			{
				var selectedItem = treeView.SelectedItem as ObjectViewModel;
				if (selectedItem != null &&
					selectedItem.TheObject != null)
				{
					int index = treeView.SelectedIndex;
					do
					{
						index--;
						if (index < 0)
							index = treeView.Items.Count - 1;
						var otherObject = (treeView.Items[index] as ObjectViewModel)?.TheObject;
						if (selectedItem.TheObject.GetType().IsValueType && selectedItem.TheObject.Equals(otherObject) || selectedItem.TheObject == otherObject)
						{
							treeView.SelectedIndex = index;
							treeView.ScrollIntoView(treeView.Items[index]);
							break;
						}
					}
					while (index != treeView.SelectedIndex);
				}
				args.Handled = true;
			}
			if (args.Key == Key.F && args.KeyboardDevice.Modifiers == ModifierKeys.Control)
			{
				var dlg = new FindDialog
				{
					SearchText = searchText,
					CaseSensitive = searchCaseSensitive,
					ExpandNodes = searchExpandNodes,
					Owner = this
				};
				if (dlg.ShowDialog() == true)
				{
					searchText = dlg.SearchText;
					searchCaseSensitive = dlg.CaseSensitive;
					searchExpandNodes = dlg.ExpandNodes;
					searchDirectionForward = dlg.DirectionForward;
					StartFind(treeView.SelectedItem as SharpTreeNode);
				}
				args.Handled = true;
			}
			if (args.Key == Key.F3 && args.KeyboardDevice.Modifiers == ModifierKeys.None && !string.IsNullOrEmpty(searchText))
			{
				searchDirectionForward = true;
				StartFind(treeView.SelectedItem as SharpTreeNode);
				args.Handled = true;
			}
			if (args.Key == Key.F3 && args.KeyboardDevice.Modifiers == ModifierKeys.Shift && !string.IsNullOrEmpty(searchText))
			{
				searchDirectionForward = false;
				StartFind(treeView.SelectedItem as SharpTreeNode);
				args.Handled = true;
			}
		}

		#endregion Window event handlers

		#region Control event handlers

		private void TreeView_SelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			var item = treeView.SelectedItem as ObjectViewModel;
			string status = "";
			bool prevItemIsIndexName = false;
			while (item != null && item != treeView.Items[0])
			{
				bool isIndexName = Regex.IsMatch(item.Name, @"^\[[0-9]+\]$");
				if (!prevItemIsIndexName)
				{
					status = (status != "" ? " / " : "") + status;
				}
				if (isIndexName || item.TheObject is Mono.Cecil.MemberReference)
				{
					string shortName = item.Value;
					if (item.TheObject is Mono.Cecil.MemberReference)
					{
						shortName = ((Mono.Cecil.MemberReference)item.TheObject).Name;
					}
					if (item.TheObject is Mono.Cecil.Cil.Instruction)
					{
						shortName = ((Mono.Cecil.Cil.Instruction)item.TheObject).OpCode.ToString();
					}
					status = " \"" + shortName + "\"" + status;
				}
				status = item.Name + status;
				prevItemIsIndexName = isIndexName;
				item = item.ParentObject;
			}
			statusText.Text = status;
		}

		private void TreeView_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			if (args.WidthChanged)
			{
				if (prevTreeColumnsWidth == 0)
				{
					InitializeColumnWidths(args.NewSize.Width);
				}
				else
				{
					UpdateColumnWidths(prevTreeColumnsWidth, args.NewSize.Width);
				}
				prevTreeColumnsWidth = args.NewSize.Width;
			}
		}

		#endregion Control event handlers

		#region Column width methods

		private void InitializeColumnWidths(double totalWidth)
		{
			var columns = ((SharpGridView)treeView.View).Columns;
			if (App.Settings.ColumnWidths.Length == 2)
			{
				columns[0].Width = App.Settings.ColumnWidths[0];
				columns[1].Width = App.Settings.ColumnWidths[1];
			}
			else
			{
				columns[0].Width = totalWidth * 2 / 7;
				columns[1].Width = totalWidth * 4 / 7;
			}
			prevColumn0Width = columns[0].Width;

			((INotifyPropertyChanged)columns[0]).PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "ActualWidth")
				{
					columns[1].Width -= columns[0].Width - prevColumn0Width;
					prevColumn0Width = columns[0].ActualWidth;
					App.Settings.ColumnWidths = new[] { (int)columns[0].ActualWidth, (int)columns[1].ActualWidth };
				}
			};
			((INotifyPropertyChanged)columns[1]).PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "ActualWidth")
				{
					App.Settings.ColumnWidths = new[] { (int)columns[0].ActualWidth, (int)columns[1].ActualWidth };
				}
			};
		}

		private void UpdateColumnWidths(double oldWidth, double newWidth)
		{
			var columns = ((SharpGridView)treeView.View).Columns;

			double width0 = columns[0].ActualWidth;
			double width1 = columns[1].ActualWidth;

			columns[0].Width = width0 / oldWidth * newWidth;
			columns[1].Width = width1 / oldWidth * newWidth;
			prevColumn0Width = columns[0].Width;
		}

		#endregion Column width methods

		#region Find methods

		private void StartFind(SharpTreeNode node)
		{
			try
			{
				statusBorder.Background = Brushes.MistyRose;
				statusText.Text = "Searching, please wait…";
				Cursor = Cursors.Wait;
				TaskHelper.DoEvents(DispatcherPriority.Background);

				SharpTreeNode startNode;
				bool startFirst = false;
				if (node == null)
				{
					node = treeView.Items.OfType<ObjectViewModel>().FirstOrDefault();
					startNode = node;
					startFirst = true;
				}
				else
				{
					startNode = node;
					if (searchDirectionForward)
						node = NavigateFindNext(node);
					else
						node = NavigateFindPrevious(node);
				}

				while (!CheckFindItem(node) && (node != startNode || startFirst))
				{
					startFirst = false;
					if (searchDirectionForward)
						node = NavigateFindNext(node);
					else
						node = NavigateFindPrevious(node);
				}

				if (node != startNode || startFirst)
				{
					AcceptFindItem((ObjectViewModel)node);
				}
				else
				{
					statusText.Text = "Nothing found.";
				}
			}
			finally
			{
				statusBorder.Background = null;
				Cursor = null;
			}
		}

		private bool CheckFindItem(SharpTreeNode node)
		{
			var item = node as ObjectViewModel;
			StringComparison cmp = StringComparison.InvariantCultureIgnoreCase;
			if (searchCaseSensitive)
				cmp = StringComparison.InvariantCulture;
			return item != null && item.Value.IndexOf(searchText, cmp) != -1;
		}

		private void AcceptFindItem(ObjectViewModel item)
		{
			SharpTreeNode parent = item.Parent;
			while (parent != null)
			{
				parent.IsExpanded = true;
				parent = parent.Parent;
			}
			treeView.SelectedItem = item;
			treeView.FocusNode(item);
			TaskHelper.Background(() => treeView.ScrollIntoView(item));
		}

		private SharpTreeNode NavigateFindNext(SharpTreeNode node)
		{
			var child = GetFirstChild(node);
			if (child != null)
				return child;

			do
			{
				var sibling = GetNextSibling(node);
				if (sibling != null)
					return sibling;

				node = node.Parent;
				var item = node as ObjectViewModel;
				if (item?.IsChildrenLoadedBySearch == true)
				{
					item.UnloadChildren();
					item.IsChildrenLoadedBySearch = false;
				}
			}
			while (node.Parent != null);
			return node;
		}

		private SharpTreeNode NavigateFindPrevious(SharpTreeNode node)
		{
			var sibling = GetPreviousSibling(node);
			if (sibling != null)
			{
				return GetLastGrandChild(sibling);
			}

			if (node.Parent != null)
			{
				node = node.Parent;
				var item = node as ObjectViewModel;
				if (item?.IsChildrenLoadedBySearch == true)
				{
					item.UnloadChildren();
					item.IsChildrenLoadedBySearch = false;
				}
				return node;
			}

			return GetLastGrandChild(node);
		}

		private SharpTreeNode GetFirstChild(SharpTreeNode node)
		{
			var item = node as ObjectViewModel;
			if (!CanLoadChildrenForSearch(item))
			{
				return null;
			}

			if (!searchExpandNodes && !node.IsExpanded)
			{
				return null;
			}
			if (searchExpandNodes)
			{
				if (!item.IsChildrenLoaded)
					item.IsChildrenLoadedBySearch = true;
				node.EnsureLazyChildren();
			}
			foreach (var child in node.Children)
			{
				return child;
			}
			return null;
		}

		private SharpTreeNode GetLastGrandChild(SharpTreeNode node)
		{
			while (true)
			{
				var item = node as ObjectViewModel;
				if (!CanLoadChildrenForSearch(item))
				{
					return node;
				}
				if (searchExpandNodes)
				{
					if (!item.IsChildrenLoaded)
						item.IsChildrenLoadedBySearch = true;
					node.EnsureLazyChildren();
				}
				if (node.Children.Count == 0)
				{
					if (item.IsChildrenLoadedBySearch == true)
					{
						item.UnloadChildren();
						item.IsChildrenLoadedBySearch = false;
					}
					return node;
				}

				node = node.Children.Last();
			}
		}

		private SharpTreeNode GetNextSibling(SharpTreeNode node)
		{
			if (node.Parent == null)
				return null;
			int index = node.Parent.Children.IndexOf(node);
			if (index < node.Parent.Children.Count - 1)
				return node.Parent.Children[index + 1];
			return null;
		}

		private SharpTreeNode GetPreviousSibling(SharpTreeNode node)
		{
			if (node.Parent == null)
				return null;
			int index = node.Parent.Children.IndexOf(node);
			if (index > 0)
				return node.Parent.Children[index - 1];
			return null;
		}

		private bool CanLoadChildrenForSearch(ObjectViewModel item)
		{
			if (item == null ||
				//level > 40 ||
				item.IsDefinitionForReference ||
				item.IsHigherElementLevel ||
				item.IsMethod ||
				item.TheObject is Exception ||
				item.TheObject is Mono.Cecil.Cil.Instruction)
			{
				return false;
			}
			return true;
		}

		#endregion Find methods
	}
}
