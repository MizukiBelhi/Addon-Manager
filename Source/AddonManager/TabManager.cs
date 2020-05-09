using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FontAwesome.WPF;

namespace AddonManager
{
	public class TabManager
	{
		private List<AddonDisplayObject> addonDisplayList = new List<AddonDisplayObject>();
		private List<AddonDisplayObject> addonDisplayListCopy = new List<AddonDisplayObject>();

		protected delegate void EmptyDelegate();

		private EmptyDelegate emptyDelegate;

		private MainWindow mainWindow;
		private readonly InstalledAddons installedAddons;


		private readonly string repoSource = "https://raw.githubusercontent.com/{0}/master/addons.json";

		public bool isFinishedLoading;
		private int finishedLoadingNum;

		private int sortMode;
		private int sortBy;


		private bool isSorting;
		private bool isSearching;
		private bool hasSorted; //Hack


		private int displayMode;


		private readonly Dictionary<string, string> addonSources;


		private int _onPage; //Current page we're on

		private int _maxPageCount = 10; //Amount of addons displayed for each page

		private int _currentCount; //Current amount of addons displayed
		private int _lastIndex;

		//Token to cancel sorting
		private CancellationTokenSource cts = new CancellationTokenSource();


		public TabManager(Dictionary<string, string> addonSources)
		{
			this.addonSources = addonSources;
			// this.source = managersUrl;
			//this.tabName = tabName;
			mainWindow = (MainWindow) Application.Current.MainWindow;

			installedAddons = new InstalledAddons();

			installedAddons.Init(mainWindow, this);

			BrokenAddons.Load();


			Load();
		}


		public async void SearchListAsync(string term)
		{
			try
			{
				cts = new CancellationTokenSource();
				await Task.Run(() => SearchList(term, cts.Token), cts.Token);
			}
			catch (Exception)
			{
			}
		}


		public void SearchList(string sterm, CancellationToken cancellationToken)
		{
			var searchTerm = sterm.ToLower().Split(' ');

			//The search term is "Search..." when it is empty automatically
			if (sterm == "Search..." || sterm == "")
			{
				if (addonDisplayListCopy.Count > 0)
				{
					addonDisplayList = new List<AddonDisplayObject>(addonDisplayListCopy);

					isSearching = false;

					_onPage = 0;

					addonDisplayListCopy.Clear();

					SortListAsync();
				}

				return;
			}

			if (addonDisplayListCopy.Count > 0)
				addonDisplayList = new List<AddonDisplayObject>(addonDisplayListCopy);

			addonDisplayListCopy = new List<AddonDisplayObject>(addonDisplayList);
			var searchResult = new List<AddonDisplayObject>();

			foreach (string term in searchTerm)
			{
				//name, author
				searchResult.AddRange(
					addonDisplayList.FindAll(p =>
						p.currentDisplay.addon.name.ToLower().Contains(term) ||
						p.currentDisplay.repo.Split('/')[0].ToLower().Contains(term)
					));


				//tags  probably can be simplified
				foreach (AddonDisplayObject obj in addonDisplayList)
					if (obj.currentDisplay.addon.tags != null)
					{
						int tagCount = obj.currentDisplay.addon.tags.FindAll(r => r.Contains(term)).Count;
						if (tagCount > 0 && !searchResult.Contains(obj))
							searchResult.Add(obj);

						cancellationToken.ThrowIfCancellationRequested();
					}
			}

			addonDisplayList = new List<AddonDisplayObject>(searchResult).Distinct().ToList();
			searchResult.Clear();


			UpdateCurrentCanvasSize();

			isSearching = true;

			if (isSorting)
			{
				cts.Cancel();
				isSorting = false;
			}

			SortListAsync();
		}

		public void SortList(string by, int mode)
		{
			sortMode = mode;

			switch (by)
			{
				case "Name":
					sortBy = 0;
					break;
				case "Date":
					sortBy = 1;
					break;
				case "Dev":
					sortBy = 2;
					break;
			}

			if (isSorting)
			{
				cts.Cancel();
				isSorting = false;
			}

			SortListAsync();
		}

		private async void SortListAsync()
		{
			hasSorted = true;
			try
			{
				cts = new CancellationTokenSource();
				await Task.Run(() => SortList(cts.Token), cts.Token);
			}
			catch (Exception)
			{
			}
		}


