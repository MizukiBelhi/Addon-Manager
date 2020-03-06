using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using System.Net;

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


		public string TosAddonFolder { get; set; }
		public string TosReleaseFolder { get; set; }
		public string TosDataFolder { get; set; }
		public string TosLuaFolder { get; set; }

		private int currentAppVersion = 103;

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
			};

			SettingsStructure _jsonSettings = JsonManager.LoadFile<SettingsStructure>("settings.json");

			if(_jsonSettings == null)
			{
				//Any exception we create a new settings file
				if(JsonManager.CreateFile("settings.json", _settings) == false)
				{
					//error
				}
			}
			else
			{
				_settings = _jsonSettings;
			}


			this.InitSettings(_settings);

		}


		public bool HasNewVersion()
		{
			//https://raw.githubusercontent.com/MizukiBelhi/Addon-Manager/master/version
			string ver = currentAppVersion.ToString();

			try
			{

				using (WebClient wc = new WebClient())
				{
					wc.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
					wc.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
					ver = wc.DownloadString("https://raw.githubusercontent.com/MizukiBelhi/Addon-Manager/master/version");
				}

				Debug.WriteLine(ver);

				if (Int32.Parse(ver) > currentAppVersion)
					return true;

			}catch(Exception)
			{ }

			return false;
		}


		/// <summary>
		/// Generates ToS folder structure for easy accessability
		/// </summary>
		public void GenerateTosFolders()
		{
			if (JsonManager.DirectoryExists(TosFolder + @"\release"))
			{
				TosReleaseFolder = TosFolder + @"\release\";
			}

			if (JsonManager.DirectoryExists(TosFolder + @"\data"))
			{
				TosDataFolder = TosFolder + @"\data\";
			}

			if (JsonManager.DirectoryExists(TosFolder + @"\addons"))
			{
				TosAddonFolder = TosFolder + @"\addons\";
			}
			else
			{
			JsonManager.CreateFolder(TosFolder + @"\addons");
				TosAddonFolder = TosFolder + @"\addons\";
			}

			if (JsonManager.DirectoryExists(TosDataFolder))
			{
				TosLuaFolder = TosReleaseFolder + @"\lua\";
			}


		}


		/// <summary>
		/// Saves settings
		/// </summary>
		public void Save()
		{
			SettingsStructure _settings = new SettingsStructure
			{
				Version = currentAppVersion,
				Language = this.Language,
				TosFolder = this.TosFolder,
				Style = this.Style,
				DisplayUnknown = this.DisplayUnknown,
				FirstStart = this.FirstStart,
				IsGrouped = this.IsGrouped
			};

			JsonManager.CreateFile("settings.json", _settings);
		}


		/// <summary>
		/// Loads Settings for use
		/// </summary>
		void InitSettings(SettingsStructure set)
		{
			this.Version = currentAppVersion;
			this.Language = set.Language;
			this.TosFolder = set.TosFolder;
			this.Style = set.Style;
			this.DisplayUnknown = set.DisplayUnknown;
			this.FirstStart = set.FirstStart;
			this.IsGrouped = set.IsGrouped;


			Debug.WriteLine("Version: " + this.Version);
			Debug.WriteLine("Language: " + this.Language);
			Debug.WriteLine("Folder: " + this.TosFolder);
			Debug.WriteLine("Style: " + this.Style);
			Debug.WriteLine("DisplayUnknown: " + this.DisplayUnknown);
			Debug.WriteLine("FirstStart: " + this.FirstStart);
			Debug.WriteLine("IsGrouped: " + this.IsGrouped);
		}


		public static bool isTosRunning()
		{
			Process[] pname = Process.GetProcessesByName("Client_tos");
			if (pname.Length == 0)
				return false;
			else
				return true;
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

		public static void CloseTos()
		{
			try
			{
				Process[] proc = Process.GetProcessesByName("Client_tos");
				proc[0].Kill();
			}catch
			{ }
		}



	}

	[Serializable]
	class SettingsStructure
	{
		public int Version;
		public string TosFolder;
		public string Language;
		public string Style;
		public bool DisplayUnknown;
		public bool FirstStart;
		public bool IsGrouped;
	}
}
