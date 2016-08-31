using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CecilExplorer.Unclassified.UI;
using ICSharpCode.TreeView;
using Unclassified.UI;
using Unclassified.Util;

namespace CecilExplorer.ViewModels
{
	internal class ObjectViewModel : SharpTreeNode
	{
		private static readonly ImageSource fieldIcon = ImageSelector.Select("FieldBlue16Icon");
		private static readonly ImageSource methodIcon = ImageSelector.Select("MethodPurple16Icon");
		private static readonly ImageSource moduleIcon = ImageSelector.Select("Module16Icon");
		private static readonly ImageSource propertyIcon = ImageSelector.Select("Property16Icon");
		private static readonly ImageSource loopIcon = ImageSelector.Select("Loop16Icon");

		private static readonly Brush defaultBorderBrush = new SolidColorBrush(Color.FromArgb(0x10, 0, 0, 0));
		private static readonly Brush darkBorderBrush = new SolidColorBrush(Color.FromArgb(0x38, 0, 0, 0));

		private static object highlightedObject;

		private TextBlock text;
		private bool canLoadChildren;
		private ImageSource icon;
		private ElementLevel elementLevel;

		public ObjectViewModel(string name, object theObject)
			: this(name, theObject, theObject.GetType(), moduleIcon, null, 0)
		{
		}

		public ObjectViewModel(string name, object theObject, Type type, ImageSource icon, Brush valueColor, ElementLevel lastElementLevel)
		{
			this.icon = icon;
			Name = name;
			TheObject = theObject;

			elementLevel = lastElementLevel;
			ElementLevel level;
			if (TryGetElementLevel(out level))
			{
				elementLevel = level;
				IsHigherElementLevel = elementLevel < lastElementLevel;
			}

			Type = FormatType(type);
			if (TheObject != null &&
				TheObject.GetType() != type)
			{
				Type += " {" + FormatType(TheObject.GetType()) + "}";
				if (typeof(Mono.Cecil.MemberReference).IsAssignableFrom(type) &&
					typeof(Mono.Cecil.IMemberDefinition).IsAssignableFrom(TheObject.GetType()))
				{
					IsDefinitionForReference = true;
				}
			}
			SetValue();
			if (valueColor != null)
			{
				ValueColor = valueColor;
			}
			if (IsDefinitionForReference)
			{
				ValueColor = Brushes.Green;
			}
			if (IsHigherElementLevel)
			{
				ValueColor = Brushes.DarkViolet;
			}
			LazyLoading = canLoadChildren;
		}

		public object TheObject { get; private set; }

		public ObjectViewModel ParentObject => Parent as ObjectViewModel;

		public override object Icon => icon;

		public override object Text
		{
			get
			{
				if (text == null)
				{
					text = new TextBlock { Text = Name };
					TextBlockAutoToolTip.SetEnabled(text, true);
				}
				return text;
			}
		}

		public string Name { get; }

		public string Value { get; private set; }

		public string Type { get; }

		public bool IsDefinitionForReference { get; private set; }

		public bool IsHigherElementLevel { get; private set; }

		public bool IsMethod => icon == methodIcon;

		public bool IsChildrenLoaded { get; private set; }

		public bool IsChildrenLoadedBySearch { get; set; }

		public override string ToString()
		{
			return Name + " = " + Value;
		}

		protected override void OnIsSelectedChanged()
		{
			ForAllNodes(o => { o.StateColor = Brushes.Transparent; o.BorderColor = defaultBorderBrush; });

			if (IsSelected)
			{
				highlightedObject = TheObject;

				TaskHelper.Post(() =>
				{
					var parent = ParentObject;
					while (parent != null)
					{
						//parent.StateColor = Brushes.AliceBlue;
						parent.BorderColor = darkBorderBrush;
						parent = parent.ParentObject;
					}

					ForAllNodes(o => o.HighlightObject());
				});
			}
		}

		private Brush borderColor = defaultBorderBrush;
		public Brush BorderColor
		{
			get
			{
				return borderColor;
			}
			set
			{
				if (value != borderColor)
				{
					borderColor = value;
					RaisePropertyChanged(nameof(BorderColor));
				}
			}
		}

		private Brush stateColor;
		public Brush StateColor
		{
			get
			{
				return stateColor;
			}
			set
			{
				if (value != stateColor)
				{
					stateColor = value;
					RaisePropertyChanged(nameof(StateColor));
				}
			}
		}

		public Brush ValueColor { get; set; } = Brushes.Black;

