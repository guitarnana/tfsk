using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace tfsk
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show(String.Format("Message: {0}\n Stack:\n{1}",e.Exception.Message, e.Exception.StackTrace), "Error");
			e.Handled = true;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			CommandlineArgument arguments;
			if (ParseCommandlineArguments(out arguments))
			{
				MainWindowViewModel vm = new MainWindowViewModel(arguments);
				MainWindow window = new MainWindow {DataContext = vm};
				window.Show();
			}
			else
			{
				UsageWindow usageWindow = new UsageWindow();
				usageWindow.Show();
			}
		}

		protected override void OnExit(ExitEventArgs e)
		{
			tfsk.Properties.Settings.Default.Save();

			base.OnExit(e);
		}

		private bool ParseCommandlineArguments(out CommandlineArgument arguments)
		{
			arguments = new CommandlineArgument();
			bool success = true;
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 1; i < args.Length; i += 2)
			{
				if (String.Equals(args[i], "-server", StringComparison.OrdinalIgnoreCase))
				{
					tfsk.Properties.Settings.Default.TFSUrl = args[i + 1];
				}
				else if (String.Equals(args[i], "-path", StringComparison.OrdinalIgnoreCase))
				{
					arguments.FilePath = args[i + 1];
				}
				else if (String.Equals(args[i], "-numdisplay", StringComparison.OrdinalIgnoreCase))
				{
					int numDisplay;
					if (Int32.TryParse(args[i + 1], out numDisplay))
					{
						arguments.NumDisplay = numDisplay;
					}
				}
				else if (String.Equals(args[i], "-excludeUser", StringComparison.OrdinalIgnoreCase))
				{
					arguments.ExcludeUsers = args[i + 1];
				}
				else if (String.Equals(args[i], "-version", StringComparison.OrdinalIgnoreCase))
				{
					VersionSpec[] versions = VersionSpec.Parse(args[i + 1], null);

					if (versions != null && versions.Length == 1)
					{
						arguments.VersionMax = versions[0].DisplayString;
						arguments.GetLatestVersion = false;
					}
					else if (versions != null && versions.Length == 2)
					{
						arguments.VersionMin = versions[0].DisplayString;
						arguments.VersionMax = versions[1].DisplayString;
						arguments.GetLatestVersion = false;
						arguments.NoMinVersion = false;
					}
				}
				else
				{
					success = false;
				}
			}

			if (String.IsNullOrEmpty(tfsk.Properties.Settings.Default.TFSUrl))
			{
				MessageBox.Show("TFS Server URL is empty. Please set it using -server <tfsurl>");
				success = false;
			}

			if (String.IsNullOrEmpty(arguments.FilePath))
			{
				MessageBox.Show("File path is empty. Please set it using -path <file or directory>");
				success = false;
			}

			return success;
		}
	}
}
