using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CecilExplorer.Unclassified.UI
{
	public static class ImageSelector
	{
		public static string ImagePath { get; set; } = $"/{Application.ResourceAssembly.GetName().Name};component/Images/";

		public static ImageSource Select(string imageName)
		{
			if (GetDpi() == 96)
			{
				return new BitmapImage(new Uri($"{ImagePath}{imageName}.png", UriKind.Relative));
			}
			else
			{
				return new DrawingImage(Application.Current.FindResource(imageName) as Drawing);
			}
		}

		public static double GetDpi()
		{
			var dpiXProperty = typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
			var dpiX = (int?)dpiXProperty?.GetValue(null) ?? 96;
			return dpiX;
		}
	}
}
