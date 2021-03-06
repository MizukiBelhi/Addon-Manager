﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Script.Serialization;

namespace AddonManager
{
	internal static class Language
	{
		private static Dictionary<string, LanguageDataObject> translationData;

		public static string CurrentLanguage { get; set; }


		/// <summary>
		/// Loads JSON language files from %programfolder%/lang/
		/// </summary>
		public static void Init()
		{
			translationData = new Dictionary<string, LanguageDataObject>();

			//Preload language
			LoadLanguageResource("en.json");
			LoadLanguageResource("es.json");
			LoadLanguageResource("fr.json");
			LoadLanguageResource("ja.json");
			LoadLanguageResource("ko.json");
			LoadLanguageResource("pl.json");
			LoadLanguageResource("pt-BR.json");

			//We still load them after, for people who want to add new languages or change existing ones.
			try
			{
				var dirFiles = Directory.GetFiles(JsonManager.ProgramFolder + "lang/", "*.json");

				foreach (string file in dirFiles)
				{
					string fileName = Path.GetFileName(file);
					string langName = fileName.Remove(fileName.Length - 5);

					Debug.WriteLine("Trying: " + langName);

					LanguageDataObject langObj = JsonManager.LoadFile<LanguageDataObject>("lang/" + fileName);

					//Overwrite if it exists, otherwise add
					if (translationData.ContainsKey(langName.ToLower()))
						translationData[langName.ToLower()] = langObj;
					else
						translationData.Add(langName.ToLower(), langObj);

					Debug.WriteLine("Added Language: " + langName);
				}
			}
			catch (Exception)
			{
				//This is fine
			}
		}


