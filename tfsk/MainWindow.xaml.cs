using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace tfsk
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly VersionControl versionControl;

		public MainWindow()
		{
			InitializeComponent();

			versionControl = new VersionControl();
			/*
			if (ParseCommandlineArguments())
			{
				versionControl.UpdateVersionControl();

				List<Changeset> changesets = versionControl.QueryChangeset();
				UpdateChangesetSource(changesets);
				UpdateUI(changesets[0]);
			}
			else
			{
				UsageWindow usageWindow = new UsageWindow();
				usageWindow.Show();
				Close();
			}*/
		}

		private void UpdateChangesetSource(List<Changeset> changesets)
		{
			lvChangeset.ItemsSource = changesets;
			CollectionViewSource.GetDefaultView(lvChangeset.ItemsSource).Filter = ChangeSetFilter;
		}

		/// <summary>
		/// Filter out user displayed in changeset listview 
		/// </summary>
		/// <param name="item"></param>
		/// <returns>
		/// True - if we want to display user
		/// False - if we do not want to display user
		/// </returns>
		private bool ChangeSetFilter(object item)
		{
			bool display = true;
			Changeset changeset = item as Changeset;
			string[] excludeUsers = versionControl.ExcludeUsers;
			if (excludeUsers != null)
			{
				foreach (string user in excludeUsers)
				{
					if (changeset.OwnerDisplayName.Equals(user))
					{
						display = false;
					}
				}
			}

			string searchKeyword = tbSearchMessage.Text;
			if (!String.IsNullOrEmpty(searchKeyword))
			{
				Match match = Regex.Match(changeset.Comment, searchKeyword, RegexOptions.IgnoreCase);
				if (!match.Success)
				{
					display = false;
				}
			}
			return display;
		}
		
		private void UpdateChangeDiffBox(Change change)
		{
			rtbChangeDiff.Document.Blocks.Clear();
			rtbChangeDiff.Document.Blocks.Add(CreateDiffTextForDisplay(versionControl.DiffItemWithPrevVersion(change.Item)));
		}

		Paragraph CreateDiffTextForDisplay(string diffText)
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

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Properties.Settings.Default.Save();
		}
	}

	public class AddTextRun : Run
	{
		public AddTextRun (string text) 
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
