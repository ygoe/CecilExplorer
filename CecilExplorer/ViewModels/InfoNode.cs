using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.TreeView;

namespace CecilExplorer.ViewModels
{
	internal class InfoNode : SharpTreeNode
	{
		private TextBlock text;
		private ImageSource icon;

		public InfoNode(string message, Brush color, ImageSource icon)
		{
			text = new TextBlock
			{
				Text = message,
				TextTrimming = TextTrimming.CharacterEllipsis,
				Foreground = color
			};
			this.icon = icon;
		}

		public override object Text => text;

		public override object Icon => icon;
	}
}
