using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Documents;
using Semver;
using FontAwesome.WPF;
using System.Globalization;

namespace AddonManager
{

	public class TabManager
	{
		//private string source;
		//private string tabName;

		private List<AddonDisplayObject> addonDisplayList = new List<AddonDisplayObject>();
		private List<AddonDisplayObject> addonDisplayListCopy = new List<AddonDisplayObject>();

		protected delegate void EmptyDelegate();
		private EmptyDelegate emptyDelegate;

		private MainWindow mainWindow;
		private InstalledAddons installedAddons;

		//private WebClient cwc;


		private string repoSource = "https://raw.githubusercontent.com/{0}/master/addons.json";

		public bool isFinishedLoading = false;
		private int finishedLoadingNum = 0;

		private int sortMode = 0;


		private bool isSorting = false;


		private Dictionary<string, string> addonSources;


		private int _onPage = 0; //Current page we're on

		private int _maxPageCount = 10; //Amount of addons displayed for each page

		private int _currentCount = 0; //Current amount of addons displayed

		//Token to cancel sorting
		CancellationTokenSource cts = new CancellationTokenSource();


		public TabManager(Dictionary<string, string> addonSources)
		{
			this.addonSources = addonSources;
		   // this.source = managersUrl;
			//this.tabName = tabName;
			this.mainWindow = (MainWindow)Application.Current.MainWindow;

			installedAddons = new InstalledAddons();

			installedAddons.Init(mainWindow, this);

			
			this.Load();
		}


		public async void SearchListAsync(string term)
		{
			try {
				cts = new CancellationTokenSource();
				await Task.Run(() => SearchList(term, cts.Token), cts.Token);
			}catch(Exception e)
			{ }
		}

		public void SearchList(string sterm, CancellationToken cancellationToken)
		{
			string[] searchTerm = sterm.ToLower().Split(' ');

			//The search term is "Search..." when it is empty automatically
			if (sterm == "Search...")
			{

				if (addonDisplayListCopy.Count > 0)
				{

					addonDisplayList = new List<AddonDisplayObject>(addonDisplayListCopy);
					UpdateCurrentCanvasSize();

					addonDisplayListCopy.Clear();

					SortListAsync();
				}

				return;
			}

			if(addonDisplayListCopy.Count > 0)
				addonDisplayList = new List<AddonDisplayObject>(addonDisplayListCopy);

			addonDisplayListCopy = new List<AddonDisplayObject>(addonDisplayList);
			List<AddonDisplayObject> searchResult = new List<AddonDisplayObject>();

			foreach (string term in searchTerm)
			{
				//name, author
				searchResult.AddRange(
					addonDisplayList.FindAll( p =>
						p.currentDisplay.addon.name.ToLower().Contains(term) ||
						p.currentDisplay.repo.Split('/')[0].ToLower().Contains(term)
					));


				//tags  probably can be simplified
				foreach (AddonDisplayObject obj in addonDisplayList)
				{
					if (obj.currentDisplay.addon.tags != null)
					{
						int tagCount = obj.currentDisplay.addon.tags.FindAll(r => r.Contains(term)).Count;
						if (tagCount > 0 && !searchResult.Contains(obj))
							searchResult.Add(obj);

						cancellationToken.ThrowIfCancellationRequested();
					}
				}
			}

			addonDisplayList = new List<AddonDisplayObject>(searchResult);
			searchResult.Clear();


			UpdateCurrentCanvasSize();

			SortListAsync();
		}

		private async void SortListAsync()
		{
			try
			{
				cts = new CancellationTokenSource();
				await Task.Run(() => SortList(cts.Token), cts.Token);
			}catch(Exception e)
			{ }

		}

