using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.MVVM;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using tfsk.Annotations;

namespace tfsk
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		public ICommand QueryHistoryCommand { get; private set; }
		public ICommand FilterCommand { get; private set; }

		private VersionControlServer Server;

		public string TFSUrl
		{
			get { return Properties.Settings.Default.TFSUrl; }
			set
			{
				Properties.Settings.Default.TFSUrl = value;
				UpdateVersionControl();
				NotifyPropertyChanged();
			}
		}

		public string FilePath { get; set; }
		public int NumDisplay { get; set; }
		public string VersionMin { get; set; }
		public string VersionMax { get; set; }
		public string[] ExcludeUsers { get; set; }
		public bool GetLatestVersion { get; set; }
		public bool NoMinVersion { get; set; }
		public string SearchKeyword { get; set; }

		private List<Changeset> _changesets;
		public List<Changeset> Changesets
		{
			get { return _changesets; }
			set
			{
				_changesets = value;
				CollectionViewSource.GetDefaultView(_changesets).Filter = ChangeSetFilter;
				NotifyPropertyChanged();
			}
		}

		private string _changesetMessage;
		public string ChangesetMessage
		{
			get { return _changesetMessage; }
			set
			{
				_changesetMessage = value; 
				NotifyPropertyChanged();
			}
		}

		private Changeset _selectedChangeset;
		public Changeset SelectedChangeset
		{
			get { return _selectedChangeset; }
			set
			{
				if (_selectedChangeset == value)
					return;

				_selectedChangeset = value;
				if (_selectedChangeset != null)
				{
					ChangesetMessage = _selectedChangeset.Comment;
					Changes = GetChangesForChangeset(_selectedChangeset.ChangesetId);
				}
				else
				{
					ChangesetMessage = "";
					Changes = null;
				}
			}
		}

		private Change[] _changes;
		public Change[] Changes
		{
			get { return _changes; }
			set
			{
				_changes = value;

				if (_changes.Length > 0)
				{
					SelectedChange = _changes[0];
				}
				NotifyPropertyChanged();
			}
		}

		private Change _selectedChange;
		public Change SelectedChange
		{
			get { return _selectedChange; }
			set
			{
				if (SelectedChange == value)
					return;

				_selectedChange = value;

				if (_selectedChange != null)
				{
					ChangeDiff = DiffItemWithPrevVersion(_selectedChange.Item);
				}
			}
		}

		private string _changeDiff;

		public string ChangeDiff
		{
			get { return _changeDiff; }
			set
			{
				_changeDiff = value; 
				NotifyPropertyChanged();
			}
		}
		
		public MainWindowViewModel()
		{
			Init();

			if (!ParseCommandlineArguments())
			{
				// request close
			}
			
			UpdateVersionControl();
			QueryHistory();
		}

		private void Init()
		{
			NoMinVersion = true;
			GetLatestVersion = true;
			NumDisplay = 100;
			QueryHistoryCommand = new RelayCommand(QueryHistory);
			FilterCommand = new RelayCommand(Filter);
		}

		#region INotifyPropertyChanged Implementation
		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		private bool ParseCommandlineArguments()
		{
			bool success = true;
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 1; i < args.Length; i += 2)
			{
				if (String.Equals(args[i], "-server", StringComparison.OrdinalIgnoreCase))
				{
					Properties.Settings.Default.TFSUrl = args[i + 1];
				}
				else if (String.Equals(args[i], "-path", StringComparison.OrdinalIgnoreCase))
				{
					FilePath = args[i + 1];
				}
				else if (String.Equals(args[i], "-numdisplay", StringComparison.OrdinalIgnoreCase))
				{
					int numDisplay;
					if (Int32.TryParse(args[i + 1], out numDisplay))
					{
						NumDisplay = numDisplay;
					}
				}
				else if (String.Equals(args[i], "-excludeUser", StringComparison.OrdinalIgnoreCase))
				{
					ExcludeUsers = args[i + 1].Split(';');
				}
				else if (String.Equals(args[i], "-version", StringComparison.OrdinalIgnoreCase))
				{
					VersionSpec[] versions = VersionSpec.Parse(args[i + 1], null);

					if (versions != null && versions.Length == 1)
					{
						VersionMax = versions[0].DisplayString;
						GetLatestVersion = false;
					}
					else if (versions != null && versions.Length == 2)
					{
						VersionMin = versions[0].DisplayString;
						VersionMax = versions[1].DisplayString;
						GetLatestVersion = false;
						NoMinVersion = false;
					}
				}
				else
				{
					success = false;
				}
			}
			/*
			if (String.IsNullOrEmpty(Properties.Settings.Default.TFSUrl))
			{
				MessageBox.Show("TFS Server URL is empty. Please set it using -server <tfsurl>");
				success = false;
			}

			if (String.IsNullOrEmpty(FilePath))
			{
				MessageBox.Show("File path is empty. Please set it using -path <file or directory>");
				success = false;
			}
			*/
			return success;
		}

		public void QueryHistory()
		{
			QueryHistoryParameters queryHistoryParameter = new QueryHistoryParameters(FilePath, RecursionType.Full);
			queryHistoryParameter.MaxResults = NumDisplay;

			if (!NoMinVersion)
			{
				queryHistoryParameter.VersionStart = VersionSpec.ParseSingleSpec(VersionMin, null);
			}

			if (!GetLatestVersion)
			{
				queryHistoryParameter.VersionEnd = VersionSpec.ParseSingleSpec(VersionMax, null);
			}

			Changesets = Server.QueryHistory(queryHistoryParameter).ToList();
		}

		public void Filter()
		{
			CollectionViewSource.GetDefaultView(Changesets).Refresh();
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
			if (changeset != null)
			{
				string[] excludeUsers = ExcludeUsers;
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

				if (!String.IsNullOrEmpty(SearchKeyword))
				{
					Match match = Regex.Match(changeset.Comment, SearchKeyword, RegexOptions.IgnoreCase);
					if (!match.Success)
					{
						display = false;
					}
				}
			}
			return display;
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