		private void SetValue()
		{
			if (TheObject == null)
			{
				Value = "null";
				ValueColor = Brushes.Gray;
				canLoadChildren = false;
			}
			else if (TheObject is bool)
			{
				Value = ((bool)TheObject).ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
			}
			else if (TheObject is byte)
			{
				Value = (byte)TheObject + " (0x" + GroupDigits(((byte)TheObject).ToString("x2")) + ")";
			}
			else if (TheObject is sbyte)
			{
				Value = (sbyte)TheObject + " (0x" + GroupDigits(((sbyte)TheObject).ToString("x2")) + ")";
			}
			else if (TheObject is ushort)
			{
				Value = (ushort)TheObject + " (0x" + GroupDigits(((ushort)TheObject).ToString("x4")) + ")";
			}
			else if (TheObject is short)
			{
				Value = (short)TheObject + " (0x" + GroupDigits(((short)TheObject).ToString("x4")) + ")";
			}
			else if (TheObject is uint)
			{
				Value = (uint)TheObject + " (0x" + GroupDigits(((uint)TheObject).ToString("x8")) + ")";
			}
			else if (TheObject is int)
			{
				Value = (int)TheObject + " (0x" + GroupDigits(((int)TheObject).ToString("x8")) + ")";
			}
			else if (TheObject is ulong)
			{
				Value = (ulong)TheObject + " (0x" + GroupDigits(((ulong)TheObject).ToString("x16")) + ")";
			}
			else if (TheObject is long)
			{
				Value = (long)TheObject + " (0x" + GroupDigits(((long)TheObject).ToString("x16")) + ")";
			}
			else if (TheObject is Exception)
			{
				Value = ((Exception)TheObject).Message;
				canLoadChildren = true;
			}
			else if (canLoadChildren && IsEnumerable(TheObject))
			{
				int count = ((IEnumerable)TheObject).OfType<object>().Count();
				if (count == 0)
					Value = "(empty)";
				else if (count == 1)
					Value = "(1 item)";
				else
					Value = "(" + count + " items)";
				ValueColor = Brushes.Gray;
				if (count == 0)
					canLoadChildren = false;
			}
			else
			{
				Value = Convert.ToString(TheObject, CultureInfo.InvariantCulture);
				if (Value == TheObject.GetType().FullName)
				{
					if (TheObject.GetType() == typeof(Mono.Cecil.CustomAttribute))
					{
						Value = (TheObject.GetType().GetProperty("AttributeType")?.GetValue(TheObject) as Mono.Cecil.TypeReference).Name;
						ValueColor = Brushes.MediumBlue;
					}
					else if (TheObject.GetType() == typeof(Mono.Cecil.CustomAttributeArgument))
					{
						Value = "(" + (TheObject.GetType().GetProperty("Type")?.GetValue(TheObject) as Mono.Cecil.TypeReference).Name + ") " +
							Convert.ToString(TheObject.GetType().GetProperty("Value")?.GetValue(TheObject), CultureInfo.InvariantCulture);
						ValueColor = Brushes.MediumBlue;
					}
				}
			}
		}

		private string GroupDigits(string str)
		{
			StringBuilder sb = new StringBuilder();
			int i = 0;
			if (str.Length % 2 != 0)
			{
				sb.Append(str[i++]);
			}
			while (i < str.Length)
			{
				if (i > 0)
					sb.Append(" ");
				sb.Append(str[i++]);
				sb.Append(str[i++]);
			}
			return sb.ToString();
		}

		private string FormatType(Type type)
		{
			if (type == null) return "";
			if (type == typeof(object)) return "object";
			if (type == typeof(bool)) return "bool";
			if (type == typeof(byte)) return "byte";
			if (type == typeof(sbyte)) return "sbyte";
			if (type == typeof(ushort)) return "ushort";
			if (type == typeof(short)) return "short";
			if (type == typeof(uint)) return "uint";
			if (type == typeof(int)) return "int";
			if (type == typeof(ulong)) return "ulong";
			if (type == typeof(long)) return "long";
			if (type == typeof(float)) return "float";
			if (type == typeof(double)) return "double";
			if (type == typeof(decimal)) return "decimal"; 
			if (type == typeof(char)) return "char";
			if (type == typeof(string)) return "string";

			canLoadChildren = true;
			if (type == typeof(object[])) return "object[]";
			if (type == typeof(bool[])) return "bool[]";
			if (type == typeof(byte[])) return "byte[]";
			if (type == typeof(sbyte[])) return "sbyte[]";
			if (type == typeof(ushort[])) return "ushort[]";
			if (type == typeof(short[])) return "short[]";
			if (type == typeof(uint[])) return "uint[]";
			if (type == typeof(int[])) return "int[]";
			if (type == typeof(ulong[])) return "ulong[]";
			if (type == typeof(long[])) return "long[]";
			if (type == typeof(float[])) return "float[]";
			if (type == typeof(double[])) return "double[]";
			if (type == typeof(decimal[])) return "decimal[]";
			if (type == typeof(char[])) return "char[]";
			if (type == typeof(string[])) return "string[]";

			canLoadChildren = false;
			if (type.IsEnum)
			{
				return type.Name;
			}
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				return FormatType(type.GenericTypeArguments[0]) + "?";
			}
			if (!type.IsGenericType)
			{
				canLoadChildren = true;
				return type.Name;
			}
			canLoadChildren = true;
			StringBuilder sb = new StringBuilder();
			sb.Append(type.Name.Split('`')[0]);
			sb.Append("<");
			for (int i = 0; i < type.GenericTypeArguments.Length; i++)
			{
				if (i > 0)
					sb.Append(", ");
				sb.Append(FormatType(type.GenericTypeArguments[i]));
			}
			sb.Append(">");
			return sb.ToString();
		}

