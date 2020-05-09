using System;
using System.Diagnostics;
using System.Net;
using System.Net.Cache;
using System.Threading;
using System.Windows.Threading;

namespace AddonManager
{
	public class Settings
	{
		public int Version { get; private set; }
		public string Language { get; set; }
		public string TosFolder { get; set; }
		public string Style { get; set; }
		public bool DisplayUnknown { get; set; }
		public bool FirstStart { get; set; }
		public bool IsGrouped { get; set; }
		public bool LoadDates { get; set; }


		public string TosAddonFolder { get; set; }
		public string TosReleaseFolder { get; set; }
		public string TosDataFolder { get; set; }
		public string TosLuaFolder { get; set; }

		private const int currentAppVersion = 105;

		/// <summary>
		/// Loads JSON Setting file from %programfolder%
		/// </summary>
		public void LoadSettings()
		{
			SettingsStructure _settings = new SettingsStructure
			{
				//defaults
				Version = currentAppVersion,
				Language = "en",
				TosFolder = @"C:\Program Files (x86)\Steam\steamapps\common\TreeOfSavior\",
				Style = "default",
				DisplayUnknown = false,
				FirstStart = true,
				IsGrouped = true,
				LoadDates = false
			};

			SettingsStructure _jsonSettings = JsonManager.LoadFile<SettingsStructure>("settings.json");

			if (_jsonSettings == null)
			{
				//Any exception we create a new settings file
				if (JsonManager.CreateFile("settings.json", _settings) == false)
				{
					CauseError("There was an issue creating the settings file.");
					return;
				}
			}
			else
			{
				_settings = _jsonSettings;
			}


			InitSettings(_settings);
		}


		public int GetVersion()
		{
			return currentAppVersion;
		}

		public bool HasNewVersion()
		{
			//https://raw.githubusercontent.com/MizukiBelhi/Addon-Manager/master/version

			try
			{
				string ver;// = currentAppVersion.ToString();
				using (WebClient wc = new WebClient())
				{
					wc.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
					wc.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
					wc.Headers.Add(
						"User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
					ver = wc.DownloadString(
						"https://raw.githubusercontent.com/MizukiBelhi/Addon-Manager/master/version");
				}

				Debug.WriteLine("Git Version: " + ver);

				if (int.Parse(ver) > currentAppVersion)
					return true;
			}
			catch (Exception)
			{
			}

			return false;
		}


		/// <summary>
		/// Generates ToS folder structure for easy accessibility
		/// </summary>
		public void GenerateTosFolders()
		{
			if (JsonManager.DirectoryExists(TosFolder + @"\release")) TosReleaseFolder = TosFolder + @"\release\";

			if (JsonManager.DirectoryExists(TosFolder + @"\data")) TosDataFolder = TosFolder + @"\data\";

			if (JsonManager.DirectoryExists(TosFolder + @"\addons"))
			{
				TosAddonFolder = TosFolder + @"\addons\";
			}
			else
			{
				JsonManager.CreateFolder(TosFolder + @"\addons");
				TosAddonFolder = TosFolder + @"\addons\";
			}

			if (JsonManager.DirectoryExists(TosDataFolder)) TosLuaFolder = TosReleaseFolder + @"\lua\";
		}


		/// <summary>
		/// Saves settings
		/// </summary>
		public void Save()
		{
			SettingsStructure _settings = new SettingsStructure
			{
				Version = currentAppVersion,
				Language = Language,
				TosFolder = TosFolder,
				Style = Style,
				DisplayUnknown = DisplayUnknown,
				FirstStart = FirstStart,
				IsGrouped = IsGrouped,
				LoadDates = LoadDates
			};

			if (JsonManager.CreateFile("settings.json", _settings) == false)
				CauseError("There was an issue saving settings.");
		}


		/// <summary>
		/// Loads Settings for use
		/// </summary>
		private void InitSettings(SettingsStructure set)
		{
			if (set.Version < currentAppVersion)
			{
				Debug.WriteLine("Setting Version: "+ set.Version);
				//New Version detected.
				ShowChangelog();
			}

			Version = currentAppVersion;
			Language = set.Language;
			TosFolder = set.TosFolder;
			Style = set.Style;
			DisplayUnknown = set.DisplayUnknown;
			FirstStart = set.FirstStart;
			IsGrouped = set.IsGrouped;
			LoadDates = set.LoadDates;


			Debug.WriteLine("Version: " + Version);
			Debug.WriteLine("Language: " + Language);
			Debug.WriteLine("Folder: " + TosFolder);
			Debug.WriteLine("Style: " + Style);
			Debug.WriteLine("DisplayUnknown: " + DisplayUnknown);
			Debug.WriteLine("FirstStart: " + FirstStart);
			Debug.WriteLine("IsGrouped: " + IsGrouped);
			Debug.WriteLine("LoadDates: " + LoadDates);

			//Also immediately save them so we don't get the changelog twice
			Save();
		}


		public static bool isTosRunning()
		{
			var pname = Process.GetProcessesByName("Client_tos");
			return pname.Length != 0;
		}

		public static void CauseError(string msg)
		{
			errorMessage handler = new errorMessage();
			handler.Error(msg);
			handler.ShowDialog();
		}

		public static void CauseError(string msg, string button, ErrorButtonCallback cb)
		{
			errorMessage handler = new errorMessage();
			handler.Error(msg);
			handler.ShowButton(button, cb);
			handler.ShowDialog();
		}

		public static void CauseError(string msg, string title, string firstButton, string secondButton,
			ErrorButtonCallback cb)
		{
			errorMessage handler = new errorMessage();
			handler.Error(msg);
			handler.ShowButton(secondButton, cb);
			handler.ChangeFirstButton(firstButton);
			handler.ChangeTitle(title);
			handler.ShowDialog();
		}

		//Quick way to show the changelog on a different thread
		// https://stackoverflow.com/a/1111485
		//
		//This way program start does not get moved to after the user closes it.
		public void ShowChangelog()
		{
			Thread newWindowThread = new Thread(_ShowChangelog);
			newWindowThread.SetApartmentState(ApartmentState.STA);
			newWindowThread.IsBackground = true;
			newWindowThread.Start();
		}

		private void _ShowChangelog()
		{
			Changelog chl = new Changelog();
			chl.DisplayChangelog();
			chl.Show();
			Dispatcher.Run();
		}

		public static void CloseTos()
		{
			try
			{
				var proc = Process.GetProcessesByName("Client_tos");
				proc[0].Kill();
			}
			catch
			{
			}
		}
	}

	[Serializable]
	internal class SettingsStructure
	{
		public int Version;
		public string TosFolder;
		public string Language;
		public string Style;
		public bool DisplayUnknown;
		public bool FirstStart;
		public bool IsGrouped;
		public bool LoadDates;
	}
}