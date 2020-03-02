using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Threading;
using Semver;

namespace AddonManager
{
	public class InstalledAddons
	{

		private Dictionary<string, AddonDisplayObject> installedAddons;

		private MainWindow mainWindow;
		private TabManager tabManager;

		public void Init(MainWindow win, TabManager manager)
		{
			mainWindow = win;
			tabManager = manager;

			this.GenerateList();
		}

		public void GenerateList()
		{
			installedAddons = new Dictionary<string, AddonDisplayObject>();
			List<AddonObject> _installedAddons = DownloadManager.GetInstalledAddons();

			foreach (AddonObject addon in _installedAddons)
			{
				AddonDisplayObject _instAddonDisplay = new AddonDisplayObject();

				addon.SetDefaults();

				_instAddonDisplay.currentDisplay = addon;
				_instAddonDisplay.InstalledAddons = this;
				_instAddonDisplay.addons = new List<AddonObject> { addon };

				if (installedAddons.ContainsKey(addon.addon.file))
				{
					if (installedAddons[addon.addon.file].addons == null)
						installedAddons[addon.addon.file].addons = new List<AddonObject>();

					//is it the same version? Technically shouldn't happen but still does
					if (installedAddons[addon.addon.file].addons[0].addon.fileVersion != addon.addon.fileVersion)
					{
						//Debug.WriteLine("Found same addons: " + addon.addon.file + " V1: " + installedAddons[addon.addon.file].addons[0].addon.fileVersion + " V2: " + addon.addon.fileVersion);
						installedAddons[addon.addon.file].addons.Add(addon);
					}
				}
				else
				{
					installedAddons.Add(addon.addon.file, _instAddonDisplay);
				}
			}
		}

		public void AddAddon(AddonDisplayObject obj)
		{
			obj.InstalledAddons = this;

			if(!installedAddons.ContainsKey(obj.currentDisplay.addon.file)) //caused by updating
				installedAddons.Add(obj.currentDisplay.addon.file, obj);


			Populate();
		}

		public void RemoveAddon(AddonDisplayObject obj)
		{
			installedAddons.Remove(obj.currentDisplay.addon.file);

			Populate();
			//Put it back in the list
			obj.currentDisplay.isInstalled = false;
			obj.currentDisplay.isDownloading = false;
			tabManager.AddToList(obj);
		}

