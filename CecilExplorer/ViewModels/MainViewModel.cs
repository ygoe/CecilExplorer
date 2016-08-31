using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Mono.Cecil;

namespace CecilExplorer.ViewModels
{
	internal class MainViewModel : INotifyPropertyChanged
	{
		private const string CommonTitle = "Mono.Cecil.Explorer";

		public MainViewModel()
		{
			if (!string.IsNullOrEmpty(App.Settings.LastFileName) &&
				File.Exists(App.Settings.LastFileName))
			{
				ReadAssembly(App.Settings.LastFileName);
			}
		}

		private string title = CommonTitle;
		public string Title
		{
			get
			{
				return title;
			}
			set
			{
				if (value != title)
				{
					title = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
				}
			}
		}

		private ObjectViewModel rootObject;
		public ObjectViewModel RootObject
		{
			get
			{
				return rootObject;
			}
			set
			{
				if (value != rootObject)
				{
					rootObject = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RootObject)));
				}
			}
		}

		public Visibility HintTextVisibility =>
			RootObject == null ? Visibility.Visible : Visibility.Collapsed;

		public void ReadAssembly(string fileName)
		{
			App.Settings.LastFileName = fileName;
			Title = $"{fileName} – {CommonTitle}";
			object obj;
			try
			{
				obj = AssemblyDefinition.ReadAssembly(fileName);
			}
			catch (Exception ex)
			{
				obj = ex;
			}
			RootObject = new ObjectViewModel(Path.GetFileName(fileName), obj);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HintTextVisibility)));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