		public void SortList(CancellationToken cancellationToken)
		{
			isSorting = true;

			cancellationToken.ThrowIfCancellationRequested();

			if (sortMode == 0)
				switch (sortBy)
				{
					case 0:
						addonDisplayList.Sort((x, p) =>
							string.Compare(x.currentDisplay.addon.name, p.currentDisplay.addon.name,
								StringComparison.Ordinal));
						break;
					case 1:
						addonDisplayList = addonDisplayList.OrderBy(x => x.currentDisplay.date).ToList();
						addonDisplayList.Reverse();
						break;
					case 2:
						addonDisplayList.Sort((x, p) =>
							string.Compare(x.currentDisplay.repo, p.currentDisplay.repo,
								StringComparison.Ordinal));
						break;
				}
			else
				switch (sortBy)
				{
					case 0:
						addonDisplayList.Sort((x, p) =>
							string.Compare(p.currentDisplay.addon.name, x.currentDisplay.addon.name,
								StringComparison.Ordinal));
						break;
					case 1:
						addonDisplayList = addonDisplayList.OrderBy(x => x.currentDisplay.date).ToList();
						break;
					case 2:
						addonDisplayList.Sort((x, p) =>
							string.Compare(p.currentDisplay.repo, x.currentDisplay.repo,
								StringComparison.Ordinal));
						break;
				}


			FieldInfo gridHolder =
				mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
			if (gridHolder == null) return;

			ScrollViewer view = (ScrollViewer) gridHolder.GetValue(mainWindow);

			Canvas canvas;

			view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				canvas = (Canvas) view.Content;
				canvas.Children.Clear();
				canvas.Dispatcher.Invoke(DispatcherPriority.Render, emptyDelegate);
			}));

			_lastIndex = 0;
			_currentCount = 0;
			int y = 0;
			int addonIndex = 0;
			int addPgCt = _onPage == 0 ? 1 : 0; //Hack
			foreach (AddonDisplayObject obj in addonDisplayList)
			{
				//Maximum page count reached
				if (addonIndex >= (_onPage + addPgCt) * _maxPageCount) break;

				addonIndex++;

				//This addon is already displayed
				if (addonIndex <= _currentCount) continue;

				if (obj.Invalid) continue;


				_currentCount++;
				obj.displayCanvas = null;

				int nextIndex = _lastIndex * 120;

				if (displayMode != 0)
					nextIndex = _lastIndex * 50;

				PopulateAddon(obj, 0, nextIndex, true);
				//Debug.WriteLine($"Adding {obj.currentDisplay.addon.name} @ Y {_lastIndex * 120} IDX {_lastIndex}");

				y++;
				_lastIndex++;

				cancellationToken.ThrowIfCancellationRequested();
			}

			Debug.WriteLine(_currentCount);
			isSorting = false;
			UpdateCurrentCanvasSize();
			//UpdateCurrentCanvasSize(canvas);
		}


		public void SwapDisplayMode(int dpm)
		{
			displayMode = dpm;

			_maxPageCount = displayMode==0 ? 10: 15;

			SortListAsync();
		}

		private void Load()
		{
			if (emptyDelegate == null)
				emptyDelegate += emptyDel;

			foreach (var source in addonSources)
			{
				mainWindow = (MainWindow) Application.Current.MainWindow;
				using (WebClient wc = new WebClient())
				{
					wc.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
					wc.Encoding = Encoding.UTF8;
					wc.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
					wc.Headers.Add(
						"User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
					wc.DownloadStringCompleted += Tab_DownloadComplete;
					wc.DownloadProgressChanged += Tab_DownloadProgressChanged;
					Debug.WriteLine("Download " + source.Key + " server list");
					wc.QueryString.Add("server", source.Key);
					wc.DownloadStringAsync(new Uri(source.Value));
				}
			}
		}


		public void UpdateTabElements()
		{
			FieldInfo sortHolder = mainWindow.GetType()
				.GetField("AddonSortButton", BindingFlags.Instance | BindingFlags.Public);
			if (sortHolder == null) return;
			Button sort = (Button) sortHolder.GetValue(mainWindow);

			sort.Background = new ImageBrush
			{
				ImageSource = ImageAwesome.CreateImageSource(FontAwesomeIcon.Sort,
					new SolidColorBrush(Color.FromRgb(255, 255, 255)))
			};
			sort.ClickMode = ClickMode.Release;
			sort.Click += SortButton_Click;
		}


		private void SortButton_Click(object sender, RoutedEventArgs e)
		{
			e.Handled = true;
			if (isSorting)
			{
				cts.Cancel();
				isSorting = false;
			}


			sortMode = sortMode == 0 ? 1 : 0;


			SortListAsync();
		}

		private static void AddonButton_Click(object sender, RoutedEventArgs e)
		{
			if (!(sender is Button button)) return;
			AddonButton addonButton = (AddonButton) button.DataContext;

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
					addon.DisplayReadme();
					break;
				case AddonButtonType.UPDATE:
					addon.StartUpdate();
					break;
				case AddonButtonType.OPENLIST:
					if (addonButton.list != null)
						addonButton.list.IsDropDownOpen = true;
					break;
				case AddonButtonType.NONE:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}


		private string GetDaySuffix(int day)
		{
			switch (day)
			{
				case 1:
				case 21:
				case 31:
					return "st";
				case 2:
				case 22:
					return "nd";
				case 3:
				case 23:
					return "rd";
				default:
					return "th";
			}
		}

		private int PopulateRepo(string data, Canvas canvas, string server)
		{
			ManagersObject managers = JsonManager.LoadString<ManagersObject>(data);

			mainWindow.UpdateLoadingBarMax(managers.sources.Count);

			foreach (ManagersSource source in managers.sources)
			{
				string repoData;
				if ((repoData = LoadRepo(source.repo, server)) == null)
					continue;

				var addons = JsonManager.LoadString<List<AddonsObject>>(repoData);


				mainWindow.UpdateLoadingBarMax(addons.Count);

				foreach (AddonsObject addon in addons)
				{
					if (addon == null)
					{
						mainWindow.UpdateLoadingBarMax(-1);
						continue;
					}

					numAddons++;

					AddonObject addonObj = new AddonObject {repo = source.repo, addon = addon, dependencies = managers.dependencies};
					if (addonObj.date == 0 && MainWindow.settings.LoadDates)
					{
						DateTimeOffset t = DownloadManager.GetAddonDate(addonObj);
						addonObj.date = long.Parse(t.ToString("yyyyMMddhhmmss"));
						if (addonObj.date == 999999999999)
							addonObj.date = -1;

						//string.Format("{0:dddd dd}{1} {0:MMMM yyyy}", DateTime.Now, GetDaySuffix(int.Parse(t.ToString("d"))));
						addonObj.readdate = t.ToString("ddddd, MMMM d[SU], yyyy @ hh:mm tt");
						addonObj.readdate = addonObj.readdate.Replace("[SU]", GetDaySuffix(int.Parse(t.ToString("dd"))));
					}
					else
					{
						addonObj.date = -1;
						addonObj.readdate = "";
					}

					AddonDisplayObject newAddonDisplay = new AddonDisplayObject
					{
						currentDisplay = addonObj
					};

					if (installedAddons.CheckInstalled(addonObj, addonObj.repo))
					{
						newAddonDisplay.Invalid = true;
						mainWindow.UpdateLoadingBarMax(-1);
						continue;
					}

					addonDisplayList.Add(newAddonDisplay);

					mainWindow.UpdateLoadingBarCurrent(1);
				}

				mainWindow.UpdateLoadingBarCurrent(1);
			}

			return 0;
		}


		public async void DisplayAddons()
		{
			FieldInfo gridHolder =
				mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
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


			await Task.Run(() => DisplayAddons(canvas));

			Debug.WriteLine("Loaded " + numAddons + " Addons.");
		}

		//Addons <->
		public int DisplayAddons(Canvas canvas)
		{
			if (canvas == null)
				return -1;

			var _installedAddons = DownloadManager.GetInstalledAddons();
			var _addonDisplayList = new List<AddonDisplayObject>(addonDisplayList);

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

				addonObj.SetupBroken();

				if (addonDisplay.addons == null) addonDisplay.addons = new List<AddonObject> {addonObj};


				if (addonDisplayList.Count > 0)
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
						if (displayObject.addons == null) displayObject.addons = new List<AddonObject>();

						string _addonFileName = displayObject.currentDisplay.addon.file;

						//we only need to compare against the displayed addon
						if (_addonFileName == addonFileName)
						{
							if (addonObj < displayObject.currentDisplay)
							{
								addonDisplay.OverrideCurrentDisplay(displayObject.currentDisplay);
								addonDisplayList[addonDisplayList.FindIndex(x => x == displayObject)].Invalid = true;
							}
							else
							{
								addonDisplay.AddAddon(displayObject.currentDisplay);
								addonDisplayList[addonDisplayList.FindIndex(x => x == displayObject)].Invalid = true;
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
			
			_lastIndex = 0;
			_currentCount = 0;
			//Update positions
			int addonIndex = 0;
			foreach (AddonDisplayObject addonDisplay in addonDisplayList)
			{
				if (addonIndex >= (_onPage+1) * _maxPageCount) break;

				addonIndex++;

				//This addon is already displayed
				if (addonIndex <= _currentCount) continue;

				if (addonDisplay.Invalid) continue;


				_currentCount++;
				addonDisplay.displayCanvas = null;

				int nextIndex = _lastIndex * 120;

				if (displayMode != 0)
					nextIndex = _lastIndex * 50;

				PopulateAddon(addonDisplay, 0, nextIndex);
				//Debug.WriteLine($"Adding {obj.currentDisplay.addon.name} @ Y {_lastIndex * 120} IDX {_lastIndex}");
				_lastIndex++;
			}
		}

		public void RemoveFromList(AddonDisplayObject addon)
		{
			if (addonDisplayList.Contains(addon))
			{
				//Remove from main list
				addonDisplayList.Remove(addon);

				//Remove from secondary list if applicable
				if (addonDisplayListCopy.Contains(addon))
					addonDisplayListCopy.Remove(addon);

				//PopulateAddon(addon, 0, 0, true);

				//UpdateListPositions();
				DisplayAddons();
			}
		}

		public void AddToList(AddonDisplayObject addon)
		{
			addonDisplayList.Add(addon);
			if (isSearching)
				addonDisplayListCopy.Add(addon);
			//UpdateListPositions();
			DisplayAddons();
		}

		public void AddToInstalledAddons(AddonDisplayObject addon)
		{
			installedAddons.AddAddon(addon);
		}

		public void LoadNextPage(bool f=false)
		{
			//We're already showing all addons
			if (_currentCount >= addonDisplayList.Count) return;

			_onPage++;
			if (f)
				_onPage++;

			FieldInfo gridHolder =
				mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
			if (gridHolder == null) return;
			ScrollViewer view = (ScrollViewer) gridHolder.GetValue(mainWindow);

			Canvas canvas = null;

			view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { canvas = (Canvas) view.Content; }));


			canvas.Dispatcher.Invoke(DispatcherPriority.Normal,
				new Action(() => { canvas.Height = 30 + _currentCount + 10 * 120; }));


			int y = _currentCount;
			//TODO: Not sure what is happening here but this needs to be fixed.
			if (hasSorted)
				y -= 2;
			int addonIndex = 0;
			foreach (AddonDisplayObject addonDisplay in addonDisplayList)
			{
				//Maximum page count reached
				if (addonIndex >= _onPage * _maxPageCount) break;

				addonIndex++;

				//This addon is already displayed
				if (addonIndex <= _currentCount) continue;

				if (addonDisplay.Invalid) continue;


				_currentCount++;
				int nextIndex = _lastIndex * 120;

				if (displayMode != 0)
					nextIndex = _lastIndex * 50;


				PopulateAddon(addonDisplay, 0, nextIndex, true);
				//Debug.WriteLine($"LoadNextPage()@@Adding {addonDisplay.currentDisplay.addon.name} @ Y {_lastIndex*120} IDX {_lastIndex}");
				_lastIndex++;

				y++;
			}

			UpdateCurrentCanvasSize(canvas);
		}


		private void AddonSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cbox = sender as ComboBox;
			dynamic selectedItem = cbox.SelectedItem;
			string selectedLang = selectedItem.ToLower();


			AddonDisplayObject addon = (AddonDisplayObject) cbox.DataContext;

			AddonObject selectedAddon = null;


			foreach (AddonObject obj in addon.addons)
				if (selectedLang == (obj.addon.fileVersion + " by " + obj.repo.Split('/')[0]).ToLower())
				{
					selectedAddon = obj;
					break;
				}

			addon.currentDisplay = selectedAddon;
			PopulateAddon(addon, 0, 0);

			Debug.WriteLine("AddonSelect: " + selectedLang);
		}


		private string LoadRepo(string repo, string server)
		{
			try
			{
				using (WebClient wc = new WebClient())
				{
					wc.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
					string useRepo = string.Format(repoSource, repo);
					wc.Encoding = Encoding.UTF8;
					wc.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
					wc.Headers.Add(
						"User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
					// wc.DownloadFileCompleted += Manager_DownloadComplete;
					//  wc.DownloadProgressChanged += Manager_DownloadProgressChanged;
					return wc.DownloadString(new Uri(useRepo));
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine("Error trying to download repo: " + repo);
				Debug.WriteLine(e.Message);
				return null;
			}
		}


		private int numAddons;

		public void PopulateAddon(AddonDisplayObject addonDisplay, int x, int y, bool updatePos = false,
			Canvas _canvas = null)
		{
			FieldInfo gridHolder =
				mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
			if (gridHolder == null) return;
			ScrollViewer view = (ScrollViewer) gridHolder.GetValue(mainWindow);

			Canvas canvas = null;

			view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { canvas = (Canvas) view.Content; }));


			if (_canvas != null) canvas = _canvas;


			if (canvas == null)
			{
				Debug.WriteLine("Canvas is null");
				return;
			}

			//bool isExist = false;

			canvas.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() =>
			{
				if (Application.Current == null)
					return;

				Canvas addonCanvas;
				AddonControl addonCtrl;
				if (addonDisplay.displayCanvas == null)
				{
					addonCtrl = new AddonControl();
					addonCanvas = addonCtrl.AddonCanvas;

					Canvas.SetLeft(addonCtrl, (displayMode==0?x:10+x));
					Canvas.SetTop(addonCtrl, (displayMode==0?y:10+y));

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
					foreach (Button button in buttons)
						addonCanvas.Children.Remove(button);

					if (updatePos)
					{
						Canvas.SetLeft(addonCtrl, (displayMode == 0 ? x : 10 + x));
						Canvas.SetTop(addonCtrl, (displayMode == 0 ? y : 10 + y));
					}

					//isExist = true;
				}

				if (displayMode == 0)
				{
					addonCtrl.AddonCanvas.Visibility = Visibility.Visible;
					addonCtrl.AddonCanvasCompact.Visibility = Visibility.Hidden;
				}
				else
				{
					addonCtrl.AddonCanvas.Visibility = Visibility.Hidden;
					addonCtrl.AddonCanvasCompact.Visibility = Visibility.Visible;
				}

				AddonObject addon = addonDisplay.currentDisplay;


				//addonCtrl.AddonName.Style = @Application.Current.FindResource("AddonTextStyle") as Style;
				addonCtrl.AddonName.Content = "";
				addonCtrl.AddonNamec.Content = "";


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
				addonCtrl.AddonName.Content = new Run(addon.addon.name) {FontWeight = FontWeights.Bold};
				addonCtrl.AddonNamec.Content = new Run(addon.addon.name) { FontWeight = FontWeights.Bold };
				//addonCtrl.AddonName.Content = addonDisplay.currentDisplay.addon.name;


				addonCtrl.AddonVersion.Content = addon.addon.fileVersion;
				addonCtrl.AddonVersionc.Content = addonCtrl.AddonVersion.Content;
				//addonCtrl.AddonVersion.Style = @Application.Current.FindResource("AddonTextStyle") as Style;

				addonCtrl.AddonDevLabel.Content = addon.repo.Split('/')[0];
				addonCtrl.AddonDevLabelc.Content = addonCtrl.AddonDevLabel.Content;
				//addonCtrl.AddonDevLabel.Style = @Application.Current.FindResource("AddonDevTextStyle") as Style;

				addonCtrl.AddonDate.Content = addon.readdate;
				addonCtrl.AddonDatec.Content = addonCtrl.AddonDate.Content;
				//addonCtrl.AddonDate.Content = string.Format("IDX: {0} Y: {1}", (y / 120), y);


				if ((string) addonCtrl.AddonDevLabel.Content == "unknown")
					addon.addon.description = "Addon is not known and therefore not available for download.";

				addon.addon.description = addon.addon.description.Replace("<br>", "\n");

				addonCtrl.AddonDescription.Text = addon.addon.description;

				ToolTip tt = new ToolTip
				{
					Content = new TextBlock {Text = addon.addon.description, TextWrapping = TextWrapping.Wrap},
					MaxWidth = 260
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
				addonCtrl.DynamicNotificationBGc.Visibility = Visibility.Hidden;

				//addon.isReported = true;

				if (addon.HasError())
				{
					addonCtrl.DynamicNotificationBG.Source =
						new BitmapImage(new Uri("pack://application:,,,/UI/error_bar.png"));
					addonCtrl.DynamicNotificationBG.Visibility = Visibility.Visible;
					addonCtrl.DynamicNotificationBGc.Visibility = Visibility.Visible;
					// Canvas.SetLeft(addonCtrl.AddonName, 20);

					if (addon.isOutdated)
						addonCanvas.Children.Add(AddButton("Addon is outdated and might no longer work",
							"pack://application:,,,/UI/notification.png", 5, 7, addonDisplay, AddonButtonType.NONE));

					//int notificationX = 219;
					// if (addon.isInstalled)
					//	notificationX -= 24;
					// if (addon.hasUpdate || addon.isDownloading)
					//	notificationX -= 24;

					if (addon.isReported)
						addonCanvas.Children.Add(AddButton(
							"Addon has been reported as broken and is no longer available for download",
							"pack://application:,,,/UI/notification.png", 5, 7, addonDisplay, AddonButtonType.NONE));
				}

				if (!addon.isUnknown)
				{
					addonCanvas.Children.Add(AddButton("Developer GitHub", FontAwesomeIcon.Github,
						Color.FromRgb(255, 255, 255), 3, 415, addonDisplay, AddonButtonType.GITHUB));
					addonCanvas.Children.Add(AddButton(Language.Translate("ADDONS.README"), FontAwesomeIcon.InfoCircle,
						Color.FromRgb(255, 255, 255), 80, 6, addonDisplay, AddonButtonType.MORE, 14));
				}


				if (!addon.isDownloading)
				{
					if (addon.hasUpdate)
					{
						addonCtrl.DynamicNotificationBG.Source =
							new BitmapImage(new Uri("pack://application:,,,/UI/updating_bar.png"));
						addonCtrl.DynamicNotificationBG.Visibility = Visibility.Visible;
						addonCtrl.DynamicNotificationBGc.Visibility = Visibility.Visible;
						addonCanvas.Children.Add(AddButton(Language.Translate("ADDONS.UPDATE"),
							FontAwesomeIcon.Download, Color.FromRgb(255, 255, 102), addon.HasError() ? 30 : 5, 6,
							addonDisplay, AddonButtonType.UPDATE));
					}

					if (addon.isInstalled)
						addonCanvas.Children.Add(AddButton(Language.Translate("ADDONS.UNINSTALL"),
							FontAwesomeIcon.TrashOutline, Color.FromRgb(255, 102, 102), addon.isUnknown ? 80 : 60, 6,
							addonDisplay, AddonButtonType.REMOVE));
					else if (!addon.hasUpdate)
						if (!addon.isInstalled && !addon.IsQueued && !addon.isReported)
							addonCanvas.Children.Add(AddButton(Language.Translate("ADDONS.INSTALL"),
								FontAwesomeIcon.Download, Color.FromRgb(102, 255, 102), addon.HasError() ? 30 : 5, 6,
								addonDisplay, AddonButtonType.DOWNLOAD));
				}
				else
				{
					addonCtrl.DynamicNotificationBG.Source =
						new BitmapImage(new Uri("pack://application:,,,/UI/downloading_bar.png"));
					addonCtrl.DynamicNotificationBG.Visibility = Visibility.Visible;
				}


				int count = 0;
				if (addonDisplay.addons != null)
					if (addonDisplay.addons.Count > 0)
						foreach (AddonObject obj in addonDisplay.addons)
						{
							if (obj.addon.name == addon.addon.name && obj.addon.fileVersion == addon.addon.fileVersion)
								continue;
							count++;
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
							obj.repo = "unknown/unknown";

						list.Items.Add(obj.addon.fileVersion + " by " + obj.repo.Split('/')[0]);
					}

					list.DataContext = addonDisplay;

					Canvas.SetTop(list, 5);
					Canvas.SetLeft(list, 257);

					//Hiding this in the background
					Panel.SetZIndex(list, -100);

					Button versionSelectButton = AddButton("Version Select", FontAwesomeIcon.ArrowCircleOutlineDown,
						Color.FromRgb(255, 255, 255), 4, 280, addonDisplay, AddonButtonType.OPENLIST, 16, true, list);
					versionSelectButton.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));

					addonCanvas.Children.Add(versionSelectButton);

					addonCanvas.Children.Add(list);
				}


				if (addonDisplay.displayCanvas == null)
				{
					canvas.Children.Add(addonCanvas);
				}
				else
				{
					try
					{
						canvas.Children.Remove(addonCtrl);

						if (!canvas.Children.Contains(addonCtrl))
							canvas.Children.Add(addonCtrl);
					}catch
					{}

					//if (updatePos)
					{
						//Canvas.SetLeft(addonCtrl, x);
						//Canvas.SetTop(addonCtrl, y);
					}
					//else canvas.Children.Add(addonCanvas);
				}

			}));

			addonDisplay.tabManager = this;
		}


		private Button AddButton(string name, string image, int x, int y, AddonDisplayObject addon,
			AddonButtonType buttonType, bool hasToolTip = true, ComboBox list = null)
		{
			//we're gonna take the template from the discord button
			FieldInfo discHolder = mainWindow.GetType()
				.GetField("discordButton", BindingFlags.Instance | BindingFlags.Public);
			Button disc = (Button) discHolder.GetValue(mainWindow);

			Button githubButton = new Button
			{
				Cursor = Cursors.Hand,
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
			ImageBrush brush = new ImageBrush
			{
				ImageSource = new BitmapImage(new Uri(image))
			};
			githubButton.Background = brush;
			ToolTip tt = new ToolTip
			{
				Content = new Run(name) {FontWeight = FontWeights.Bold}
			};
			if (hasToolTip)
				githubButton.ToolTip = tt;

			githubButton.Click += AddonButton_Click;
			githubButton.DataContext = new AddonButton {buttonType = buttonType, addon = addon, list = list};

			return githubButton;
		}

		private Button AddButton(string name, FontAwesomeIcon icon, Color color, int x, int y, AddonDisplayObject addon,
			AddonButtonType buttonType, double size = 17, bool hasToolTip = true, ComboBox list = null)
		{
			//we're gonna take the template from the discord button
			FieldInfo discHolder = mainWindow.GetType()
				.GetField("discordButton", BindingFlags.Instance | BindingFlags.Public);
			Button disc = (Button) discHolder.GetValue(mainWindow);

			Button githubButton = new Button
			{
				Cursor = Cursors.Hand,
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
				Content = new Run(name) {FontWeight = FontWeights.Bold}
			};
			if (hasToolTip)
				githubButton.ToolTip = tt;

			githubButton.Click += AddonButton_Click;
			githubButton.DataContext = new AddonButton {buttonType = buttonType, addon = addon, list = list};

			return githubButton;
		}

		private Button AddSpinner(string name, ImageSource image, int x, int y)
		{
			//we're gonna take the template from the discord button
			FieldInfo discHolder = mainWindow.GetType()
				.GetField("discordButton", BindingFlags.Instance | BindingFlags.Public);
			Button disc = (Button) discHolder.GetValue(mainWindow);

			Button githubButton = new Button
			{
				// Cursor = System.Windows.Input.Cursors.Hand,
				RenderTransformOrigin = new Point(-0.058, 0.493),
				BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
				Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
				Template = disc.Template,
				HorizontalAlignment = HorizontalAlignment.Left,
				Width = 21,
				Height = 21,
				VerticalAlignment = VerticalAlignment.Bottom
			};


			// image.Spin = true;

			Canvas.SetTop(githubButton, x);
			Canvas.SetLeft(githubButton, y);
			ImageBrush brush = new ImageBrush
			{
				ImageSource = image
			};
			githubButton.Background = brush;

			return githubButton;
		}


		private void UpdateCanvasSize(Canvas _canvas, List<AddonDisplayObject> _addonDisplayList)
		{
			Canvas canvas = _canvas;

			UpdateCurrentCanvasSize(canvas);
		}

		private void UpdateCurrentCanvasSize()
		{
			FieldInfo gridHolder =
				mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
			if (gridHolder == null) return;
			ScrollViewer view = (ScrollViewer) gridHolder.GetValue(mainWindow);

			Canvas canvas = null;

			view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { canvas = (Canvas) view.Content; }));

			if (!isSearching)
				UpdateCurrentCanvasSize(canvas);
			else
				UpdateCurrentCanvasSizeSearch(canvas);
		}

		private void UpdateCurrentCanvasSize(Canvas canvas)
		{
			Debug.WriteLine("UpdateCurrentCanvasSize " + _currentCount);
			canvas.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				if (_currentCount > 0)
					canvas.Height = 30 + ((displayMode == 0 && addonDisplayList.Count == _currentCount) ? 0 : 10) + (_currentCount) * (displayMode==0 ? 120: 50);
				else
					canvas.Height = 30;
			}));
		}

		private void UpdateCurrentCanvasSizeSearch(Canvas canvas)
		{
			Debug.WriteLine("UpdateCurrentCanvasSizeSearch " + _currentCount + " - " + addonDisplayList.Count);
			canvas.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				if (addonDisplayList.Count > 0)
					canvas.Height = 30 + (addonDisplayList.Count) * (displayMode == 0 ? 120 : 50);
				else
					canvas.Height = 30;
			}));
		}

		private async void PopulateTab(string data, string server)
		{
			FieldInfo gridHolder =
				mainWindow.GetType().GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
			if (gridHolder == null) return;
			ScrollViewer view = (ScrollViewer) gridHolder.GetValue(mainWindow);

			Canvas addonCanvas = new Canvas();
			view.Content = addonCanvas;

			Canvas canvas = (Canvas) view.Content;


			UpdateTabElements();


			try
			{
				int res = await Task.Run(() => PopulateRepo(data, canvas, server));
			}
			catch (Exception)
			{
			}

			finishedLoadingNum++;

			if (finishedLoadingNum == addonSources.Count)
			{

				Debug.WriteLine("Finished Loading Repos!");

				canvas.Dispatcher.Invoke(DispatcherPriority.Normal,
					new Action(() => { canvas.Height = 50 + addonDisplayList.Count * 120; }));

				isFinishedLoading = true;
				await Task.Run(() => installedAddons.Populate());
			}
		}

		private void Tab_DownloadComplete(object sender, DownloadStringCompletedEventArgs e)
		{
			Debug.WriteLine(e.Error);

			if (!e.Cancelled && e.Error == null)
			{
				string server = ((WebClient) sender).QueryString["server"];
				PopulateTab(e.Result, server);
			}
			else
			{
				// isRepoDownload = false;
				string server = ((WebClient) sender).QueryString["server"];
				Settings.CauseError("There was an issue getting addon data for " + server + "!");
			}
		}

		private void Tab_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
		}

		private Size MeasureString(Label label)
		{
			//Run run = (Run)label.Content;
			FormattedText formattedText = new FormattedText(
				(string) label.Content,
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
				label.Content = ((string) label.Content).Remove(((string) label.Content).Length - (maxWidth + 1));
				isTruncated = true;
			}

			if (isTruncated)
			{
				string desc = (string) label.Content;
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
				FieldInfo gridHolder = mainWindow.GetType()
					.GetField("AddonCanvas", BindingFlags.Instance | BindingFlags.Public);
				if (gridHolder == null) return;

				ScrollViewer view = (ScrollViewer) gridHolder.GetValue(mainWindow);

				Canvas _canvas = null;
				view.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
				{
					//view.Content = new Canvas();
					_canvas = (Canvas) view.Content;
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
				PopulateAddon(addonDisplay, 0, y * 120, true, _canvas);
		}
#pragma warning disable IDE1006 // Naming Styles
		private void emptyDel()
		{
		}
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