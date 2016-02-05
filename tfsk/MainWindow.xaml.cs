using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;


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

			if (!ParseCommandlineArguments())
			{
				// print usage
				return;
			}

			List<Changeset> changesets = versionControl.QueryChangeset();
			UpdateChangesetSource(changesets);
			UpdateUI(changesets[0]);
		}

		private bool ParseCommandlineArguments()
		{
			bool success = true;
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 1; i < args.Length; i += 2 )
			{
				if (String.Equals(args[i], "-server", StringComparison.OrdinalIgnoreCase))
				{
					versionControl.TfsUrl = args[i + 1];
				}
				else if (String.Equals(args[i], "-path", StringComparison.OrdinalIgnoreCase))
				{
					versionControl.FilePath = args[i + 1];
				}
				else if (String.Equals(args[i], "-numdisplay", StringComparison.OrdinalIgnoreCase))
				{
					int numDisplay;
					if (Int32.TryParse(args[i + 1], out numDisplay))
					{
						versionControl.NumDisplay = numDisplay;
					}
					{
						Console.WriteLine("Invalid num display. Default to 100.");
					}
				}
				else if (String.Equals(args[i], "-excludeUser", StringComparison.OrdinalIgnoreCase))
				{
					versionControl.ExcludeUsers = args[i + 1].Split(';');
				}
				else if (String.Equals(args[i], "-version", StringComparison.OrdinalIgnoreCase))
				{
					VersionSpec[] versions = VersionSpec.Parse(args[i + 1], null);

					if (versions != null && versions.Length == 1)
					{
						versionControl.VersionMax = versions[0];
						versionControl.GetLatestVersion = false;
					}
					else if (versions != null && versions.Length == 2)
					{
						versionControl.VersionMin = versions[0];
						versionControl.VersionMax = versions[1];
						versionControl.GetLatestVersion = false;
						versionControl.NoMinVersion = false;
					}
				}
				else
				{
					success = false;
				}
			}

			return success;
		}

		private void lvChangeset_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0)
			{
				Changeset changeset = e.AddedItems[0] as Changeset;
				if (changeset != null)
				{
					UpdateUI(changeset);
				}
			}
		}

		private void lvFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0)
			{
				Change change = e.AddedItems[0] as Change;
				if (change != null)
				{
					UpdateChangeDiffBox(change);
				}
			}
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

		private void UpdateUI(Changeset changeset)
		{
			// Update tfs server
			tbTfsServer.Text = versionControl.TfsUrl;
			
			// Update Path 
			tbPath.Text = versionControl.FilePath;

			// Update Version
			UpdateVersionUI();

			// Update num display
			tbNumDisplay.Text = versionControl.NumDisplay.ToString();

			// Update exclude committer
			if (versionControl.ExcludeUsers != null)
			{
				tbExcludeUser.Text = String.Join(";", versionControl.ExcludeUsers);
			}

			// Update change comment
			tbChangeComment.Text = changeset.Comment;

			// Get all changes for this changeset
			Change[] changes = versionControl.GetChangesForChangeset(changeset.ChangesetId);

			// Update list of change files
			lvFiles.ItemsSource = changes;

			// Update diff to show the first file of the change			
			UpdateChangeDiffBox(changes[0]);
		}

		private void UpdateVersionUI()
		{
			if (versionControl.NoMinVersion)
			{
				cbNoMin.IsChecked = true;
			}
			else
			{
				tbVersionMin.Text = versionControl.VersionMin.DisplayString;
			}

			if (versionControl.GetLatestVersion)
			{
				cbLatest.IsChecked = true;
			}
			else
			{
				tbVersionMax.Text = versionControl.VersionMax.DisplayString;
			}
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

		private void cbNoMin_Checked(object sender, RoutedEventArgs e)
		{
			tbVersionMin.IsEnabled = false;
			versionControl.NoMinVersion = true;
		}

		private void cbNoMin_Unchecked(object sender, RoutedEventArgs e)
		{
			tbVersionMin.IsEnabled = true;
			versionControl.NoMinVersion = false;
		}

		private void cbLatest_Checked(object sender, RoutedEventArgs e)
		{
			tbVersionMax.IsEnabled = false;
			versionControl.GetLatestVersion = true;
		}

		private void cbLatest_Unchecked(object sender, RoutedEventArgs e)
		{
			tbVersionMax.IsEnabled = true;
			versionControl.GetLatestVersion = false;
		}

		private void btFilter_Click(object sender, RoutedEventArgs e)
		{
			versionControl.ExcludeUsers = tbExcludeUser.Text.Split(';');
			CollectionViewSource.GetDefaultView(lvChangeset.ItemsSource).Refresh();
		}

		private void btQuery_Click(object sender, RoutedEventArgs e)
		{
			RefreshHistory();
		}

		private void RefreshOnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			RefreshHistory();
		}

		private void RefreshHistory()
		{
			if (!versionControl.TfsUrl.Equals(tbTfsServer.Text))
			{
				versionControl.TfsUrl = tbTfsServer.Text;
			}

			versionControl.FilePath = tbPath.Text;
			versionControl.NumDisplay = Int32.Parse(tbNumDisplay.Text);

			versionControl.NoMinVersion = true;

			if (cbNoMin.IsChecked.HasValue &&
				!cbNoMin.IsChecked.Value &&
				!String.IsNullOrEmpty(tbVersionMin.Text))
			{
				versionControl.VersionMin = VersionSpec.ParseSingleSpec(tbVersionMin.Text, null);
				versionControl.NoMinVersion = false;
			}

			versionControl.GetLatestVersion = true;

			if (cbLatest.IsChecked.HasValue &&
				!cbLatest.IsChecked.Value &&
				!String.IsNullOrEmpty(tbVersionMax.Text))
			{
				versionControl.VersionMax = VersionSpec.ParseSingleSpec(tbVersionMax.Text, null);
				versionControl.GetLatestVersion = false;
			}

			// Query changeset from version control
			//
			List<Changeset> changesets = versionControl.QueryChangeset();
			UpdateChangesetSource(changesets);
			if (changesets.Count > 0)
			{
				UpdateUI(changesets[0]);
			}
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
