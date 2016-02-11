using System;

namespace tfsk
{
	public class CommandlineArgument
	{
		public const int DefaultNumDisplay = 100;

		public string FilePath { get; set; }
		public int NumDisplay { get; set; }
		public string ExcludeUsers { get; set; }
		public string VersionMin { get; set; }
		public string VersionMax { get; set; }
		public bool GetLatestVersion { get; set; }
		public bool NoMinVersion { get; set; }

		public CommandlineArgument()
		{
			NumDisplay = DefaultNumDisplay;
			NoMinVersion = true;
			GetLatestVersion = true;
		}

	}
}