		private static void LoadLanguageResource(string fileName)
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(fileName));

			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			{
				if (stream == null) return;

				using (StreamReader reader = new StreamReader(stream))
				{
					string _data = reader.ReadToEnd();

					string langName = fileName.Remove(fileName.Length - 5);

					LanguageDataObject langObj = JsonManager.LoadString<LanguageDataObject>(_data);

					translationData.Add(langName.ToLower(), langObj);
				}
			}
		}


		/// <summary>
		/// Returns array of available languages.
		/// </summary>
		public static string[] GetAvailable()
		{
			var availableLanguages = new List<string>(translationData.Keys).ToArray();

			return availableLanguages;
		}


		public static string Translate(string TransText)
		{
			return CanTranslate(TransText) ? TranslateText(TransText) : TranslateApi(TranslateText(TransText, "en"));
		}

		public static string TranslateApi(string input)
		{
			// Set the language from/to in the url (or pass it into this function)
			string url =
				$"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl={CurrentLanguage}&dt=t&q={Uri.EscapeUriString(input)}";
			HttpClient httpClient = new HttpClient();
			string result = httpClient.GetStringAsync(url).Result;

			// Get all json data
			var jsonData = new JavaScriptSerializer().Deserialize<List<dynamic>>(result);

			// Extract just the first array element (This is the only data we are interested in)
			dynamic translationItems = jsonData[0];

			// Translation Data
			string translation = "";

			// Loop through the collection extracting the translated objects
			foreach (object item in translationItems)
			{
				// Convert the item array to IEnumerable
				IEnumerable translationLineObject = item as IEnumerable;

				// Convert the IEnumerable translationLineObject to a IEnumerator
				IEnumerator translationLineString = translationLineObject.GetEnumerator();

				// Get first object in IEnumerator
				translationLineString.MoveNext();

				// Save its value (translated text)
				translation += $" {Convert.ToString(translationLineString.Current)}";
			}

			// Remove first blank character
			if (translation.Length > 1) translation = translation.Substring(1);

			// Return translation
			return translation;
		}

		private static bool CanTranslate(string TransText)
		{
			if (translationData.TryGetValue(CurrentLanguage, out LanguageDataObject data))
			{
				string _holder = TransText.Split('.')[0];
				string _name = TransText.Split('.')[1];

				if (_holder == string.Empty)
					return false;
				if (_name == string.Empty)
					return false;

				PropertyInfo dataHolder =
					data.GetType().GetProperty(_holder, BindingFlags.Instance | BindingFlags.Public);

				if (dataHolder == null)
					return false;

				object _data = dataHolder.GetValue(data);

				if (_data == null)
					return false;

				PropertyInfo _dataProp =
					_data.GetType().GetProperty(_name, BindingFlags.Instance | BindingFlags.Public);

				if (_dataProp == null)
					return false;

				string translatedText = (string) _dataProp.GetValue(_data);

				if (string.IsNullOrEmpty(translatedText))
					return false;
			}
			else
			{
				return false;
			}

			return true;
		}

		/// <summary>Translates a Dictionary string.
		/// <para /> See language file.
		/// <para /> <example>If you have the word Search the Dictionary string would be "BROWSE.SEARCH".</example>
		/// </summary>
		public static string TranslateText(string TransText, string lang="N/A")
		{
			string translatedText;
			if (lang == "N/A")
				lang = CurrentLanguage;

			if (translationData.TryGetValue(lang, out LanguageDataObject data))
			{
				string _holder = TransText.Split('.')[0];
				string _name = TransText.Split('.')[1];

				PropertyInfo dataHolder =
					data.GetType().GetProperty(_holder, BindingFlags.Instance | BindingFlags.Public);

				if (dataHolder == null && _holder == string.Empty)
					return "LANG__NO__" + TransText;
				if(dataHolder == null && _holder != string.Empty)
					return "LANG__NO__" + _holder.ToUpper();

				object _data = dataHolder.GetValue(data);

				PropertyInfo _dataProp =
					_data.GetType().GetProperty(_name, BindingFlags.Instance | BindingFlags.Public);

				if (_dataProp == null && _name != string.Empty)
					return "LANG__NO__" + _name.ToUpper();
				if (_dataProp == null && _name == string.Empty)
					return "LANG__NO__" + TransText;

				translatedText = (string) _dataProp.GetValue(_data);

				if (string.IsNullOrEmpty(translatedText))
					return "LANG__NO__" + _name.ToUpper();
			}
			else
			{
				return "NO__LANGUAGE__" + lang.ToUpper();
			}

			return translatedText;
		}
	}

	//LANGUAGE DATA FOR JSON
	public class TAB
	{
		public string SETTINGS { get; set; }
		public string BROWSE { get; set; }
		public string INSTALLED { get; set; }
	}

	public class FOOTER
	{
		public string RELOAD { get; set; }
		public string SCROLL_TO_TOP { get; set; }
		public string OPEN_OFFICIAL_SITE { get; set; }
		public string TRANSLATE_DESCRIPTION { get; set; }
	}

	public class SETTINGS
	{
		public string NEED_UPDATE { get; set; }
		public string DOWNLOAD { get; set; }
		public string CONFIGURE_FOLDER { get; set; }
		public string OPEN_FOLDER_BROWSE_DIALOG { get; set; }
		public string INVALID_FOLDER { get; set; }
		public string INSTALLATION_HISTORY { get; set; }
		public string CLEAR { get; set; }
		public string CLEAR_DEFAULT_ERROR { get; set; }
		public string DISPLAY_UNKNOWN { get; set; }
	}

	public class BROWSE
	{
		public string SEARCH { get; set; }
		public string ALL { get; set; }
		public string INSTALLED { get; set; }
		public string UNINSTALLED { get; set; }
		public string SHOW_ONLY_UPDATABLE { get; set; }
		public string UPDATE_ALL_ADDONS { get; set; }
		public string SORT_BY_NAME_ASC { get; set; }
		public string SORT_BY_NAME_DESC { get; set; }
		public string SORT_BY_DATE_ASC { get; set; }
		public string SORT_BY_DATE_DESC { get; set; }
		public string SORT_BY_DEVELOPER_NAME { get; set; }
	}

	public class ADDONS
	{
		public string README { get; set; }
		public string CLOSE_README { get; set; }
		public string INSTALL { get; set; }
		public string UPDATE { get; set; }
		public string UNINSTALL { get; set; }
		public string UPDATE_LIST_SUCCESS { get; set; }
		public string UPDATE_LIST_BLANK { get; set; }
	}

	public class TOS
	{
		public string SITE_URL { get; set; }
	}

	public class LanguageDataObject
	{
		public TAB TAB { get; set; }
		public FOOTER FOOTER { get; set; }
		public SETTINGS SETTINGS { get; set; }
		public BROWSE BROWSE { get; set; }
		public ADDONS ADDONS { get; set; }
		public TOS TOS { get; set; }
	}
}