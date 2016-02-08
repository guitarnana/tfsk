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
		#region Fields

		private VersionControlServer Server;
		private List<Changeset> _changesets;
		private string _changesetMessage;
		private Changeset _selectedChangeset;
		private Change[] _changes;
		private Change _selectedChange;
		private string _changeDiff;
		private string _status;
		private int _numDisplay;
		private bool _getLatestVersion;
		private bool _noMinVersion;

		private readonly BackgroundWorker queryHistoryWorker;
		#endregion

		#region Commands

		public ICommand QueryHistoryCommand { get; private set; }
		public ICommand FilterCommand { get; private set; }
		
		#endregion

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
		public string VersionMin { get; set; }
		public string VersionMax { get; set; }
		public string ExcludeUsers { get; set; }
		public string SearchKeyword { get; set; }

		public int NumDisplay
		{
			get { return _numDisplay; }
			set 
			{
				_numDisplay = (value <= 0) ? CommandlineArgument.DefaultNumDisplay : value;
			}
		}

		public string Status
		{
			get { return _status; }
			set
			{
				_status = value; 
				NotifyPropertyChanged();
			}
		}

		public bool GetLatestVersion
		{
			get { return _getLatestVersion; }
			set
			{
				_getLatestVersion = value;
				NotifyPropertyChanged();
			}
		}
		public bool NoMinVersion
		{
			get { return _noMinVersion; }
			set
			{
				_noMinVersion = value;
				NotifyPropertyChanged();
			}
		}

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

		public string ChangesetMessage
		{
			get { return _changesetMessage; }
			set
			{
				_changesetMessage = value; 
				NotifyPropertyChanged();
			}
		}

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

				NotifyPropertyChanged();
			}
		}

		public Change[] Changes
		{
			get { return _changes; }
			set
			{
				if (_changes == value)
					return;

				_changes = value;
				if (_changes != null)
				{
					if (_changes.Length > 0)
					{
						SelectedChange = _changes[0];
					}
				}

				NotifyPropertyChanged();
			}
		}

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
				else
				{
					ChangeDiff = "";
				}

				NotifyPropertyChanged();
			}
		}

		public string ChangeDiff
		{
			get { return _changeDiff; }
			set
			{
				_changeDiff = value; 
				NotifyPropertyChanged();
			}
		}
		
		#region Constructor
		public MainWindowViewModel(CommandlineArgument argument)
		{
			QueryHistoryCommand = new RelayCommand(QueryHistory);
			FilterCommand = new RelayCommand(Filter);

			FilePath = argument.FilePath;
			NumDisplay = argument.NumDisplay;
			ExcludeUsers = argument.ExcludeUsers;
			VersionMin = argument.VersionMin;
			VersionMax = argument.VersionMax;
			GetLatestVersion = argument.GetLatestVersion;
			NoMinVersion = argument.NoMinVersion;
			Status = "Ready";

			queryHistoryWorker = new BackgroundWorker();
			queryHistoryWorker.DoWork += QueryHistory_DoWork;
			queryHistoryWorker.RunWorkerCompleted += QueryHistory_RunWorkerComplete;

			UpdateVersionControl();
			QueryHistory();
		}
		
		#endregion

		#region INotifyPropertyChanged Implementation
		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		#region Command handler
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

			// Query history if we are not already querying
			//
			if (!queryHistoryWorker.IsBusy)
			{
				Status = "Querying History";
				queryHistoryWorker.RunWorkerAsync(queryHistoryParameter);
			}
		}

		public void Filter()
		{
			CollectionViewSource.GetDefaultView(Changesets).Refresh();
		}
		
		#endregion
		
		#region Helper functions
		
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
				if (ExcludeUsers != null)
				{
					string[] excludeUsers = ExcludeUsers.Split(';');
					foreach (string user in excludeUsers)
					{
						if (changeset.OwnerDisplayName.Equals(user))
						{
							display = false;
						}
					}
				}

				if (!String.IsNullOrEmpty(SearchKeyword) && 
					!String.IsNullOrEmpty(changeset.Comment))
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
			Status = String.Format("Query changes for changeset {0}", changsetId);

			Change[] changes = Server.GetChangesForChangeset(
				changsetId,
				false, // includeDownloadInfo
				Int32.MaxValue, // number of items to return
				null); // return from start of this changeset

			Status = "Ready";

			return changes;
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

		private void QueryHistory_DoWork(object sender, DoWorkEventArgs e)
		{
			QueryHistoryParameters param = e.Argument as QueryHistoryParameters;
			e.Result = Server.QueryHistory(param).ToList();
		}

		private void QueryHistory_RunWorkerComplete(object sender, RunWorkerCompletedEventArgs e)
		{
			Status = "Ready";
			Changesets = e.Result as List<Changeset>;
		}

		#endregion

	}
}