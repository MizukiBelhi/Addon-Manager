using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace AddonManager
{
	internal class DownloadManager
	{
		private static readonly List<DownloadQueueItem> DownloadQueue = new List<DownloadQueueItem>();

		public static bool isDownloadInProgress;

		private static string addonDownloadUrl = @"https://github.com/{0}/releases/download/{1}/{2}-{3}.{4}";
		private static string dateDownloadUrl = @"https://github.com/{0}/commits/{1}.atom";

		private static string readmeDownloadUrl = @"https://raw.githubusercontent.com/{0}/master/README.md";
		private static string readmeDownloadUrlFirst = @"https://raw.githubusercontent.com/{0}/master/{1}/README.md";

		private static string brokenAddonsDownloadUrl =
			@"https://raw.githubusercontent.com/JTosAddon/Addons/master/broken-addons.json";

		private static string addonFileName = "_{0}-{1}-{2}.{3}";

		private static NoKeepAliveWebClient webClient; //Current in-use webclient;

		private static Dictionary<string, DateTimeOffset> _usedReleases;


		/// <summary>
		/// Queues addon for download
		/// </summary>
		public static void Queue(AddonDisplayObject addon, DownloadProgressChangedEventHandler ProgressCallback = null)
		{
			DownloadQueueItem item = new DownloadQueueItem
			{
				ProgressCallback = ProgressCallback ?? Manager_DownloadProgressChanged, addon = addon
			};

			DownloadQueue.Add(item);

			ProcessQueue();
		}


		/// <summary>
		/// Processes Download Queue
		/// </summary>
		private static void ProcessQueue()
		{
			if (webClient != null && webClient.IsBusy)
				return;

			if (DownloadQueue.Count <= 0)
				return;

			ServicePointManager.SecurityProtocol = (SecurityProtocolType) 3072;

			webClient = new NoKeepAliveWebClient();
			webClient.CancelAsync();
			webClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

			AddonsObject addon = DownloadQueue.First().addon.currentDisplay.addon;

			string useSource = string.Format(addonDownloadUrl, DownloadQueue.First().addon.currentDisplay.repo,
				addon.releaseTag, addon.file, addon.fileVersion, addon.extension);

			Debug.WriteLine("Downloading from " + useSource);

			string fileName = string.Format(addonFileName, addon.file, addon.unicode, addon.fileVersion,
				addon.extension);

			Debug.WriteLine(fileName);

			webClient.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
			webClient.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
			webClient.DownloadDataCompleted += Manager_DownloadComplete;
			webClient.DownloadProgressChanged += DownloadQueue.First().ProgressCallback;
			webClient.DownloadDataAsync(new Uri(useSource));


			if (!DownloadQueue.First().addon.currentDisplay.isDownloading)
			{
				DownloadQueue.First().addon.currentDisplay.isDownloading = true;
				DownloadQueue.First().addon.tabManager.PopulateAddon(DownloadQueue.First().addon, 0, 0);
			}
		}


		/// <summary>
		/// Downloads Dependencies
		/// </summary>
		public static void DownloadDependencies(List<ManagersDependency> dependencies)
		{
			ServicePointManager.SecurityProtocol = (SecurityProtocolType) 3072;


			foreach (ManagersDependency dependency in dependencies)
			{
				Uri depdencyUri = new Uri(dependency.url);
				string fileName = Path.GetFileName(depdencyUri.LocalPath);

				using (WebClient wc = new WebClient())
				{
					wc.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
					wc.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
					wc.Headers.Add(
						"User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
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
			string useSource = string.Format(addonDownloadUrl, obj.currentDisplay.repo, addon.releaseTag, addon.file,
				addon.fileVersion, addon.extension);
			Debug.WriteLine("Checking if file exists at: " + useSource);
			try
			{
				using (WebClient testClient = new WebClient())
				{
					testClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
					ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
					Stream stream = testClient.OpenRead(useSource);
					stream.Dispose();
				}
			}
			catch (Exception ex)
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
			JsonManager.RemoveFile(MainWindow.settings.TosDataFolder,
				string.Format(addonFileName, addon.file, addon.unicode, addon.fileVersion, addon.extension));
			if (deleteFolder)
				RemoveAddonFolder(addonObj.currentDisplay.addon);

			addonObj.currentDisplay.isInstalled = false;
		}

		/// <summary>
		/// Deletes Addon
		/// </summary>
		public static void DeleteAddon(AddonObject addonObj, bool deleteFolder = true)
		{
			AddonsObject addon = addonObj.addon;
			JsonManager.RemoveFile(MainWindow.settings.TosDataFolder,
				string.Format(addonFileName, addon.file, addon.unicode, addon.fileVersion, addon.extension));
			if (deleteFolder)
				RemoveAddonFolder(addonObj.addon);

			addonObj.isInstalled = false;
		}


		/// <summary>
		/// Checks if Addon file exists
		/// </summary>
		public static bool AddonExists(AddonsObject addon, bool isIToS)
		{
			string addonFile = string.Format(addonFileName, addon.file, addon.unicode, addon.fileVersion,
				addon.extension);

			return JsonManager.FileExists(MainWindow.settings.TosDataFolder + addonFile);
		}

		/// <summary>
		/// Creates Addon folder
		/// </summary>
		private static void CreateAddonFolder(AddonsObject addon)
		{
			JsonManager.CreateFolder(MainWindow.settings.TosAddonFolder + @"\" + addon.file);
		}

		/// <summary>
		/// Removes Addon folder
		/// </summary>
		private static void RemoveAddonFolder(AddonsObject addon)
		{
			JsonManager.DeleteFolder(MainWindow.settings.TosAddonFolder + @"\" + addon.file);
		}

		/// <summary>
		/// Returns List of all installed Addons
		/// </summary>
		public static List<AddonObject> GetInstalledAddons()
		{
			var installedAddons = new List<AddonObject>();

			if (!Directory.Exists(MainWindow.settings.TosDataFolder))
				return installedAddons;

			var dirFiles = Directory.GetFiles(MainWindow.settings.TosDataFolder, "*.ipf");

			Regex rx = new Regex(@"_(.*)-(.*)-(v\d+.\d+.\d+.*).ipf", RegexOptions.Compiled | RegexOptions.IgnoreCase);

			foreach (string file in dirFiles)
			{
				string fileName = Path.GetFileName(file);
				//Debug.WriteLine("Trying: " + fileName);

				MatchCollection matches = rx.Matches(fileName);
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
							unicode = addonUnicode
						}
					};


					installedAddons.Add(addonObj);
					//Debug.WriteLine("Found Addon: " + addonFile);
				}
			}

			return installedAddons;
		}


		/// <summary>
		/// Continues Queue on download complete
		/// </summary>
		private static void Manager_DownloadComplete(object sender, DownloadDataCompletedEventArgs e)
		{
			try
			{
				//Debug.WriteLine("DownloadComplete");
				if (e.Error != null)
				{
					//there's been an error downloading the addon for whatever reason
					Debug.WriteLine(e.Error.Message);
				}
				else
				{
					AddonsObject addon = DownloadQueue.First().addon.currentDisplay.addon;
					string filePath = string.Format(addonFileName, addon.file, addon.unicode, addon.fileVersion,
						addon.extension);

					Debug.WriteLine("Writing to path: " + MainWindow.settings.TosDataFolder + filePath);

					File.WriteAllBytes(MainWindow.settings.TosDataFolder + filePath, e.Result);
					DownloadDependencies(DownloadQueue.First().addon.currentDisplay.dependencies);

					DownloadQueue.First().addon.currentDisplay.IsQueued = false;
					DownloadQueue.First().addon.currentDisplay.isInstalled = true;
					DownloadQueue.First().addon.currentDisplay.isDownloading = false;

					DownloadQueue.First().addon.tabManager.RemoveFromList(DownloadQueue.First().addon);
					DownloadQueue.First().addon.tabManager.AddToInstalledAddons(DownloadQueue.First().addon);

					CreateAddonFolder(DownloadQueue.First().addon.currentDisplay.addon);
				}

				isDownloadInProgress = false;

				DownloadQueue.Remove(DownloadQueue.First());

				ProcessQueue();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

		/// <summary>
		/// Sets isDownloadInProgress
		/// </summary>
		private static void Manager_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			isDownloadInProgress = true;
		}

		//https://raw.githubusercontent.com/repo/master/README.md
		//https://raw.githubusercontent.com/repo/master/addonName/README.md
		public static string GetReadme(AddonObject addon)
		{
			string url = string.Format(readmeDownloadUrlFirst, addon.repo, addon.addon.name.ToLower());
			Debug.WriteLine("Trying to get README from: " + url);
			try
			{
				using (WebClient testClient = new WebClient())
				{
					testClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
					ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
					testClient.Encoding = Encoding.UTF8;
					string readme = testClient.DownloadString(url);

					if (readme[0] == '4' && readme[1] == '0' && readme[2] == '4')
						return SecondReadme(addon);
					return readme;
				}
			}
			catch (Exception)
			{
				return SecondReadme(addon);
			}
		}

		private static string SecondReadme(AddonObject addon)
		{
			string url = string.Format(readmeDownloadUrl, addon.repo);
			Debug.WriteLine("Trying to get README from: " + url);

			try
			{
				using (WebClient testClient = new WebClient())
				{
					testClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
					ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
					testClient.Encoding = Encoding.UTF8;
					return testClient.DownloadString(url);
				}
			}
			catch (Exception)
			{
				return "ERROR";
			}
		}

		//Fetch version
		public static DateTimeOffset GetAddonDate(AddonObject addon)
		{
			string url = string.Format(dateDownloadUrl, addon.repo, addon.addon.releaseTag);

			if (_usedReleases == null)
				_usedReleases = new Dictionary<string, DateTimeOffset>();

			if (_usedReleases.ContainsKey(url))
				return _usedReleases[url];

			try
			{
				using (WebClient testClient = new WebClient())
				{
					testClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable);
					ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
					testClient.Encoding = Encoding.UTF8;
					Atom10FeedFormatter formatter = new Atom10FeedFormatter();

					using (XmlReader reader = XmlReader.Create(new StringReader(testClient.DownloadString(url))))
					{
						formatter.ReadFrom(reader);
					}

					foreach (SyndicationItem item in formatter.Feed.Items)
					{
						//long t = long.Parse(item.LastUpdatedTime.ToString("yyyyMMddhhmmss"));

						if (_usedReleases == null)
							_usedReleases = new Dictionary<string, DateTimeOffset>();

						_usedReleases.Add(url, item.LastUpdatedTime);

						return item.LastUpdatedTime;
					}
				}
			}
			catch (WebException e)
			{
				Debug.WriteLine(((HttpWebResponse) e.Response).StatusCode);

				if ((int) ((HttpWebResponse) e.Response).StatusCode == 429)
					return Task.Delay(5000).ContinueWith(t => GetAddonDate(addon)).Result;

				return new DateTimeOffset();
			}
			catch (Exception)
			{
				//Anything else we ignore
			}

			return new DateTimeOffset();
		}


		/// <summary>
		/// Returns json string of broken addons
		/// </summary>
		public static string GetBrokenAddons()
		{

			try
			{
				using (WebClient testClient = new WebClient())
				{
					testClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
					ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
					testClient.Encoding = Encoding.UTF8;
					return testClient.DownloadString(brokenAddonsDownloadUrl);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}

			return "";
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
			WebRequest request = base.GetWebRequest(address);
			if (request is HttpWebRequest httpWebRequest) httpWebRequest.KeepAlive = false;

			return request;
		}
	}
}