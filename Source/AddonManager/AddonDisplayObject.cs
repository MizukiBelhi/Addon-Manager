using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace AddonManager
{
	public class AddonDisplayObject
	{
		public AddonObject currentDisplay;
		public List<AddonObject> addons;
		public Canvas displayCanvas;
		public AddonControl addonControl;
		public TabManager tabManager;
		public InstalledAddons InstalledAddons;

		public bool Invalid = false;


		public float downloadProgress;

		public void OverrideCurrentDisplay(AddonObject display)
		{
			if (!addons.Contains(currentDisplay))
				addons.Add(currentDisplay);

			currentDisplay = display;
			addons.Add(display);
		}

		public void AddAddon(AddonObject display)
		{
			addons.Add(display);
		}

		public void StartDownload()
		{
			if (DownloadManager.AddonFileExists(this))
			{
				AddonObject obj = GetOtherInstalled();
				if (obj != null)
				{
					if (Settings.isTosRunning())
					{
						Settings.CauseError("Please close ToS before downloading a different version.", "Close ToS",
							Settings.CloseTos);

						return;
					}

					//Removes current installed addon
					DownloadManager.DeleteAddon(obj, false);
				}

				//Set the progress to 0
				addonControl.DynamicNotificationBG.Width = 0;

				//Update the display
				currentDisplay.IsQueued = true;
				tabManager.PopulateAddon(this, 0, 0);

				DownloadManager.Queue(this, UpdateDownloadProgress);
			}
			else
			{
				Debug.WriteLine("File couldn't be found on github.");
				Settings.CauseError("File could not be found. Please contact the Author.");
			}
		}

		public void StartUpdate()
		{
			if (!currentDisplay.hasUpdate)
				return;

			AddonObject newObj = GetNewest();


			//The file can't be downloaded
			if (!DownloadManager.AddonFileExists(newObj))
			{
				Settings.CauseError("File could not be found. Please contact the Author.");
				return;
			}

			//Make sure it isn't the exact same version
			if (newObj == currentDisplay)
			{
				Settings.CauseError("Cannot update to the same version.");
				return;
			}

			//Remove the current installed file
			Remove(true);

			//Set the display to the newest version
			currentDisplay = newObj;

			//Set the progress to 0
			addonControl.DynamicNotificationBG.Width = 0;

			//Update the display
			tabManager.PopulateAddon(this, 0, 0);

			//Download the newest version
			DownloadManager.Queue(this, UpdateDownloadProgress);
		}

		public void Remove(bool isUpdate = false)
		{
			if (Settings.isTosRunning())
			{
				Settings.CauseError("Please close ToS before " + (isUpdate ? "updating" : "removing") + " addons.",
					"Close ToS", Settings.CloseTos);
				return;
			}

			if (!isUpdate)
			{
				AddonObject installedObj = GetOtherInstalled();

				//Remove it from the list if there isn't another installed one
				if (installedObj == null)
				{
					InstalledAddons.RemoveAddon(this);
					DownloadManager.DeleteAddon(this);
				}
				else //Remove it and update the display to the installed one
				{
					DownloadManager.DeleteAddon(this, false);
					//Set it to uninstalled
					currentDisplay.isInstalled = false;
					//Update the display
					currentDisplay = installedObj;
					tabManager.PopulateAddon(this, 0, 0);
				}
			}
			else
			{
				currentDisplay.isInstalled = false;
				currentDisplay.hasUpdate = false;
				DownloadManager.DeleteAddon(this, false);
			}
		}

		public void UpdateDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
		{
			DownloadManager.isDownloadInProgress = true;

			downloadProgress = e.ProgressPercentage;

			if (downloadProgress < 100f)
			{
				addonControl.DynamicNotificationBG.Visibility = Visibility.Visible;
				addonControl.DynamicNotificationBG.Width = 529f / 100f * downloadProgress;
			}
			else
			{
				//this.addonControl.DynamicNotificationBG.Visibility = Visibility.Hidden;
				addonControl.DynamicNotificationBG.Width = 529f;
			}

			//Update the display
			tabManager.PopulateAddon(this, 0, 0);
		}

		private AddonObject GetNewest()
		{
			//List<AddonObject> _versionList = new List<AddonObject>(this.addons);

			AddonObject newest = null;

			foreach (AddonObject obj in addons)
			{
				if (newest == null)
				{
					newest = obj;
					continue;
				}

				if (obj > newest) newest = obj;
			}

			Debug.WriteLine("Newest: " + newest.addon.fileVersion);
			return newest;
		}

		private AddonObject GetOtherInstalled()
		{
			foreach (AddonObject obj in addons)
				if (obj.isInstalled && obj.addon.fileVersion != currentDisplay.addon.fileVersion)
					return obj;
			return null;
		}

		public void DisplayReadme()
		{
			string readme = DownloadManager.GetReadme(currentDisplay);

			if (readme == "ERROR")
			{
				Settings.CauseError("No Readme available.");
				return;
			}

			readmeWindow handler = new readmeWindow();
			handler.DisplayReadme(readme);
			handler.ShowDialog();
		}
	}
}