		public void SortList(CancellationToken cancellationToken)
		{
		//	if (isSorting) return;


			isSorting = true;

			cancellationToken.ThrowIfCancellationRequested();

			if (sortMode == 0)
			{

				addonDisplayList.Sort((x, p) =>
					x.currentDisplay.addon.name.CompareTo(p.currentDisplay.addon.name));
			}
			else
			{
				addonDisplayList.Sort((x, p) =>
					p.currentDisplay.addon.name.CompareTo(x.currentDisplay.addon.name));
			}


			FieldInfo gridHolder = mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
			ScrollViewer view = (ScrollViewer)gridHolder.GetValue(mainWindow);

			Canvas canvas = null;

			view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				canvas = (Canvas)view.Content;
				canvas.Children.Clear();
				canvas.Dispatcher.Invoke(DispatcherPriority.Render, emptyDelegate);
			}));


			int y = 0;
			foreach (AddonDisplayObject obj in addonDisplayList)
			{
				obj.displayCanvas = null;
				PopulateAddon(obj, 0, y * 120, true);
				y++;

				cancellationToken.ThrowIfCancellationRequested();
			}

			isSorting = false;
			UpdateCurrentCanvasSize(canvas);

		}


		private void Load()
		{
			if (emptyDelegate == null)
				emptyDelegate += emptyDel;

			//PopulateInstalledAddons();

			foreach (KeyValuePair<string, string> source in this.addonSources)
			{

				// isRepoDownload = true;

				mainWindow = (MainWindow)Application.Current.MainWindow;
				using (WebClient wc = new WebClient())
				{

					wc.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
					wc.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
					wc.DownloadFileCompleted += Tab_DownloadComplete;
					wc.DownloadProgressChanged += Tab_DownloadProgressChanged;
					Debug.WriteLine("Download " + source.Key + " server list");
					wc.QueryString.Add("server", source.Key);
					wc.DownloadFileAsync(
						// Param1 = Link of file
						new System.Uri(source.Value),
						// Param2 = Path to save
						JsonManager.ProgramFolder + source.Key + "managers.json"
					);
				}



			}
		}


		public void UpdateTabElements()
		{
			FieldInfo sortHolder = mainWindow.GetType().GetField("AddonSortButton", BindingFlags.Instance | BindingFlags.Public);
			Button sort = (Button)sortHolder.GetValue(mainWindow);

			sort.Background = new ImageBrush() { ImageSource = ImageAwesome.CreateImageSource(FontAwesomeIcon.Sort, new SolidColorBrush(Color.FromRgb(255,255,255))) };
			sort.ClickMode = ClickMode.Release;
			sort.Click += SortButton_Click;
		}




		void SortButton_Click(object sender, RoutedEventArgs e)
		{
			e.Handled = true;
			if (isSorting)
			{
				cts.Cancel();
				isSorting = false;
			}


			if (sortMode == 0)
				sortMode = 1;
			else
				sortMode = 0;


			SortListAsync();
		}

		void AddonButton_Click(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;

			AddonButton addonButton = (AddonButton)button.DataContext;

			AddonDisplayObject addon = addonButton.addon;


			switch (addonButton.buttonType)
			{
				case AddonButtonType.DOWNLOAD:
					addon.StartDownload();
					break;
				case AddonButtonType.GITHUB:
					Process.Start("https://github.com/" + addon.currentDisplay.repo + "/");
					break;
				case AddonButtonType.REMOVE:
					addon.Remove();
					break;
				case AddonButtonType.MORE:
					break;
				case AddonButtonType.UPDATE:
					addon.StartUpdate();
					break;
				case AddonButtonType.OPENLIST:
					if (addonButton.list != null)
						addonButton.list.IsDropDownOpen = true;
					break;
				default:
					break;
			}
		}

		int PopulateRepo(Canvas canvas, string server)
		{
			
			ManagersObject managers = JsonManager.LoadFile<ManagersObject>(server + "managers.json");

			foreach (ManagersSource source in managers.sources)
			{

				if (LoadRepo(source.repo, server) == 1)
					continue;

				List<AddonsObject> addons = JsonManager.LoadFile<List<AddonsObject>>(server + "repo.json");


				foreach(AddonsObject addon in addons)
				{
					if (addon == null)
						continue;

					numAddons++;

					AddonObject addonObj = new AddonObject() { repo = source.repo, addon = addon, dependencies = managers.dependencies };

					AddonDisplayObject newAddonDisplay = new AddonDisplayObject
					{
						currentDisplay = addonObj
					};

					if (installedAddons.CheckInstalled(addonObj, addonObj.repo))
					{
						newAddonDisplay.Invalid = true;
						continue;
					}

					addonDisplayList.Add(newAddonDisplay);
				}

			}

			return 0;
		}


		public async void DisplayAddons()
		{
			FieldInfo gridHolder = mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
			ScrollViewer view = (ScrollViewer)gridHolder.GetValue(mainWindow);

			Canvas canvas = null;

			view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				if (view.Content == null)
				{
					Canvas addonCanvas = new Canvas();
					view.Content = addonCanvas;
				}

				canvas = (Canvas)view.Content;
			}));


			

			await Task.Run(() => DisplayAddons(canvas));

			Debug.WriteLine("Loaded " + numAddons + " Addons.");
		}

		//Addons <->
		int DisplayAddons(Canvas canvas)
		{
			if (canvas == null)
				return -1;

			List<AddonObject> _installedAddons = DownloadManager.GetInstalledAddons();
			List<AddonDisplayObject> _addonDisplayList = new List<AddonDisplayObject>(addonDisplayList);

			foreach (AddonDisplayObject addonDisplay in addonDisplayList)
			{

				if (addonDisplay == null)
					continue;

				if (addonDisplay.Invalid)
					continue;

				AddonObject addonObj = addonDisplay.currentDisplay;
				AddonsObject addon = addonObj.addon;
				string addonName = addon.name;
				string addonFileName = addon.file;
				SemVersion addonSemVer = SemVersion.Parse(addon.fileVersion.Remove(0, 1));

				if (addonDisplay.addons == null)
				{
					addonDisplay.addons = new List<AddonObject>() { addonObj };
				}

				
				if (addonDisplayList.Count > 0)
				{
					//We see if there's other addons with the same name
					foreach (AddonDisplayObject displayObject in _addonDisplayList)
					{
						if (displayObject == null)
							continue;
						//it's the exact same one
						if (displayObject.currentDisplay.addon == addon)
							continue;

						if (addonDisplay.addons.Contains(displayObject.currentDisplay))
							continue;

						if (displayObject.Invalid)
							continue;

						//If for whatever reason the list is not initialized
						if (displayObject.addons == null)
						{
							displayObject.addons = new List<AddonObject>();
						}

						string _addonFileName = displayObject.currentDisplay.addon.file;

						//we only need to compare against the displayed addon
						if (_addonFileName == addonFileName)
						{

							//Version check:  -1 = Newer   0 = Same   1 = Older
							
							int semVerCheck = addonSemVer.CompareTo(SemVersion.Parse(displayObject.currentDisplay.addon.fileVersion.Remove(0, 1)));

							if (semVerCheck == -1)
							{
								addonDisplay.OverrideCurrentDisplay(displayObject.currentDisplay);
								addonDisplayList[addonDisplayList.FindIndex(x => x == displayObject)].Invalid = true;
							}
							else if (semVerCheck >= 0)
							{
								addonDisplay.AddAddon(displayObject.currentDisplay);
								addonDisplayList[addonDisplayList.FindIndex(x => x == displayObject)].Invalid = true;
							}
						}
					}
				}
			}


			Debug.WriteLine("Removing Installed Addons From Main List");
			addonDisplayList.RemoveAll(x => x.Invalid);

			UpdateListPositions();

			UpdateCurrentCanvasSize(canvas);

			return 0;
		}


		private void UpdateListPositions()
		{
			_currentCount = 0;
			//Update positions
			int y = 0;
			int addonIndex = 0;
			foreach (AddonDisplayObject addonDisplay in addonDisplayList)
			{
				if (addonIndex >= (_onPage + 1) * _maxPageCount)
					break;

				addonIndex++;
				_currentCount++;

				if (addonDisplay.Invalid)
					continue;


				PopulateAddon(addonDisplay, 0, y * 120, true);


				y++;
			}
		}

		public void RemoveFromList(AddonDisplayObject addon)
		{
			//Remove from main list
			addonDisplayList.Remove(addon);
			//UpdateListPositions();
			DisplayAddons();

		}

		public void AddToList(AddonDisplayObject addon)
		{
			addonDisplayList.Add(addon);
			//UpdateListPositions();
			DisplayAddons();
		}

		public void AddToInstalledAddons(AddonDisplayObject addon)
		{
			installedAddons.AddAddon(addon);
		}

		public void LoadNextPage()
		{
			//We're already showing all addons
			if(_currentCount >= addonDisplayList.Count)
			{
				return;
			}

			_onPage++;

			FieldInfo gridHolder = mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
			ScrollViewer view = (ScrollViewer)gridHolder.GetValue(mainWindow);

			Canvas canvas = null;

			view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				canvas = (Canvas)view.Content;
			}));



			
			canvas.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				canvas.Height = 30 + (_currentCount+10 * 120);
			}));
			

			int y = _currentCount-1;
			int addonIndex = 0;
			foreach (AddonDisplayObject addonDisplay in addonDisplayList)
			{
				//Maximum page count reached
				if (addonIndex >= (_onPage) * _maxPageCount)
				{
					break;
				}

				addonIndex++;

				//This addon is already displayed
				if (addonIndex < _currentCount)
				{
					continue;
				}

				if (addonDisplay.Invalid)
					continue;


				_currentCount++;

				PopulateAddon(addonDisplay, 0, y * 120, true);


				y++;
			}

			UpdateCurrentCanvasSize(canvas);

		}


		private void AddonSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cbox = sender as ComboBox;
			dynamic selectedItem = cbox.SelectedItem as dynamic;
			string selectedLang = selectedItem.ToLower();


			AddonDisplayObject addon = (AddonDisplayObject)cbox.DataContext;

			AddonObject selectedAddon = null;


			foreach (AddonObject obj in addon.addons)
			{

				if (selectedLang == (obj.addon.fileVersion + " by " + obj.repo.Split('/')[0]).ToLower())
				{
					selectedAddon = obj;
					break;
				}
			}

			addon.currentDisplay = selectedAddon;
			PopulateAddon(addon, 0, 0);

			Debug.WriteLine("AddonSelect: " + selectedLang);
		}



		private int LoadRepo(string repo, string server)
		{
			try
			{
				using (WebClient wc = new WebClient())
				{
					string useRepo = string.Format(repoSource, repo);

					wc.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
					wc.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
					// wc.DownloadFileCompleted += Manager_DownloadComplete;
					//  wc.DownloadProgressChanged += Manager_DownloadProgressChanged;
					wc.DownloadFile(
						// Param1 = Link of file
						new System.Uri(useRepo),
						// Param2 = Path to save
						JsonManager.ProgramFolder + server + "repo.json"
					);
				}
			}catch(Exception e)
			{
				Debug.WriteLine("Error trying to download repo: " + repo);
				Debug.WriteLine(e.Message.ToString());
				return 1;
			}

			return -1;
		}


		private void RemoveAddon(AddonDisplayObject addon, bool removeFolder = true)
		{
			addon.currentDisplay.hasUpdate = false;
			bool removeDisplay = false;

			if (addon.currentDisplay.isUnknownInstalled)
				removeDisplay = true;

			DownloadManager.DeleteAddon(addon, removeFolder);

			if (removeDisplay)
			{
				addon.addons.Remove(addon.currentDisplay);
				addon.currentDisplay = null;

				if (addon.addons.Count == 0)
				{
					addonDisplayList.Remove(addon);
				}
				else
				{
					if (addon.addons.Count > 1)
					{
						foreach (AddonObject obj in addon.addons)
						{
							foreach (AddonObject _obj in addon.addons)
							{
								int semVerCheck = SemVersion.Parse(obj.addon.fileVersion.Remove(0, 1)).CompareTo(SemVersion.Parse(_obj.addon.fileVersion.Remove(0, 1)));
								if (semVerCheck == -1)
								{
									_obj.isNewest = false;
									obj.isNewest = true;
									addon.currentDisplay = obj;
								}
							}
						}
					}
					else
					{
						addon.addons[0].isNewest = true;
						addon.currentDisplay = addon.addons[0];
					}
				}

				RebuildAddonList();
				UpdateCurrentCanvasSize();
			}

			if (addon.addons.Count > 1)
			{
				foreach (AddonObject obj in addon.addons)
				{
					foreach (AddonObject _obj in addon.addons)
					{
						int semVerCheck = SemVersion.Parse(_obj.addon.fileVersion.Remove(0, 1)).CompareTo(SemVersion.Parse(obj.addon.fileVersion.Remove(0, 1)));
						if (semVerCheck == -1)
						{
							_obj.isNewest = false;
							obj.isNewest = true;
							addon.currentDisplay = obj;

							Debug.WriteLine("Newest version detected: " + obj.addon.name + " v" + obj.addon.fileVersion);
						}
					}
				}
			}
		}

		int numAddons = 0;
		public void PopulateAddon(AddonDisplayObject addonDisplay, int x, int y, bool updatePos = false, Canvas _canvas = null)
		{
			FieldInfo gridHolder = mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
			ScrollViewer view = (ScrollViewer)gridHolder.GetValue(mainWindow);

			Canvas canvas = null;

			view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				canvas = (Canvas)view.Content;
			}));


			if (_canvas != null)
			{
				canvas = _canvas;
			}


			if (canvas == null)
				return;

			bool isExist = false;

			canvas.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() =>
			{
				if (Application.Current == null)
					return;

				Canvas addonCanvas = null;
				AddonControl addonCtrl = null;
				if (addonDisplay.displayCanvas == null)
				{
					addonCtrl = new AddonControl();
					addonCanvas = addonCtrl.AddonCanvas;

					Canvas.SetLeft(addonCtrl, x);
					Canvas.SetTop(addonCtrl, y);

					addonCanvas.Margin = new Thickness(10, 10, 0, 0);

					addonDisplay.displayCanvas = addonCanvas;
					addonDisplay.addonControl = addonCtrl;
				}
				else
				{
					addonCanvas = addonDisplay.displayCanvas;
					addonCtrl = addonDisplay.addonControl;
					//addonCanvas.Children.Clear();

					//Remove buttons so we don't get overlapping or weird artifacts
					var buttons = addonCanvas.Children.OfType<Button>().ToList();
					foreach (var button in buttons)
						addonCanvas.Children.Remove(button);

					if (updatePos)
					{
						Canvas.SetLeft(addonCtrl, x);
						Canvas.SetTop(addonCtrl, y);
					}

					isExist = true;
				}

				AddonObject addon = addonDisplay.currentDisplay;


				//addonCtrl.AddonName.Style = @Application.Current.FindResource("AddonTextStyle") as Style;
				addonCtrl.AddonName.Content = "";


				//TODO: We NEED to truncate these labels but it's slow, find a faster way!
				
			   /* if (TruncateLabel(addonCtrl.AddonName, 150))
				{
					ToolTip tt = new ToolTip
					{
						Content = new Run(addon.addon.name) { FontWeight = FontWeights.Bold }
					};
					addonCtrl.AddonName.ToolTip = tt;
				}
				*/

				//addonCtrl.AddonName.Content = addon.addon.name;
				addonCtrl.AddonName.Content = new Run((string)addon.addon.name) { FontWeight = FontWeights.Bold };
				//addonCtrl.AddonName.Content = addonDisplay.currentDisplay.addon.name;


				addonCtrl.AddonVersion.Content = addon.addon.fileVersion;
				//addonCtrl.AddonVersion.Style = @Application.Current.FindResource("AddonTextStyle") as Style;

				addonCtrl.AddonDevLabel.Content = addon.repo.Split('/')[0];
				//addonCtrl.AddonDevLabel.Style = @Application.Current.FindResource("AddonDevTextStyle") as Style;


				if ((string)addonCtrl.AddonDevLabel.Content == "unknown")
				{
					addon.addon.description = "Addon is not known and therefore not available for download.";
				}

				addon.addon.description = addon.addon.description.Replace("<br>", "\n");

				addonCtrl.AddonDescription.Text = addon.addon.description;

				ToolTip tt = new ToolTip
				{
					Content = new TextBlock() { Text = addon.addon.description, TextWrapping = TextWrapping.Wrap },
					MaxWidth = 260,
				};
				addonCtrl.AddonDescription.ToolTip = tt;
				addonCtrl.AddonDescription.TextWrapping = TextWrapping.Wrap;
				
				/*if (TruncateLabel(addonCtrl.AddonDescription, 260))
				{
					ToolTip tt = new ToolTip
					{
						Content = new TextBlock() { Text = addon.addon.description, TextWrapping = TextWrapping.Wrap },
						MaxWidth = 260,
					};
					addonCtrl.AddonDescription.ToolTip = tt;
				}*/
				//addonCtrl.AddonDescription.Style = @Application.Current.FindResource("AddonTextBoxStyle") as Style;
				//addonDesc.FontSize = 13;

				//addon.isReported = true;

				addonCtrl.DynamicNotificationBG.Visibility = Visibility.Hidden;

				//addon.isReported = true;

				if (addon.HasError())
				{

					addonCtrl.DynamicNotificationBG.Source = new BitmapImage(new Uri("pack://application:,,,/UI/error_bar.png"));
					addonCtrl.DynamicNotificationBG.Visibility = Visibility.Visible;
					// Canvas.SetLeft(addonCtrl.AddonName, 20);

					if(addon.isOutdated)
						addonCanvas.Children.Add(AddButton("Addon is outdated and might no longer work", "pack://application:,,,/UI/notification.png", 5, 7, addonDisplay, AddonButtonType.NONE));

					//int notificationX = 219;
					// if (addon.isInstalled)
					//	notificationX -= 24;
					// if (addon.hasUpdate || addon.isDownloading)
					//	notificationX -= 24;

					if(addon.isReported)
						addonCanvas.Children.Add(AddButton("Addon has been reported as broken and is no longer available for download", "pack://application:,,,/UI/notification.png", 5, 7, addonDisplay, AddonButtonType.NONE));
				}
				else
				{


				}

				if (!addon.isUnknown)
				{
					addonCanvas.Children.Add(AddButton("Developer GitHub", FontAwesomeIcon.Github, Color.FromRgb(255,255,255),  3, 415, addonDisplay, AddonButtonType.GITHUB));
					addonCanvas.Children.Add(AddButton(Language.Translate("ADDONS.README"), FontAwesomeIcon.InfoCircle, Color.FromRgb(255, 255, 255), 80, 6, addonDisplay, AddonButtonType.MORE, 14));
				}


				if (!addon.isDownloading)
				{

					if (addon.hasUpdate)
					{

						addonCtrl.DynamicNotificationBG.Source = new BitmapImage(new Uri("pack://application:,,,/UI/updating_bar.png"));
						addonCtrl.DynamicNotificationBG.Visibility = Visibility.Visible;
						addonCanvas.Children.Add(AddButton(Language.Translate("ADDONS.UPDATE"), FontAwesomeIcon.Download, Color.FromRgb(255, 255, 102), addon.HasError() ? 30 : 5, 6, addonDisplay, AddonButtonType.UPDATE));
					}
					if(addon.isInstalled)
					{
						addonCanvas.Children.Add(AddButton(Language.Translate("ADDONS.UNINSTALL"), FontAwesomeIcon.TrashOutline, Color.FromRgb(255, 102, 102), addon.isUnknown ? 80 : 60, 6, addonDisplay, AddonButtonType.REMOVE));
					}
					else if(!addon.hasUpdate)
					{
						if(!addon.isInstalled && !addon.IsQueued)
							addonCanvas.Children.Add(AddButton(Language.Translate("ADDONS.INSTALL"), FontAwesomeIcon.Download, Color.FromRgb(102, 255, 102), addon.HasError() ? 30 : 5, 6, addonDisplay, AddonButtonType.DOWNLOAD));
					}
				}
				else
				{
					addonCtrl.DynamicNotificationBG.Source = new BitmapImage(new Uri("pack://application:,,,/UI/downloading_bar.png"));
					addonCtrl.DynamicNotificationBG.Visibility = Visibility.Visible;
				}
				

				int count = 0;
				if (addonDisplay.addons.Count > 0)
				{
					foreach (AddonObject obj in addonDisplay.addons)
					{
						if (obj.addon.name == addon.addon.name && obj.addon.fileVersion == addon.addon.fileVersion)
							continue;
						count++;
					}
				}



				if (count > 0)
				{

					ComboBox list = new ComboBox();

					list.SelectionChanged += AddonSelect_SelectionChanged;

					foreach (AddonObject obj in addonDisplay.addons)
					{
						if (obj.addon.name == addon.addon.name && obj.addon.fileVersion == addon.addon.fileVersion)
							continue;

						if (obj.repo == null)
							obj.repo = "unknown/unknow";

						list.Items.Add(obj.addon.fileVersion + " by " + obj.repo.Split('/')[0]);

					}

					list.DataContext = addonDisplay;

					Canvas.SetTop(list, 5);
					Canvas.SetLeft(list, 257);

					//Hiding this in the background
					Panel.SetZIndex(list, -100);

					Button versionSelectButton = AddButton("Version Select", FontAwesomeIcon.ArrowCircleOutlineDown, Color.FromRgb(255, 255, 255), 4, 280, addonDisplay, AddonButtonType.OPENLIST, 16, true, list);
					versionSelectButton.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));

					addonCanvas.Children.Add(versionSelectButton);

					addonCanvas.Children.Add(list);

				}


				if (addonDisplay.displayCanvas == null)
					canvas.Children.Add(addonCanvas);
				else
				{

					//AddonControl _pCanvas = (AddonControl)addonCanvas.Parent;
					//if (_pCanvas != null)
					// _pCanvas.Children.Remove(addonCanvas);

					if (!isExist)
					{
						canvas.Children.Remove(addonCtrl);


						canvas.Children.Add(addonCtrl);
					}
				}
			   // addonCtrl.Dispatcher.Invoke(DispatcherPriority.Render, emptyDelegate);

				//canvas.Dispatcher.Invoke(DispatcherPriority.Render, emptyDelegate);

			}));

			addonDisplay.tabManager = this;
		}
		

		Button AddButton(string name, string image, int x, int y, AddonDisplayObject addon, AddonButtonType buttonType, bool hasToolTip = true, ComboBox list = null)
		{
			//we're gonna take the template from the discord button
			FieldInfo discHolder = mainWindow.GetType().GetField("discordButton", BindingFlags.Instance | BindingFlags.Public);
			Button disc = (Button)discHolder.GetValue(mainWindow);

			Button githubButton = new Button
			{
				Cursor = System.Windows.Input.Cursors.Hand,
				RenderTransformOrigin = new Point(-0.058, 0.493),
				BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
				Foreground = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
				Template = disc.Template,
				HorizontalAlignment = HorizontalAlignment.Left,
				Width = 21,
				Height = 21,
				VerticalAlignment = VerticalAlignment.Bottom
			};


			Canvas.SetTop(githubButton, x);
			Canvas.SetLeft(githubButton, y);
			var brush = new ImageBrush
			{
				ImageSource = new BitmapImage(new Uri(image))
			};
			githubButton.Background = brush;
			ToolTip tt = new ToolTip
			{
				Content = new Run(name) { FontWeight = FontWeights.Bold }
			};
			if (hasToolTip)
				githubButton.ToolTip = tt;

			githubButton.Click += AddonButton_Click;
			githubButton.DataContext = new AddonButton() { buttonType = buttonType, addon = addon, list = list };

			return githubButton;
		}

		Button AddButton(string name, FontAwesomeIcon icon, Color color, int x, int y, AddonDisplayObject addon, AddonButtonType buttonType, double size=17, bool hasToolTip = true, ComboBox list = null)
		{
			//we're gonna take the template from the discord button
			FieldInfo discHolder = mainWindow.GetType().GetField("discordButton", BindingFlags.Instance | BindingFlags.Public);
			Button disc = (Button)discHolder.GetValue(mainWindow);

			Button githubButton = new Button
			{
				Cursor = System.Windows.Input.Cursors.Hand,
				RenderTransformOrigin = new Point(-0.058, 0.493),
				BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
				Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
				Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
				HorizontalAlignment = HorizontalAlignment.Left,
				Template = disc.Template,
				Width = 21,
				Height = 21,
				VerticalAlignment = VerticalAlignment.Bottom
			};


			Canvas.SetTop(githubButton, x);
			Canvas.SetLeft(githubButton, y);

			githubButton.FontSize = size;
			
			Awesome.SetContent(githubButton, icon);
			githubButton.Foreground = new SolidColorBrush(color);

			ToolTip tt = new ToolTip
			{
				Content = new Run(name) { FontWeight = FontWeights.Bold }
			};
			if (hasToolTip)
				githubButton.ToolTip = tt;

			githubButton.Click += AddonButton_Click;
			githubButton.DataContext = new AddonButton() { buttonType = buttonType, addon = addon, list = list };

			return githubButton;
		}

		Button AddSpinner(string name, ImageSource image, int x, int y)
		{
			//we're gonna take the template from the discord button
			FieldInfo discHolder = mainWindow.GetType().GetField("discordButton", BindingFlags.Instance | BindingFlags.Public);
			Button disc = (Button)discHolder.GetValue(mainWindow);

			Button githubButton = new Button
			{
				// Cursor = System.Windows.Input.Cursors.Hand,
				RenderTransformOrigin = new Point(-0.058, 0.493),
				BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
				Foreground = new SolidColorBrush(Color.FromArgb(255,255,255,255)),
				Template = disc.Template,
				HorizontalAlignment = HorizontalAlignment.Left,
				Width = 21,
				Height = 21,
				VerticalAlignment = VerticalAlignment.Bottom
			};


			// image.Spin = true;

			Canvas.SetTop(githubButton, x);
			Canvas.SetLeft(githubButton, y);
			var brush = new ImageBrush
			{
				ImageSource = image
			};
			githubButton.Background = brush;

			return githubButton;
		}


		void UpdateCanvasSize(Canvas _canvas, List<AddonDisplayObject> _addonDisplayList)
		{
			Canvas canvas = _canvas;

			UpdateCurrentCanvasSize(canvas);
		}

		void UpdateCurrentCanvasSize()
		{
			FieldInfo gridHolder = mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
			ScrollViewer view = (ScrollViewer)gridHolder.GetValue(mainWindow);

			Canvas canvas = null;

			view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				canvas = (Canvas)view.Content;
			}));

			UpdateCurrentCanvasSize(canvas);
		}

		void UpdateCurrentCanvasSize(Canvas canvas)
		{
			canvas.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				if(_currentCount > 0)
					canvas.Height = 30 + ((_currentCount-1) * 120);
				else
					canvas.Height = 30;
			}));
		}

		async void PopulateTab(string server)
		{

			FieldInfo gridHolder = mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
			ScrollViewer view = (ScrollViewer)gridHolder.GetValue(mainWindow);

			Canvas addonCanvas = new Canvas();
			view.Content = addonCanvas;

			Canvas canvas = (Canvas)view.Content;


			UpdateTabElements();


			try
			{
				int res = await Task.Run(() => PopulateRepo(canvas, server));
			}
			catch(Exception)
			{ }
			//isRepoDownload = false;
			//populatedTabs.Add(currentTab);

			



			//file cleanup
			JsonManager.RemoveFile(server + "managers.json");
			JsonManager.RemoveFile(server + "repo.json");

			finishedLoadingNum++;

			if (finishedLoadingNum == addonSources.Count)
			{
				//int res = await Task.Run(() => DisplayAddons(canvas));

				Debug.WriteLine("Finished Loading Repos!");

				canvas.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
				{
					canvas.Height = 50 + (addonDisplayList.Count * 120);
				}));

				isFinishedLoading = true;
				await Task.Run(() => installedAddons.Populate());
				//installedAddons.Populate();

				//RebuildDisplayList();
			}

		}

		void Tab_DownloadComplete(object sender, AsyncCompletedEventArgs e)
		{
			Debug.WriteLine(e.Error);

			if (!e.Cancelled && e.Error == null)
			{

				string server = ((WebClient)(sender)).QueryString["server"];
				PopulateTab(server);
			}
			else
			{
			   // isRepoDownload = false;
			}
		}

		void Tab_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			//FieldInfo dataHolder = mainWindow.GetType().GetField(tabName + "ProgressBar", BindingFlags.Instance | BindingFlags.Public);

			//ProgressBar _data = (ProgressBar)dataHolder.GetValue(mainWindow);

			//_data.Value = e.ProgressPercentage;

		}

		private Size MeasureString(Label label)
		{
			//Run run = (Run)label.Content;
			var formattedText = new FormattedText(
				(string)label.Content,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				 new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch),
				14,
				Brushes.Black,
				new NumberSubstitution(),
				TextFormattingMode.Display);

			return new Size(formattedText.Width, formattedText.Height);
		}


		private bool TruncateLabel(Label label, int maxWidth)
		{
			bool isTruncated = false;

			if (MeasureString(label).Width > maxWidth)
			{
				label.Content = ((string)label.Content).Remove(((string)label.Content).Length - (maxWidth+1) );
				isTruncated = true;
			}

			if (isTruncated)
			{
				string desc = (string)label.Content;
				if (desc[desc.Length - 1] == ' ')
					desc = desc.Remove(desc.Length - 1);

				label.Content = desc + "...";
			}

			return isTruncated;
		}

		public void RebuildAddonList()
		{
			if (addonDisplayList != null)
			{
				FieldInfo gridHolder = mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
				ScrollViewer view = (ScrollViewer)gridHolder.GetValue(mainWindow);

				Canvas _canvas = null;
				view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
				{
					//view.Content = new Canvas();
					_canvas = (Canvas)view.Content;
				}));

				RebuildDisplayList(_canvas);
				UpdateCanvasSize(_canvas, addonDisplayList);
			}

			Debug.WriteLine("Rebuilding");
		}

		public void RebuildDisplayList(Canvas _canvas = null)
		{
			Debug.WriteLine("Rebuilding display list");


			int y = 0;

			foreach (AddonDisplayObject addonDisplay in addonDisplayList)
			{
				PopulateAddon(addonDisplay, 0, y*120, true, _canvas);


				
			}
		}
#pragma warning disable IDE1006 // Naming Styles
		void emptyDel()
		{ }

	}


	public enum AddonButtonType
	{
		NONE,
		GITHUB,
		MORE,
		DOWNLOAD,
		UPDATE,
		REMOVE,
		OPENLIST
	}

	public class AddonButton
	{
		public AddonButtonType buttonType;
		public AddonDisplayObject addon;
		public ComboBox list;
	}

	//Json Managers
	public class ManagersDependency
	{
		public string url { get; set; }
	}

	public class ManagersSource
	{
		public string repo { get; set; }
	}

	public class ManagersObject
	{
		public List<ManagersDependency> dependencies { get; set; }
		public List<ManagersSource> sources { get; set; }
	}

	//Json addons

	public class AddonsObject
	{
		public string tosversion { get; set; }
		public string name { get; set; }
		public string file { get; set; }
		public string extension { get; set; }
		public string fileVersion { get; set; }
		public string releaseTag { get; set; }
		public string unicode { get; set; }
		public string description { get; set; }
		public List<string> tags { get; set; }
	}
#pragma warning restore IDE1006 // Naming Styles
}
