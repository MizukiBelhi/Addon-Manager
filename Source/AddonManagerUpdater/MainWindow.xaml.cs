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
using System.Net;
using System.Diagnostics;

namespace AddonManagerUpdater
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		Dictionary<string, string> arguments = new Dictionary<string, string>();

		string currentVersion = "999";

		int numFiles = 0;
		int numFilesFinished = 0;
		List<string> FileList = new List<string>();

		public MainWindow()
		{
			InitializeComponent();

			//Debugger.Launch();

			string[] args = Environment.GetCommandLineArgs();


			if (args.Length < 3)
			{
				//No arguments supplied cannot do anything
				this.Close();
			}
			else
			{
				for(int index = 1; index < args.Length; index += 2)
				{
					string arg = args[index].Replace("-", "");
					arguments.Add(arg, args[index + 1]);
					Debug.WriteLine(arg + " = " + args[index + 1]);
				}
			}

			//process arguments
			if(arguments.ContainsKey("version"))
			{
				currentVersion = arguments["version"];
			}

			updateLabel.Content = "Gathering Update Information.";

			int ver = HasNewVersion();
			if(ver.ToString() == currentVersion)
			{
				Task.Delay(2000).ContinueWith(t => CloseProgram());
			}
			else
			{
				if(ver > Int32.Parse(currentVersion))
				{
					infoLabel.Content = "Found new version "+ver.ToString();
					updateLabel.Content = "Updating.";

					FileList = GetUpdateFileList();
					if(FileList.Count == 0)
					{
						infoLabel.Content = "Could not retrieve file info.";
						Task.Delay(2000).ContinueWith(t => CloseProgram());
						return;
					}

					progress.Maximum = FileList.Count;
					numFiles = FileList.Count;

					infoLabel.Content = "Waiting for AddonManager to close...";

					Process[] proc = Process.GetProcessesByName("AddonManager");
					if (proc.Length > 0)
					{
						Process process = proc[0];
						//we kill it just in case..
						process.Kill();
						while (!process.HasExited)
						{
							System.Threading.Thread.Sleep(100);
						}
					}

					updateLabel.Content = "Updating. (0/"+numFiles.ToString()+")";

					ProcessDownloadQueue();
				}
			}
		}

		public void CloseProgram()
		{
			Environment.Exit(0);
		}

		string currentFileName = "";

		//https://github.com/MizukiBelhi/Addon-Manager/raw/master/bin/fileName
		public void ProcessDownloadQueue()
		{
			string file = FileList.First();

			string fileS = System.Text.RegularExpressions.Regex.Replace(file, @"\t|\n|\r", "");
			Uri fileUri = new Uri("https://github.com/MizukiBelhi/Addon-Manager/raw/master/bin/" + fileS);
			//Uri fileUri = new Uri("https://speed.hetzner.de/100MB.bin");

			currentFileName = fileS;

			try
			{
				ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
				using (WebClient wc = new WebClient())
				{
					wc.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
					wc.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
					wc.DownloadProgressChanged += OnDownloadUpdate;
					wc.DownloadFileCompleted += OnDownloadFinished;
					wc.DownloadFileAsync(fileUri, Environment.CurrentDirectory + "\\" + fileS);
				}

			}
			catch (Exception)
			{

			}
		}

		
		
		static readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
		public static string FormatSize(Int64 bytes)
		{
			int counter = 0;
			decimal number = (decimal)bytes;
			while (Math.Round(number / 1024) >= 1)
			{
				number = number / 1024;
				counter++;
			}

			return string.Format("{0:n1}{1}", number, suffixes[counter]);
		}

		private void OnDownloadFinished(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{

			numFilesFinished++;

			updateLabel.Content = "Updating. ("+ numFilesFinished.ToString()+" / " + numFiles.ToString() + ")";

			if (numFilesFinished >= numFiles)
			{
				Process.Start("AddonManager.exe");

				CloseProgram();
			}
			else
			{
				FileList.Remove(FileList.First());
				ProcessDownloadQueue();
			}
		}

		private void OnDownloadUpdate(object sender, DownloadProgressChangedEventArgs e)
		{
			infoLabel.Content = currentFileName+" - "+FormatSize(e.BytesReceived)+"/"+FormatSize(e.TotalBytesToReceive);

			progress.Value = e.BytesReceived;
			progress.Maximum = e.TotalBytesToReceive;
		}

		public int HasNewVersion()
		{
			//https://raw.githubusercontent.com/MizukiBelhi/Addon-Manager/master/version
			string ver = currentVersion;

			try
			{

				using (WebClient wc = new WebClient())
				{
					wc.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
					wc.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
					ver = wc.DownloadString("https://raw.githubusercontent.com/MizukiBelhi/Addon-Manager/master/version");
				}

				
				return Int32.Parse(ver);

			}
			catch (Exception)
			{
				infoLabel.Content = "Could not retrieve version info.";
			}

			return Int32.Parse(ver);
		}

		//gets a list of files that need to be updated
		public List<string> GetUpdateFileList()
		{
			//https://raw.githubusercontent.com/MizukiBelhi/Addon-Manager/master/updatelist
			string files = "";

			List<string> fileList = new List<string>();

			try
			{

				using (WebClient wc = new WebClient())
				{
					wc.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
					wc.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
					files = wc.DownloadString("https://raw.githubusercontent.com/MizukiBelhi/Addon-Manager/master/updatelist");
				}


				string[] fileSep = files.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

				fileList = fileSep.ToList();
				return fileList;

			}
			catch (Exception)
			{
				infoLabel.Content = "Could not retrieve file info.";
			}

			return fileList;
		}
	}
}
