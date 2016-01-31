﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Data;
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
		private readonly VersionControlServer versionControl;

		private string tfsUrl;
		private string path;
		private int numDisplay;
		private VersionSpec[] versions;
		private string[] excludeCommitters;

		public MainWindow()
		{
			InitializeComponent();

			if (!ParseCommandlineArguments())
			{
				// print usage
				return;
			}

			TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(tfsUrl));
			versionControl = tpc.GetService<VersionControlServer>();

			QueryHistoryParameters queryHistoryParameter = new QueryHistoryParameters(path, RecursionType.Full);
			queryHistoryParameter.MaxResults = numDisplay;

			if (versions != null && versions.Length == 1)
			{
				queryHistoryParameter.VersionEnd = versions[0];
			}
			else if (versions != null && versions.Length == 2)
			{
				queryHistoryParameter.VersionStart = versions[0];
				queryHistoryParameter.VersionEnd = versions[1];
			}

			List<Changeset> changesets = versionControl.QueryHistory(queryHistoryParameter).ToList();

			lvChangeset.ItemsSource = changesets;

			CollectionViewSource.GetDefaultView(lvChangeset.ItemsSource).Filter = ChangeSetFilter;

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
					tfsUrl = args[i + 1];
				}
				else if (String.Equals(args[i], "-path", StringComparison.OrdinalIgnoreCase))
				{
					path = args[i + 1];
				}
				else if (String.Equals(args[i], "-numdisplay", StringComparison.OrdinalIgnoreCase))
				{
					if (!Int32.TryParse(args[i + 1], out numDisplay))
					{
						Console.WriteLine("Invalid num display. Default to 100.");
					}
				}
				else if (String.Equals(args[i], "-excludeUser", StringComparison.OrdinalIgnoreCase))
				{
					excludeCommitters = args[i + 1].Split(';');
				}
				else if (String.Equals(args[i], "-version", StringComparison.OrdinalIgnoreCase))
				{
					versions = VersionSpec.Parse(args[i + 1], null);
				}
				else
				{
					success = false;
				}
			}

			return success;
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
			if (excludeCommitters != null)
			{
				Changeset changeset = item as Changeset;
				foreach (string committer in excludeCommitters)
				{
					if (changeset.OwnerDisplayName.Equals(committer))
					{
						return false;
					}
				}
			}
			return true;
		}

		private string DiffItemWithPrevVersion(VersionControlServer versionControl, Item item)
		{
			string diffStr = "";

			// Get previous version item
			//
			Item prevItem = versionControl.GetItem(item.ItemId, item.ChangesetId - 1);
			if (prevItem != null)
			{
				DiffItemVersionedFile curFile = new DiffItemVersionedFile(versionControl, item.ItemId, item.ChangesetId, null);
				DiffItemVersionedFile prevFile = new DiffItemVersionedFile(versionControl, prevItem.ItemId, prevItem.ChangesetId, null);

				// Create memory stream for buffering diff output in memory
				//
				MemoryStream memStream = new MemoryStream();

				// Here we set up the options to show the diffs in the console with the unified diff 
				// format.
				DiffOptions options = new DiffOptions();
				options.UseThirdPartyTool = false;

				// These settings are just for the text diff (not needed for an external tool). 
				options.Flags = DiffOptionFlags.EnablePreambleHandling | DiffOptionFlags.IgnoreWhiteSpace;
				options.OutputType = DiffOutputType.Unified;
				options.TargetEncoding = Console.OutputEncoding;
				options.SourceEncoding = Console.OutputEncoding;
				options.StreamWriter = new StreamWriter(memStream);
				options.StreamWriter.AutoFlush = true;

				Difference.DiffFiles(versionControl, prevFile, curFile, options, prevItem.ServerItem, true);

				// Move to the beginning of the stream for reading.
				memStream.Seek(0, SeekOrigin.Begin);

				StreamReader sr = new StreamReader(memStream);
				diffStr = sr.ReadToEnd();
			}

			return diffStr;
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

		private void UpdateUI(Changeset changeset)
		{
			// Update tfs server
			tbTfsServer.Text = tfsUrl;
			
			// Update Path 
			tbPath.Text = path;

			// Update change comment
			tbChangeComment.Text = changeset.Comment;

			// Get all changes for this changeset
			Change[] changes = versionControl.GetChangesForChangeset(
				changeset.ChangesetId,
				false, // includeDownloadInfo
				Int32.MaxValue, // number of items to return
				null); // return from start of this changeset

			// Update list of change files
			lvFiles.ItemsSource = changes;

			// Update diff to show the first file of the change			
			UpdateChangeDiffBox(changes[0]);
		}

		private void UpdateChangeDiffBox(Change change)
		{
			rtbChangeDiff.Document.Blocks.Clear();
			rtbChangeDiff.Document.Blocks.Add(CreateDiffTextForDisplay(DiffItemWithPrevVersion(versionControl, change.Item)));
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
		}

		private void cbNoMin_Unchecked(object sender, RoutedEventArgs e)
		{
			tbVersionMin.IsEnabled = true;
		}

		private void cbLatest_Checked(object sender, RoutedEventArgs e)
		{
			tbVersionMax.IsEnabled = false;
		}

		private void cbLatest_Unchecked(object sender, RoutedEventArgs e)
		{
			tbVersionMax.IsEnabled = true;
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
