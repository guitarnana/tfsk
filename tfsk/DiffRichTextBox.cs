using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace tfsk
{
	public class DiffRichTextBox : RichTextBox
	{
		public static readonly DependencyProperty ChangeDiffStringProperty =
			DependencyProperty.Register("ChangeDiffString",
			typeof(string),
			typeof(DiffRichTextBox),
			new PropertyMetadata(new PropertyChangedCallback(DiffDocumentChanged)));

		private static void DiffDocumentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			DiffRichTextBox rtbBox = obj as DiffRichTextBox;
			if (rtbBox != null)
			{
				rtbBox.Document.Blocks.Clear();
				rtbBox.Document.Blocks.Add(rtbBox.CreateDiffTextForDisplay(rtbBox.ChangeDiffString));
			}
		}

		public string ChangeDiffString
		{
			get { return GetValue(ChangeDiffStringProperty) as string; }
			set { SetValue(ChangeDiffStringProperty, value); }
		}

		public Paragraph CreateDiffTextForDisplay(string diffText)
		{
			Paragraph diffParagraph = new Paragraph();

			string[] lines = diffText.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None);

			foreach (string line in lines)
			{
				if (line.StartsWith("+"))
				{
					diffParagraph.Inlines.Add(new AddTextRun(line));
				}
				else if (line.StartsWith("-"))
				{
					diffParagraph.Inlines.Add(new DeleteTextRun(line));
				}
				else if (line.StartsWith("@@"))
				{
					diffParagraph.Inlines.Add(new LineBreak());
					diffParagraph.Inlines.Add(new LineNumberTextRun(line));
				}
				else
				{
					diffParagraph.Inlines.Add(new Run(line));
				}
				diffParagraph.Inlines.Add(new LineBreak());
			}

			return diffParagraph;
		}

		public class AddTextRun : Run
		{
			public AddTextRun(string text)
				: base(text)
			{
				this.Foreground = Brushes.Green;
			}
		}

		public class DeleteTextRun : Run
		{
			public DeleteTextRun(string text)
				: base(text)
			{
				this.Foreground = Brushes.Red;
			}
		}

		public class LineNumberTextRun : Run
		{
			public LineNumberTextRun(string text)
				: base(text)
			{
				this.Foreground = Brushes.Blue;
			}
		}
	}
}
