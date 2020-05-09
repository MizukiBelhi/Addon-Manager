using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AddonManager
{
	internal class DeveloperTools
	{
		private static readonly List<AddonDisplayObject> addonDisplayList = new List<AddonDisplayObject>();


		public static void LoadAddon(string filePath)
		{
			MainWindow mainWindow = (MainWindow) Application.Current.MainWindow;

			var addons = JsonManager.LoadFileDirect<List<AddonsObject>>(filePath);


			foreach (AddonDisplayObject newAddonDisplay in from addon in addons where addon != null select new AddonObject {repo = "DEV/DEV", addon = addon, dependencies = null} into addonObj select new AddonDisplayObject
			{
				currentDisplay = addonObj
			})
			{
				addonDisplayList.Add(newAddonDisplay);
			}


			FieldInfo gridHolder = mainWindow.GetType().GetField("DevCanvas", BindingFlags.Instance | BindingFlags.Public);
			if (gridHolder == null) return;
			ScrollViewer view = (ScrollViewer) gridHolder.GetValue(mainWindow);

			Canvas canvas = null;
			view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				if (view.Content == null)
				{
					Canvas addonCanvas = new Canvas();
					view.Content = addonCanvas;
				}

				canvas = (Canvas) view.Content;
				canvas.Children.Clear();
			}));

			MainWindow.tabManager.DisplayAddons(canvas);


			int y = 0;
			foreach (AddonDisplayObject addonDisplay in addonDisplayList)
			{
				MainWindow.tabManager.PopulateAddon(addonDisplay, 0, y * 120, true, canvas);


				y++;
			}
		}

		public static void ClearAddons()
		{
			MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
			FieldInfo gridHolder = mainWindow.GetType().GetField("DevCanvas", BindingFlags.Instance | BindingFlags.Public);
			if (gridHolder == null) return;
			ScrollViewer view = (ScrollViewer)gridHolder.GetValue(mainWindow);

			view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				if (view.Content != null)
				{
					Canvas addonCanvas = (Canvas)view.Content;
					addonCanvas.Children.Clear();
				}
			}));
		}
	}
}