using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tfsk
{
	public class CommandlineArgument
	{
		public string FilePath { get; set; }
		public int NumDisplay { get; set; }
		public string ExcludeUsers { get; set; }
		public string VersionMin { get; set; }
		public string VersionMax { get; set; }
		public bool GetLatestVersion { get; set; }
		public bool NoMinVersion { get; set; }

		public CommandlineArgument()
		{
			NumDisplay = 100;
			NoMinVersion = true;
			GetLatestVersion = true;
		}

	}
}
