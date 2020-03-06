using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Threading;

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
					if (installedAddons[addon.addon.file].addons[0] == addon)
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

			if (!installedAddons.ContainsKey(obj.currentDisplay.addon.file)) //caused by updating
			{
				installedAddons.Add(obj.currentDisplay.addon.file, obj);
				Populate();
			}
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
					installedAddons[obj.Key].currentDisplay.isUnknown = false;
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
				if (installedAddons[addon.addon.file].currentDisplay.addon.name == addon.addon.name && installedAddons[addon.addon.file].currentDisplay == addon)
				{
					//Fixes some issue where a known addon is unknown
					installedAddons[addon.addon.file].currentDisplay.isUnknown = false;
					CopyAddonDescriptor(addon);
					return true;
				}

				foreach (AddonObject obj in installedAddons[addon.addon.file].addons)
				{
					if (obj.addon.name == addon.addon.name && obj == addon)
						return true;
				}

				if (installedAddons[addon.addon.file].currentDisplay.isUnknown)
				{

					if(addon == installedAddons[addon.addon.file].currentDisplay)
					{
						CopyAddonDescriptor(addon);
						installedAddons[addon.addon.file].currentDisplay.isUnknown = false;
					}
					else
					{
						installedAddons[addon.addon.file].AddAddon(addon);
					}

				}
				else
				{

					if (addon > installedAddons[addon.addon.file].currentDisplay)
					{
						addon.isNewest = true;
						installedAddons[addon.addon.file].AddAddon(addon);
						installedAddons[addon.addon.file].currentDisplay.hasUpdate = true;
						installedAddons[addon.addon.file].currentDisplay.isNewest = false;

					}
					else
					{
						if(addon == installedAddons[addon.addon.file].currentDisplay)
						{
							CopyAddonDescriptor(addon);
							installedAddons[addon.addon.file].currentDisplay.isUnknown = false;
						}
						installedAddons[addon.addon.file].AddAddon(addon);

					}


				}

				if (installedAddons[addon.addon.file].currentDisplay.addon.name == installedAddons[addon.addon.file].currentDisplay.addon.file)
				{
					CopyAddonDescriptor(addon);

					if (addon > installedAddons[addon.addon.file].currentDisplay)
					{
						addon.isNewest = true;
						installedAddons[addon.addon.file].currentDisplay.hasUpdate = true;
						installedAddons[addon.addon.file].currentDisplay.isNewest = false;

					}
				}

				//Populate();

				return true;
			}
			return false;
		}

		private void CopyAddonDescriptor(AddonObject a)
		{
			installedAddons[a.addon.file].currentDisplay.addon.name = a.addon.name;
			installedAddons[a.addon.file].currentDisplay.addon.tags = a.addon.tags;
			installedAddons[a.addon.file].currentDisplay.dependencies = a.dependencies;
			installedAddons[a.addon.file].currentDisplay.addon.description = a.addon.description;
			installedAddons[a.addon.file].currentDisplay.addon.releaseTag = a.addon.releaseTag;
			installedAddons[a.addon.file].currentDisplay.repo = a.repo;
		}
	}
}