		public void Populate()
		{
			//Debug.WriteLine("Installed Addons Found: " + installedAddons.Count);

			FieldInfo gridHolder = mainWindow.GetType().GetField("InstalledAddonCanvas", BindingFlags.Instance | BindingFlags.Public);
			ScrollViewer view = (ScrollViewer)gridHolder.GetValue(mainWindow);

			Canvas canvas = null;

			view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				canvas = new Canvas();

				view.Content = canvas;
			}));


			int y = 0;
			foreach (KeyValuePair<string, AddonDisplayObject> obj in installedAddons)
			{
				//is it still unknown but has a name?
				if(obj.Value.currentDisplay.addon.name != obj.Key && obj.Value.currentDisplay.isUnknown)
				{
					//installedAddons[obj.Key].currentDisplay.addon.description = obj.Value.addons[obj.Value.addons.Count()-1].addon.description;
					//installedAddons[obj.Key].currentDisplay.repo = obj.Value.addons[obj.Value.addons.Count()-1].repo;
					//installedAddons[obj.Key].currentDisplay.addon.name = obj.Value.addons[obj.Value.addons.Count()-1].addon.name;
					installedAddons[obj.Key].currentDisplay.isUnknown = false;
					//installedAddons[obj.Key].currentDisplay.isUnknownInstalled = false;
					installedAddons[obj.Key].addons[0].isInstalled = true;
				}

				installedAddons[obj.Key].displayCanvas = null;

				tabManager.PopulateAddon(obj.Value, 0, y * 120, false, canvas);
				y++;
			}

			canvas.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				canvas.Height = 20 + (installedAddons.Count * 120);
			}));
		}

		public bool CheckInstalled(AddonObject addon, string repo)
		{
			if(installedAddons.ContainsKey(addon.addon.file))
			{

				//Is the same version&name already in here?
				if (installedAddons[addon.addon.file].currentDisplay.addon.name == addon.addon.name && installedAddons[addon.addon.file].currentDisplay.addon.fileVersion == addon.addon.fileVersion)
				{
					//Fixes some issue where a known addon is unknown
					installedAddons[addon.addon.file].currentDisplay.isUnknown = false;
					installedAddons[addon.addon.file].currentDisplay.addon.description = addon.addon.description;
					installedAddons[addon.addon.file].currentDisplay.repo = addon.repo;
					installedAddons[addon.addon.file].currentDisplay.addon.name = addon.addon.name;
					return true;
				}

				foreach (AddonObject obj in installedAddons[addon.addon.file].addons)
				{
					if (obj.addon.name == addon.addon.name && obj.addon.fileVersion == addon.addon.fileVersion)
						return true;
				}

				if (installedAddons[addon.addon.file].currentDisplay.isUnknown)
				{

					//Version check:  -1 = Newer   0 = Same   1 = Older
					int semVerCheck = SemVersion.Parse(addon.addon.fileVersion.Remove(0, 1)).CompareTo(SemVersion.Parse(installedAddons[addon.addon.file].currentDisplay.addon.fileVersion.Remove(0, 1)));

					if (semVerCheck == 1)
					{
						//Debug.WriteLine("New: V1: " + addon.addon.name+ " - " + addon.addon.fileVersion + " V2: " + installedAddons[addon.addon.file].currentDisplay.addon.name + " - " + installedAddons[addon.addon.file].currentDisplay.addon.fileVersion);
						//installedAddons[addon.addon.file].OverrideCurrentDisplay(addon);
						installedAddons[addon.addon.file].AddAddon(addon);
						installedAddons[addon.addon.file].currentDisplay.isUnknown = false;
						//installedAddons[addon.addon.file].currentDisplay.isUnknownInstalled = false;
						installedAddons[addon.addon.file].currentDisplay.hasUpdate = true;
						installedAddons[addon.addon.file].currentDisplay.addon.description = addon.addon.description;
						installedAddons[addon.addon.file].currentDisplay.repo = addon.repo;
						installedAddons[addon.addon.file].currentDisplay.addon.name = addon.addon.name;
					}
					else
					{
						//installedAddons[addon.addon.file].AddAddon(addon);

						if (semVerCheck == 0)
						{
							//Debug.WriteLine("Same: V1: " + addon.addon.name + " - " + addon.addon.fileVersion + " V2: " + installedAddons[addon.addon.file].currentDisplay.addon.name + " - " + installedAddons[addon.addon.file].currentDisplay.addon.fileVersion);
							installedAddons[addon.addon.file].currentDisplay.addon.name = addon.addon.name;
							installedAddons[addon.addon.file].currentDisplay.addon.tags = addon.addon.tags;
							installedAddons[addon.addon.file].currentDisplay.addon.description = addon.addon.description;
							installedAddons[addon.addon.file].currentDisplay.repo = repo;
							installedAddons[addon.addon.file].currentDisplay.isUnknown = false;
							//installedAddons[addon.addon.file].currentDisplay.isUnknownInstalled = false;
						}
						else
						{
							//Debug.WriteLine("Old: V1: " + addon.addon.name + " - " + addon.addon.fileVersion + " V2: " + installedAddons[addon.addon.file].currentDisplay.addon.name + " - " + installedAddons[addon.addon.file].currentDisplay.addon.fileVersion);

							installedAddons[addon.addon.file].AddAddon(addon);
						}
					}

				}
				else
				{
					//installedAddons[addon.addon.file].AddAddon(addon);
					int semVerCheck = SemVersion.Parse(addon.addon.fileVersion.Remove(0, 1)).CompareTo(SemVersion.Parse(installedAddons[addon.addon.file].currentDisplay.addon.fileVersion.Remove(0, 1)));

					if (semVerCheck == 1)
					{
						//Debug.WriteLine("NNNNN: V1: " + addon.addon.name + " - " + addon.addon.fileVersion + " V2: " + installedAddons[addon.addon.file].currentDisplay.addon.name + " - " + installedAddons[addon.addon.file].currentDisplay.addon.fileVersion);
						//installedAddons[addon.addon.file].OverrideCurrentDisplay(addon);
						addon.isNewest = true;
						installedAddons[addon.addon.file].AddAddon(addon);
						installedAddons[addon.addon.file].currentDisplay.hasUpdate = true;
						installedAddons[addon.addon.file].currentDisplay.isNewest = false;

					}
					else
					{
						installedAddons[addon.addon.file].AddAddon(addon);

					}


				}

				if (installedAddons[addon.addon.file].currentDisplay.addon.name == installedAddons[addon.addon.file].currentDisplay.addon.file)
				{
					installedAddons[addon.addon.file].currentDisplay.addon.description = addon.addon.description;
					installedAddons[addon.addon.file].currentDisplay.repo = addon.repo;
					//installedAddons[addon.addon.file].currentDisplay.isUnknown = false;
					//installedAddons[addon.addon.file].currentDisplay.isUnknownInstalled = false;
					installedAddons[addon.addon.file].currentDisplay.addon.name = addon.addon.name;
				}

				//Populate();

				return true;
			}
			return false;
		}
	}
}
