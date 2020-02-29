using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;

namespace AddonManager
{
	static class JsonManager
	{

		/// <summary>
		/// exe folder location
		/// </summary>
		public static string ProgramFolder
		{
			get
			{
				return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+"/";
			}
		}


		/// <summary>
		/// Loads JSON file into a Template
		/// </summary>
		public static T LoadFile<T>(string fileName)
		{
			string file = "";
			try
			{
				file = File.ReadAllText(ProgramFolder + fileName);
			}catch (IOException)
			{
				return default(T);
			}catch(UnauthorizedAccessException)
			{
				return default(T);
			}

			return JsonConvert.DeserializeObject<T>(file);
		}

		/// <summary>
		/// Loads JSON file without Template
		/// </summary>
		public static object LoadFile(string fileName)
		{
			string file = "";
			try
			{
				file = File.ReadAllText(ProgramFolder + fileName);
			}
			catch (IOException)
			{
				return null;
			}
			catch (UnauthorizedAccessException)
			{
				return null;
			}

			return JsonConvert.DeserializeObject(file);
		}


		/// <summary>
		/// Directory.Exists 
		/// </summary>
		public static bool DirectoryExists(string directory)
		{
			return Directory.Exists(directory);
		}


		/// <summary>
		/// File.Exists
		/// </summary>
		public static bool FileExists(string filePath)
		{
			return File.Exists(filePath);
		}

		/// <summary>
		/// File.Delete from %programfolder%
		/// </summary>
		public static void RemoveFile(string fileName)
		{
			try
			{
				if (IsValidFileName(fileName))
					File.Delete(ProgramFolder + fileName);
			}
			catch (IOException)
			{

			}
			catch (UnauthorizedAccessException)
			{

			}
		}


		/// <summary>
		/// File.Delete
		/// </summary>
		public static void RemoveFile(string folder, string fileName)
		{
			try
			{
				if(IsValidFileName(fileName))
					File.Delete(folder + fileName);
			}
			catch (IOException)
			{

			}
			catch (UnauthorizedAccessException)
			{

			}
		}

		/// <summary>
		/// Writes data to file in %programfolder%
		/// </summary>
		public static void WriteFile(string fileName, string data)
		{
			File.WriteAllText(ProgramFolder + fileName, data);
		}


		/// <summary>
		/// Creates JSON file with data in %programfolder%
		/// </summary>
		public static bool CreateFile(string fileName, object data)
		{
			try
			{
				if (IsValidFileName(fileName))
					File.WriteAllText(ProgramFolder + fileName, JsonConvert.SerializeObject(data), Encoding.Unicode);
			}
			catch (IOException)
			{
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}

			return true;
		}


		/// <summary>
		/// Creates folder
		/// </summary>
		public static bool CreateFolder(string folderName)
		{
			if (DirectoryExists(folderName))
				return true;

			try
			{
				Directory.CreateDirectory(folderName);
			}
			catch (IOException)
			{
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Deletes Folder
		/// </summary>
		public static bool DeleteFolder(string folderName)
		{
			try
			{
				Directory.Delete(folderName, true);
			}
			catch(DirectoryNotFoundException)
			{
				return true;
			}
			catch (IOException)
			{
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
			return true;
		}


		/// <summary>
		/// Converts JSON string into Template
		/// </summary>
		public static T Convert<T>(string jsonText)
		{
			return JsonConvert.DeserializeObject<T>(jsonText);
		}


		private static bool IsValidFileName(string fileName)
		{
			return !string.IsNullOrEmpty(fileName) && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
		}


	}
}
