using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace tfsk
{
	public class DiffRichTextBox : RichTextBox
	{
		public static readonly DependencyProperty DocumentProperty =
			DependencyProperty.Register("DiffDocument",
			typeof(FlowDocument),
			typeof(DiffRichTextBox),
			new PropertyMetadata(new PropertyChangedCallback(DiffDocumentChanged)));

		private static void DiffDocumentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			DiffRichTextBox rtbBox = obj as DiffRichTextBox;
			if (rtbBox != null)
			{
				rtbBox.Document = rtbBox.DiffDocument;
			}
		}

		public FlowDocument DiffDocument
		{
			get { return GetValue(DocumentProperty) as FlowDocument; }
			set { SetValue(DocumentProperty, value); }
		}
	}
}
