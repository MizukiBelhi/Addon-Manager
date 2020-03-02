using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Cache;

namespace AddonManager
{
	class DownloadManager
	{
		static List<DownloadQueueItem> downloadQueue = new List<DownloadQueueItem>();

		public static bool isDownloadInProgress = false;

		static string addonDownloadUrl = @"https://github.com/{0}/releases/download/{1}/{2}-{3}.{4}";

		static string readmeDownloadUrl = @"https://raw.githubusercontent.com/{0}/master/README.md";

		static string addonFileName = "_{0}-{1}-{2}.{3}";

		private static NoKeepAliveWebClient webClient; //Current in-use webclient;


		/// <summary>
		/// Queues addon for download
		/// </summary>
		public static void Queue(AddonDisplayObject addon, DownloadProgressChangedEventHandler ProgressCallback = null)
		{
			DownloadQueueItem item = new DownloadQueueItem();

			if (ProgressCallback == null)
				item.ProgressCallback = Manager_DownloadProgressChanged;
			else
				item.ProgressCallback = ProgressCallback;

			item.addon = addon;
			

			downloadQueue.Add(item);

			ProcessQueue();
		}


		/// <summary>
		/// Processes Download QUeue
		/// </summary>
		static void ProcessQueue()
		{
			Debug.WriteLine("ProcessQueue()");
			if (webClient != null && webClient.IsBusy)
				return;


			Debug.WriteLine("Count()");
			if (downloadQueue.Count <= 0)
				return;

			ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

			Debug.WriteLine("BusyCheck()");
			if (webClient != null && !webClient.IsBusy)
			{
				Debug.WriteLine("Disposing webclient");
				//webClient.Dispose();
				//webClient = null;
			}

			if (webClient == null)
			{
				Debug.WriteLine("CreateWebClient()");


			}

			webClient = new NoKeepAliveWebClient();
			webClient.CancelAsync();
			//using (webClient)
			//{
				AddonsObject addon = downloadQueue.First().addon.currentDisplay.addon;
			   // ttps://github.com/" + source.repo + "/releases/download/" + addon.releaseTag + "/" + addon.file + "-" + addon.fileVersion + "." + addon.extension
				string useSource = string.Format(addonDownloadUrl, downloadQueue.First().addon.currentDisplay.repo ,addon.releaseTag ,addon.file, addon.fileVersion, addon.extension);

				Debug.WriteLine("Downloading from "+useSource.ToString());

				string fileName = string.Format(addonFileName, addon.file, addon.unicode, addon.fileVersion, addon.extension);

				Debug.WriteLine(fileName);

				webClient.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
				webClient.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
				webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler((sender, e) => Manager_DownloadComplete(sender, e));
				webClient.DownloadProgressChanged += downloadQueue.First().ProgressCallback;
				webClient.DownloadDataAsync(new System.Uri(useSource));
				//wc.DownloadFileAsync(
				//	new System.Uri(useSource),
				//	MainWindow.settings.TosDataFolder + fileName
				//);
			//}

			if (!downloadQueue.First().addon.currentDisplay.isDownloading)
			{
				downloadQueue.First().addon.currentDisplay.isDownloading = true;
				downloadQueue.First().addon.tabManager.PopulateAddon(downloadQueue.First().addon, 0, 0);
			}
		}


		/// <summary>
		/// Downloads Dependencies
		/// </summary>
		public static void DownloadDependencies(List<ManagersDependency> dependencies)
		{
			ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;


			foreach (ManagersDependency dependency in dependencies)
			{
				Uri depdencyUri = new Uri(dependency.url);
				string fileName = Path.GetFileName(depdencyUri.LocalPath);

				using (WebClient wc = new WebClient())
				{
					wc.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
					wc.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
					wc.DownloadFile(depdencyUri, MainWindow.settings.TosLuaFolder + fileName);
				}
			}
		}

		
		/// <summary>
		/// Checks if file exists on the requested uri
		/// </summary>
		public static bool AddonFileExists(AddonDisplayObject obj)
		{
			bool exists = true;
			AddonsObject addon = obj.currentDisplay.addon;
			string useSource = string.Format(addonDownloadUrl, obj.currentDisplay.repo, addon.releaseTag, addon.file, addon.fileVersion, addon.extension);
			Debug.WriteLine("Checking if file exists at: " + useSource.ToString());
			try
			{
				using (WebClient testClient = new WebClient())
				{
					ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
					Stream stream = testClient.OpenRead(useSource);
					stream.Dispose();
				}
			}
			catch(Exception ex)
			{
				Debug.WriteLine(ex.Message);
				exists = false;
			}

			return exists;

		}

		/// <summary>
		/// Checks if file exists on the requested uri
		/// </summary>
		public static bool AddonFileExists(AddonObject obj)
		{
			AddonDisplayObject disp = new AddonDisplayObject
			{
				currentDisplay = obj
			};

			return AddonFileExists(disp);
		}


		/// <summary>
		/// Unused
		/// </summary>
		public static void UpdateAddon(AddonDisplayObject oldAddon, AddonDisplayObject newAddon)
		{
			DeleteAddon(oldAddon, false);
			Queue(newAddon);
		}


		/// <summary>
		/// Deletes Addon
		/// </summary>
		public static void DeleteAddon(AddonDisplayObject addonObj, bool deleteFolder = true)
		{
			AddonsObject addon = addonObj.currentDisplay.addon;
			JsonManager.RemoveFile(MainWindow.settings.TosDataFolder, string.Format(addonFileName, addon.file, addon.unicode, addon.fileVersion, addon.extension) );
			if (deleteFolder)
				RemoveAddonFolder(addonObj.currentDisplay.addon);

			addonObj.currentDisplay.isInstalled = false;

			//addonObj.tabManager.PopulateAddon(addonObj, 0, 0);
		}


