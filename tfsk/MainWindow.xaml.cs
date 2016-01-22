using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.IO;

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

		public MainWindow()
		{
			InitializeComponent();

			Dictionary<string, string> arguments;

			if (!ParseCommandlineArguments(out arguments))
			{
				// print usage
				return;
			}

			string tfsUrl, path, numStr;
			if (!arguments.TryGetValue("server", out tfsUrl))
			{
				tfsUrl = "http://sqlbuvsts01:8080/main";
				Console.WriteLine("No server specify. Default to http://sqlbuvsts01:8080/main");
			}

			if (!arguments.TryGetValue("path", out path))
			{
				path = @"$/SQL Server/Imp/DS_Main";
				Console.WriteLine(@"No path specify. Default to $/SQL Server/Imp/DS_Main");
			}
			//var serverPath = @"$/Developer/jarupatj";

			int numDisplay = 100;
			if (arguments.TryGetValue("numdisplay", out numStr))
			{
				if (!Int32.TryParse(numStr, out numDisplay))
				{
					Console.WriteLine("Invalid num display. Default to 100.");
				}
			}
			else
			{
				Console.WriteLine("Invalid num display. Default to 100.");
			}

			TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(tfsUrl));
			versionControl = tpc.GetService<VersionControlServer>();

			List<Changeset> changesets = versionControl.QueryHistory(path, RecursionType.Full, numDisplay).ToList();

			lvChangeset.ItemsSource = changesets;

			UpdateUI(changesets[0]);
		}

		private bool ParseCommandlineArguments(out Dictionary<string, string> arguments)
		{
			bool success = true;
			arguments = new Dictionary<string, string>();
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 1; i < args.Length; i += 2 )
			{
				if (String.Equals(args[i], "-server", StringComparison.OrdinalIgnoreCase))
				{
					arguments.Add(args[i].Replace("-", ""), args[i+1]);
				}
				else if (String.Equals(args[i], "-path", StringComparison.OrdinalIgnoreCase))
				{
					arguments.Add(args[i].Replace("-", ""), args[i + 1]);
				}
				else if (String.Equals(args[i], "-numdisplay", StringComparison.OrdinalIgnoreCase))
				{
					arguments.Add(args[i].Replace("-", ""), args[i+1]);
				}
				else
				{
					success = false;
				}
			}

			return success;
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