		protected override void LoadChildren()
		{
			bool referenceLoop = false;
			ObjectViewModel parent = ParentObject;
			while (parent != null)
			{
				if (parent.TheObject == TheObject)
				{
					referenceLoop = true;
					break;
				}
				parent = parent.ParentObject;
			}

			if (TheObject != null && referenceLoop)
			{
				var child = new InfoNode("Reference loop", Brushes.Gray, loopIcon);
				Children.Add(child);
			}
			else if (TheObject != null && canLoadChildren)
			{
				if (TheObject.GetType().IsArray)
				{
					Array a = (Array)TheObject;
					for (int i = 0; i < a.Length; i++)
					{
						object value = a.GetValue(i);
						Children.Add(new ObjectViewModel($"[{i}]", value, TheObject.GetType().GetElementType(), fieldIcon, null, elementLevel));
					}
				}
				else if (IsEnumerable(TheObject))
				{
					int i = 0;
					foreach (var value in TheObject as IEnumerable)
					{
						Children.Add(new ObjectViewModel($"[{i}]", value, value.GetType(), fieldIcon, null, elementLevel));
						i++;
					}
				}
				else
				{
					foreach (var propInfo in TheObject.GetType()
						.GetProperties(BindingFlags.Instance | BindingFlags.Public)
						.Where(p => p.GetMethod != null)
						.OrderBy(p => p.Name))
					{
						object value = null;
						Brush valueColor = null;
						try
						{
							value = propInfo.GetValue(TheObject);
						}
						catch (Exception ex)
						{
							value = "[" + ex.Message + "]";
							valueColor = Brushes.OrangeRed;
						}
						var child = new ObjectViewModel(propInfo.Name, value, propInfo.PropertyType, propertyIcon, valueColor, elementLevel);
						Children.Add(child);
						SetFontWeight(propInfo, child);
						//if (TheObject.GetType() == typeof(Mono.Cecil.Cil.MethodBody) && propInfo.Name == "Instructions")
						//{
						//	child.IsExpanded = true;
						//}
					}

					foreach (var fieldInfo in TheObject.GetType()
						.GetFields(BindingFlags.Instance | BindingFlags.Public)
						.OrderBy(f => f.Name))
					{
						object value = null;
						Brush valueColor = null;
						try
						{
							value = fieldInfo.GetValue(TheObject);
						}
						catch (Exception ex)
						{
							value = "[" + ex.Message + "]";
							valueColor = Brushes.OrangeRed;
						}
						Children.Add(new ObjectViewModel(fieldInfo.Name, value, fieldInfo.FieldType, fieldIcon, valueColor, elementLevel));
					}

					foreach (var methodInfo in TheObject.GetType()
						.GetMethods(BindingFlags.Instance | BindingFlags.Public)
						.Where(m => m.Name != "Clone")
						.Where(m => m.Name != "GetHashCode")
						.Where(m => m.Name != "GetType")
						.Where(m => m.Name != "ToString")
						.Where(m => !m.Name.StartsWith("get_"))
						.Where(m => m.ReturnType != typeof(void))
						.Where(m => m.GetParameters().Length == 0)
						.OrderBy(m => m.Name))
					{
						object value = null;
						Brush valueColor = null;
						try
						{
							value = methodInfo.Invoke(TheObject, new object[0]);
						}
						catch (Exception ex)
						{
							value = ex;
							valueColor = Brushes.OrangeRed;
						}
						Children.Add(new ObjectViewModel(methodInfo.Name + "()", value, methodInfo.ReturnType, methodIcon, valueColor, elementLevel));
					}
				}
			}
			IsChildrenLoaded = true;
			ForNode(this, o => o.HighlightObject());
		}

		public void UnloadChildren()
		{
			Children.Clear();
			IsChildrenLoaded = false;
			LazyLoading = canLoadChildren;
		}

