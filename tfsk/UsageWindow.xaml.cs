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
using System.Windows.Shapes;

namespace tfsk
{
	/// <summary>
	/// Interaction logic for UsageWindow.xaml
	/// </summary>
	public partial class UsageWindow : Window
	{
		public UsageWindow()
		{
			InitializeComponent();

			string usage = @"Usage: 
	tfsk.exe -path <path> [-server <TeamProjectCollectionUrl>] [-numdisplay <num history>]
		[-excludeUser <ExcludeUsers>] [-version <versionspec>]

Path:
	Path to a directory or a file. The path can be TFS server path or local enlistment path.

TFS Url:
	Url to TFS team project collection. 

Num History:
	Number of history returned from version control. Default value is 100.

Exclude Users:
	Username separated by ;

Versionspec:
	Date/Time		D""any .Net Framework-supported format""
				or any of the date formats of the local machine
	Changeset number	Cnnnnnn
	Label			Llabelname
	Latest version		T
	Workspace		Wworkspacename;workspaceowner

	Specifies one of the following limits on the history data:
		- The maximum version.
		- The minimum and the maximum versions using the range ~ syntax.";

			tbUsage.Text = usage;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
