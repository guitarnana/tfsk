using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace tfsk
{
	class VersionControl
	{
		public VersionControlServer Server { get; set; }
		public string FilePath { get; set; }
		public int NumDisplay { get; set; }
		public VersionSpec VersionMin { get; set; }
		public VersionSpec VersionMax { get; set; }
		public string[] ExcludeUsers { get; set; }
		public bool GetLatestVersion { get; set; }
		public bool NoMinVersion { get; set; }

		public VersionControl()
		{
			NoMinVersion = true;
			GetLatestVersion = true;
			NumDisplay = 100;
		}

		public List<Changeset> QueryChangeset()
		{
			QueryHistoryParameters queryHistoryParameter = new QueryHistoryParameters(FilePath, RecursionType.Full);
			queryHistoryParameter.MaxResults = NumDisplay;

			if (!NoMinVersion)
			{
				queryHistoryParameter.VersionStart = VersionMin;
			}

			if (!GetLatestVersion)
			{
				queryHistoryParameter.VersionEnd = VersionMax;
			}

			List<Changeset> changesets = Server.QueryHistory(queryHistoryParameter).ToList();
			return changesets;
		}

		public void UpdateVersionControl()
		{
			TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(Properties.Settings.Default.TFSUrl));
			Server = tpc.GetService<VersionControlServer>();
		}

		public Change[] GetChangesForChangeset(int changsetId)
		{
			return Server.GetChangesForChangeset(
				changsetId,
				false, // includeDownloadInfo
				Int32.MaxValue, // number of items to return
				null); // return from start of this changeset
		}

		public string DiffItemWithPrevVersion(Item item)
		{
			string diffStr = "";

			// Get previous version item
			//
			Item prevItem = Server.GetItem(item.ItemId, item.ChangesetId - 1);
			if (prevItem != null)
			{
				DiffItemVersionedFile curFile = new DiffItemVersionedFile(Server, item.ItemId, item.ChangesetId, null);
				DiffItemVersionedFile prevFile = new DiffItemVersionedFile(Server, prevItem.ItemId, prevItem.ChangesetId, null);

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

				Difference.DiffFiles(Server, prevFile, curFile, options, prevItem.ServerItem, true);

				// Move to the beginning of the stream for reading.
				memStream.Seek(0, SeekOrigin.Begin);

				StreamReader sr = new StreamReader(memStream);
				diffStr = sr.ReadToEnd();
			}

			return diffStr;
		}
	}
}
