using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;
using System.Net;
using System.Diagnostics;
using Semver;

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


		public float downloadProgress = 0f;

		public void OverrideCurrentDisplay(AddonObject display)
		{
			if (!this.addons.Contains(this.currentDisplay))
				this.addons.Add(this.currentDisplay);

			this.currentDisplay = display;
			this.addons.Add(display);
		}

		public void AddAddon(AddonObject display)
		{
			this.addons.Add(display);
		}

		public void StartDownload()
		{
			if (DownloadManager.AddonFileExists(this))
			{
				//Set the progress to 0
				this.addonControl.DynamicNotificationBG.Width = 0;

				//Update the display
				this.currentDisplay.IsQueued = true;
				this.tabManager.PopulateAddon(this, 0, 0);

				DownloadManager.Queue(this, UpdateDownloadProgress);
			}
			else
			{
				Debug.WriteLine("File couldn't be found on github.");
			}
		}

		public void StartUpdate()
		{
			if (!this.currentDisplay.hasUpdate)
				return;

			AddonObject newObj = this.GetNewest();


			//The file can't be downloaded
			if (!DownloadManager.AddonFileExists(newObj))
			{
				Settings.CauseError("File could not be found. Please contact the Author.");
				return;
			}

			//Make sure it isn't the exact same version
			if (newObj.addon.fileVersion == this.currentDisplay.addon.fileVersion)
			{
				Settings.CauseError("Cannot update to same version.");
				return;
			}

			//Remove the current installed file
			this.Remove(true);

			//Set the display to the newest version
			this.currentDisplay = newObj;

			//Set the progress to 0
			this.addonControl.DynamicNotificationBG.Width = 0;

			//Update the display
			this.tabManager.PopulateAddon(this, 0, 0);

			//Download the newest version
			DownloadManager.Queue(this, UpdateDownloadProgress);
		}

		public void Remove(bool isUpdate = false)
		{
			if (Settings.isTosRunning())
			{
				Settings.CauseError("Please close ToS before uninstalling addons.", "Close ToS", new ErrorButtonCallback(() => { Settings.CloseTos(); }));
				
				return;
			}

			if (!isUpdate)
			{
				AddonObject installedObj = this.GetOtherInstalled();

				//Remove it from the list if there isn't another installed one
				if (installedObj == null)
				{
					this.InstalledAddons.RemoveAddon(this);
					DownloadManager.DeleteAddon(this, true);
				}
				else //Remove it and update the display to the installed one
				{
					DownloadManager.DeleteAddon(this, false);
					//Set it to uninstalled
					this.currentDisplay.isInstalled = false;
					//Update the display
					this.currentDisplay = installedObj;
					this.tabManager.PopulateAddon(this, 0, 0);
				}

			}
			else
			{
				this.currentDisplay.isInstalled = false;
				this.currentDisplay.hasUpdate = false;
				DownloadManager.DeleteAddon(this, true);
			}
		}

		public void UpdateDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
		{
			DownloadManager.isDownloadInProgress = true;

			downloadProgress = e.ProgressPercentage;

			if (downloadProgress < 100f)
			{
				this.addonControl.DynamicNotificationBG.Visibility = Visibility.Visible;
				this.addonControl.DynamicNotificationBG.Width = (529f/100f)*downloadProgress;
			}
			else
			{
				//this.addonControl.DynamicNotificationBG.Visibility = Visibility.Hidden;
				this.addonControl.DynamicNotificationBG.Width = 529f;
			}

			//Update the display
			this.tabManager.PopulateAddon(this, 0, 0);
		}

		AddonObject GetNewest()
		{
			List<AddonObject> _versionList = new List<AddonObject>(this.addons);

			AddonObject newest = null;

			SemVersion addonSemVer = SemVersion.Parse(this.currentDisplay.addon.fileVersion.Remove(0, 1));

			foreach (AddonObject obj in this.addons)
			{
				SemVersion otherSemVer = SemVersion.Parse(obj.addon.fileVersion.Remove(0, 1));
				int semVerCheck = otherSemVer.CompareTo(addonSemVer);

				if (semVerCheck == 1)
				{
					newest = obj;
				}
			}
			return newest;
		}

		AddonObject GetOtherInstalled()
		{
			foreach (AddonObject obj in this.addons)
			{
				if (obj.isInstalled && obj.addon.fileVersion != this.currentDisplay.addon.fileVersion)
				{
					return obj;
				}
			}
			return null;
		}

		public void DisplayReadme()
		{
			string readme = DownloadManager.GetReadme(this.currentDisplay);

			if(readme == "ERROR")
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