		public static bool IsEnumerable(object value)
		{
			return typeof(IEnumerable).IsAssignableFrom(value.GetType()) ||
				typeof(IEnumerable<>).IsAssignableFrom(value.GetType());
		}

		private void SetFontWeight(PropertyInfo propInfo, ObjectViewModel item)
		{
			if (TheObject.GetType() == typeof(Mono.Cecil.AssemblyDefinition))
			{
				if (propInfo.Name == "EntryPoint" ||
					propInfo.Name == "MainModule")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.CustomAttribute))
			{
				if (propInfo.Name == "AttributeType" ||
					propInfo.Name == "ConstructorArguments")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.FieldDefinition))
			{
				if (propInfo.Name == "Constant" ||
					propInfo.Name == "CustomAttributes" ||
					propInfo.Name == "FieldType")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.GenericInstanceMethod))
			{
				if (propInfo.Name == "DeclaringType" ||
					propInfo.Name == "GenericArguments" ||
					propInfo.Name == "Parameters" ||
					propInfo.Name == "ReturnType")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.GenericInstanceType))
			{
				if (propInfo.Name == "GenericArguments")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.GenericParameter))
			{
				if (propInfo.Name == "Constraints")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.Cil.Instruction))
			{
				if (propInfo.Name == "OpCode" ||
					propInfo.Name == "Operand")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.Cil.MethodBody))
			{
				if (propInfo.Name == "Instructions" ||
					propInfo.Name == "Variables")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.MethodDefinition))
			{
				if (propInfo.Name == "Body" ||
					propInfo.Name == "CustomAttributes" ||
					propInfo.Name == "GenericParameters" ||
					propInfo.Name == "Parameters" ||
					propInfo.Name == "ReturnType")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.MethodReference))
			{
				if (propInfo.Name == "DeclaringType" ||
					propInfo.Name == "Parameters" ||
					propInfo.Name == "ReturnType")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.ModuleDefinition))
			{
				if (propInfo.Name == "Types")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.ParameterDefinition))
			{
				if (propInfo.Name == "ParameterType")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.PropertyDefinition))
			{
				if (propInfo.Name == "CustomAttributes" ||
					propInfo.Name == "GetMethod" ||
					propInfo.Name == "PropertyType" ||
					propInfo.Name == "SetMethod")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
			if (TheObject.GetType() == typeof(Mono.Cecil.TypeDefinition))
			{
				if (propInfo.Name == "BaseType" ||
					propInfo.Name == "CustomAttributes" ||
					propInfo.Name == "Events" ||
					propInfo.Name == "Fields" ||
					propInfo.Name == "Interfaces" ||
					propInfo.Name == "Methods" ||
					propInfo.Name == "NestedTypes" ||
					propInfo.Name == "Properties")
				{
					((TextBlock)item.Text).FontWeight = FontWeights.Bold;
				}
			}
		}

		private void HighlightObject()
		{
			if (!IsSelected &&
				highlightedObject != null &&
				(highlightedObject.GetType().IsValueType && highlightedObject.Equals(TheObject) || TheObject == highlightedObject))
			{
				StateColor = Brushes.LemonChiffon;
			}
		}

		private void ForAllNodes(Action<ObjectViewModel> action)
		{
			ObjectViewModel root = this;
			while (root.ParentObject != null)
			{
				root = root.ParentObject;
			}
			ForNode(root, action);
		}

		private static void ForNode(ObjectViewModel node, Action<ObjectViewModel> action)
		{
			action(node);
			if (node.IsChildrenLoaded)
			{
				foreach (var child in node.Children.OfType<ObjectViewModel>())
				{
					ForNode(child, action);
				}
			}
		}

		private bool TryGetElementLevel(out ElementLevel level)
		{
			level = default(ElementLevel);
			if (TheObject is Mono.Cecil.AssemblyDefinition ||
				TheObject is Mono.Cecil.AssemblyNameReference)
			{
				level = ElementLevel.Assembly;
				return true;
			}
			if (TheObject is Mono.Cecil.ModuleDefinition ||
				TheObject is Mono.Cecil.ModuleReference)
			{
				level = ElementLevel.Module;
				return true;
			}
			if (TheObject is Mono.Cecil.TypeDefinition ||
				TheObject is Mono.Cecil.TypeReference)
			{
				level = ElementLevel.Type;
				return true;
			}
			if (TheObject is Mono.Cecil.IMemberDefinition ||
				TheObject is Mono.Cecil.MemberReference)
			{
				level = ElementLevel.Member;
				return true;
			}
			return false;
		}
	}

	internal enum ElementLevel
	{
		Assembly,
		Module,
		Type,
		Member
	}
}
