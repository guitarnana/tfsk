using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


// https://www.nuget.org/packages/Microsoft.VisualStudio.Services.InteractiveClient/
using Microsoft.VisualStudio.Services.Client;

// https://www.nuget.org/packages/Microsoft.VisualStudio.Services.Client/
using Microsoft.VisualStudio.Services.Common;

// https://www.nuget.org/packages/Microsoft.TeamFoundationServer.Client/
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Net;

using Microsoft.TeamFoundation.VersionControl.Client;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.VersionControl.Common;
using System.IO;

namespace tfsk
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			var tfsUrl = "http://sqlbuvsts01:8080/main";
			// Get a reference to our Team Foundation Server. 
			TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(tfsUrl));

			// Get a reference to Version Control. 
			VersionControlServer versionControl = tpc.GetService<VersionControlServer>();

			List<Changeset> changesets = versionControl.QueryHistory(@"$/Developer/jarupatj", RecursionType.Full, 10).ToList();

			lvChangeset.ItemsSource = changesets;

			Changeset changeset = changesets[0];
			tbChangeComment.Text = changesets[0].Comment;

			StringBuilder strBuilder = new StringBuilder();

			Change[] changes = versionControl.GetChangesForChangeset(changeset.ChangesetId, false, 10, null);
			foreach (Change change in changes)
			{
				Item item = change.Item;
				strBuilder.AppendFormat("{0}\n", DiffItemWithPrevVersion(versionControl, item));
			}
			tbChangeDiff.Text = strBuilder.ToString();
		}

		private string DiffItemWithPrevVersion(VersionControlServer versionControl, Item item)
		{
			// Get previous version item
			//
			Item prevItem = versionControl.GetItem(item.ItemId, item.ChangesetId - 1);

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
			return sr.ReadToEnd();
		}
	}
}
