using System.Windows;

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

			/*
			if (ParseCommandlineArguments())
			{
				versionControl.UpdateVersionControl();

				List<Changeset> changesets = versionControl.QueryChangeset();
				UpdateChangesetSource(changesets);
				UpdateUI(changesets[0]);
			}
			else
			{
				UsageWindow usageWindow = new UsageWindow();
				usageWindow.Show();
				Close();
			}*/
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Properties.Settings.Default.Save();
		}
	}
}