		/// <summary>
		/// Checks if Addon file exists
		/// </summary>
		public static bool AddonExists(AddonsObject addon, bool isIToS)
		{
			string addonFile = string.Format(addonFileName, addon.file, addon.unicode, addon.fileVersion, addon.extension);

			return JsonManager.FileExists(MainWindow.settings.TosDataFolder + addonFile);
		}

		/// <summary>
		/// Creates Addon folder
		/// </summary>
		static void CreateAddonFolder(AddonsObject addon)
		{
			JsonManager.CreateFolder(MainWindow.settings.TosAddonFolder + @"\"+addon.file);
		}

		/// <summary>
		/// Removes Addon folder
		/// </summary>
		static void RemoveAddonFolder(AddonsObject addon)
		{
			JsonManager.DeleteFolder(MainWindow.settings.TosAddonFolder + @"\" + addon.file);
		}

		/// <summary>
		/// Returns List of all installed Addons
		/// </summary>
		public static List<AddonObject> GetInstalledAddons()
		{
			List<AddonObject> installedAddons = new List<AddonObject>();

			if (!Directory.Exists(MainWindow.settings.TosDataFolder))
				return installedAddons;

			string[] dirFiles = Directory.GetFiles(MainWindow.settings.TosDataFolder, "*.ipf");

			Regex rx = new Regex(@"_(.*)-(.*)-(v\d+.\d+.\d+.*).ipf", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			MatchCollection matches;

			foreach (string file in dirFiles)
			{
				string fileName = Path.GetFileName(file);
				//Debug.WriteLine("Trying: " + fileName);

				matches = rx.Matches(fileName);
				if (matches.Count > 0)
				{
					GroupCollection group = matches[0].Groups;

					string addonFile = group[1].Value;
					string addonUnicode = group[2].Value;
					string addonVersion = group[3].Value;
					//Debug.WriteLine("found itos "+isitos);


					AddonObject addonObj = new AddonObject
					{
						isInstalled = true,
						addon = new AddonsObject
						{
							extension = "ipf",
							file = addonFile,
							fileVersion = addonVersion,
							unicode = addonUnicode,
						}
					};


					installedAddons.Add(addonObj);
					//Debug.WriteLine("Found Addon: " + addonFile);
				}
				else
				{
					//Debug.WriteLine("No matches");
				}
			}

			return installedAddons;
		}


		/// <summary>
		/// Continues Queue on download complete
		/// </summary>
		static void Manager_DownloadComplete(object sender, DownloadDataCompletedEventArgs e)
		{
			try
			{
				//Debug.WriteLine("DownloadComplete");
				if (e.Error != null)
				{
					//there's been an error downloading the addon for whatever reason
					//we have to remove the file because it still gets created by downloadfileasync
					Debug.WriteLine(e.Error.Message);
					//AddonsObject addon = downloadQueue.First().addon.currentDisplay.addon;
					//JsonManager.RemoveFile(MainWindow.settings.TosDataFolder, string.Format(addonFileName, addon.file, addon.unicode, addon.fileVersion, addon.extension));

				}
				else
				{
					AddonsObject addon = downloadQueue.First().addon.currentDisplay.addon;
					string filePath = string.Format(addonFileName, addon.file, addon.unicode, addon.fileVersion, addon.extension);

					Debug.WriteLine("Writing to path: " + MainWindow.settings.TosDataFolder + filePath);

					File.WriteAllBytes(MainWindow.settings.TosDataFolder + filePath, e.Result);
					DownloadDependencies(downloadQueue.First().addon.currentDisplay.dependencies);

					downloadQueue.First().addon.currentDisplay.IsQueued = false;
					downloadQueue.First().addon.currentDisplay.isInstalled = true;
					downloadQueue.First().addon.currentDisplay.isDownloading = false;

					//downloadQueue.First().addon.tabManager.PopulateAddon(downloadQueue.First().addon, 0, 0);
					downloadQueue.First().addon.tabManager.RemoveFromList(downloadQueue.First().addon);
					downloadQueue.First().addon.tabManager.AddToInstalledAddons(downloadQueue.First().addon);

					CreateAddonFolder(downloadQueue.First().addon.currentDisplay.addon);
				}

				isDownloadInProgress = false;

				downloadQueue.Remove(downloadQueue.First());

				//webClient.CancelAsync();
				//webClient.Dispose();

				ProcessQueue();
			}catch(Exception ex)
			{
				Debug.WriteLine(ex.Message.ToString());
			}
		}

		/// <summary>
		/// Sets isDownloadInProgress
		/// </summary>
		static void Manager_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			isDownloadInProgress = true;
		}

		//https://raw.githubusercontent.com/MizukiBelhi/ExtendedUI/master/README.md
		public static string GetReadme(AddonObject addon)
		{
			string url = string.Format(readmeDownloadUrl, addon.repo);
			Debug.WriteLine("Trying to get README from: " + url.ToString());

			try
			{
				using (WebClient testClient = new WebClient())
				{
					ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
					return testClient.DownloadString(url);
				}
			}
			catch (Exception ex)
			{
				return "ERROR";
			}
		}



		public class DownloadQueueItem
		{
			public AddonDisplayObject addon;
			public DownloadProgressChangedEventHandler ProgressCallback;
		}

	}
}

namespace AddonManager
{
	public class NoKeepAliveWebClient : WebClient
	{
		protected override WebRequest GetWebRequest(Uri address)
		{
			var request = base.GetWebRequest(address);
			if (request is HttpWebRequest)
			{
				((HttpWebRequest)request).KeepAlive = false;
			}

			return request;
		}
	}
}