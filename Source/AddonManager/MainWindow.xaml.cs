using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Application = System.Windows.Forms.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TabControl = System.Windows.Controls.TabControl;
using Timer = System.Timers.Timer;

namespace AddonManager
{
	public partial class MainWindow
	{
		public static Settings settings = new Settings();

		private bool hasValidToSFolder;

		public static TabControl ToSTabs;

		//private static List<TabManager> tabManagers = new List<TabManager>();
		public static TabManager tabManager;
		public static ManagerDebug debugManager;

		public MainWindow()
		{
			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
			Application.ThreadException += GlobalThreadExceptionHandler;
			debugManager = new ManagerDebug();
			Debug.Listeners.Add(debugManager);

			InitializeComponent();

			//Makes text look nicer
			SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
			SetValue(TextOptions.TextRenderingModeProperty, TextRenderingMode.ClearType);


			ToSTabs = tosTabs;

			settings.LoadSettings();

			AddonManager.Language.CurrentLanguage = !string.IsNullOrEmpty(settings.Language) ? settings.Language : "en";
			AddonManager.Language.Init();

			var languages = AddonManager.Language.GetAvailable();
			if (languages.Length > 0)
				foreach (string lang in languages)
				{
					LanguageSelect.Items.Add(lang.ToUpper());
					if (lang.ToLower() == AddonManager.Language.CurrentLanguage.ToLower())
						LanguageSelect.SelectedItem = lang.ToUpper();
				}


			Dispatcher.BeginInvoke((Action) (() => tosTabs.SelectedIndex = 1));
			//LoadingCanvas.Visibility = Visibility.Hidden;
			SetLabels();

//If it's not a debug build we remove the debug tab
#if !DEBUG
			debugTab.Visibility = Visibility.Hidden;
#endif
			developerTab.Visibility = Visibility.Hidden;
		}

		public TabManager GetTabManager()
		{
			return tabManager;
		}

		public ManagerDebug GetDebugManager()
		{
			return debugManager;
		}

		private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = (Exception) e.ExceptionObject;

			CrashHandler handler = new CrashHandler();
			handler.Crash(ex.Message, ex.StackTrace);
			handler.ShowDialog();
		}

		private static void GlobalThreadExceptionHandler(object sender, ThreadExceptionEventArgs e)
		{
			Exception ex = e.Exception;

			CrashHandler handler = new CrashHandler();
			handler.Crash(ex.Message, ex.StackTrace);
			handler.ShowDialog();
		}

		private bool addTabsOnce;

		private void AddTabManagers()
		{
			if (addTabsOnce)
				return;


			string itosSource = "https://raw.githubusercontent.com/JTosAddon/Addons/itos/managers.json";
			string jtosSource = "https://raw.githubusercontent.com/JTosAddon/Addons/master/managers.json";

			tabManager = new TabManager(new Dictionary<string, string> {{"IToS", itosSource}, {"JToS", jtosSource}});

			addTabsOnce = true;
		}


		private void SetLabels()
		{
			string ver = settings.Version != 0 && settings.Version > 99 ? settings.Version.ToString() : "ERR";
			ver = "v" + ver[0] + "." + ver[1] + "." + ver[2];
			Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			VersionLabel.Content = $"{ver}.{version.Build}.{version.Revision}";

			dateCheck.IsChecked = settings.LoadDates;


			configure.Text = AddonManager.Language.Translate("SETTINGS.CONFIGURE_FOLDER");
			BrowseButton.Content = AddonManager.Language.Translate("SETTINGS.OPEN_FOLDER_BROWSE_DIALOG");

			SettingsTab.Header = AddonManager.Language.Translate("TAB.SETTINGS");
			AddonTab.Header = AddonManager.Language.Translate("TAB.BROWSE");
			InstalledAddonTab.Header = AddonManager.Language.Translate("TAB.INSTALLED");


			newVersion.Visibility = Visibility.Hidden;

			ToSClientText.Text = settings.TosFolder;
			ToSFolderCheck();

			RebuildAddonTextList();

			AddonSearchBar.Text = "Search...";
			AddonSearchBar.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));

			groupCheck.IsChecked = settings.IsGrouped;
		}

		private readonly Stopwatch _timer = new Stopwatch();

		/*private void _ToSFolderCheck()
		{
			ToSError.Content = "";
			hasValidToSFolder = true;
			settings.GenerateTosFolders();
			AddTabManagers();
			_timer.Start();

			LoadingCanvas.Visibility = Visibility.Visible;
			LoadingLabel.Content = "Collecting Addon Data";

			LoadingFunLabel.Content = funLabels[new Random(Guid.NewGuid().GetHashCode()).Next(0, funLabels.Count)];

			Timer aTimer = new Timer();
			aTimer.Elapsed += (sender, e) => OnTimedEvent(sender, e, this);
			aTimer.Interval = 2000;
			aTimer.Enabled = true;

			WaitForManagers();
		}*/

		private void ToSFolderCheck()
		{
			bool folderError = false;

			Debug.WriteLine("Checking Folder: " + settings.TosFolder);

			if (!JsonManager.DirectoryExists(settings.TosFolder))
			{
				Debug.WriteLine("Folder doesn't exist");
				folderError = true;
			}

			if (!JsonManager.DirectoryExists(settings.TosFolder + @"\release"))
			{
				Debug.WriteLine("Folder release doesn't exist");
				folderError = true;
			}

			if (!JsonManager.FileExists(settings.TosFolder + @"\release\Client_tos.exe"))
			{
				Debug.WriteLine("Client.exe doesn't exist");
				folderError = true;
			}

			if (!JsonManager.DirectoryExists(settings.TosFolder + @"\data"))
			{
				Debug.WriteLine("Folder data doesn't exist");
				folderError = true;
			}

			if (folderError)
			{
				ToSError.Content = new Run(AddonManager.Language.Translate("SETTINGS.INVALID_FOLDER"))
					{FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0))};
				hasValidToSFolder = false;
			}
			else
			{
				ToSError.Content = "";
				hasValidToSFolder = true;
				settings.GenerateTosFolders();
				AddTabManagers();
				_timer.Start();

				//if (settings.FirstStart)
				{
					LoadingCanvas.Visibility = Visibility.Visible;
					LoadingLabel.Content = "Collecting Addon Data";

					LoadingFunLabel.Content =
						funLabels[new Random(Guid.NewGuid().GetHashCode()).Next(0, funLabels.Count)];

					Timer aTimer = new Timer();
					aTimer.Elapsed += (sender, e) => OnTimedEvent(sender, e, this);
					aTimer.Interval = 5000;
					aTimer.Enabled = true;

					WaitForManagers();
				}
			}
		}

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(e.Uri.ToString());
		}

		private int sortMode = 1;
		private string lastSort = "";

		private void DoSort(string sortBy, bool ovrride=false)
		{

			if (lastSort != sortBy)
				sortMode = 1;
			if (ovrride)
				sortMode = 1;

			lastSort = sortBy;
			sortMode = sortMode == 0 ? 1 : 0;
			tabManager.SortList(sortBy, sortMode);

			if (sortMode == 0)
			{
				SearchNameArrow.Source = new BitmapImage(new Uri("pack://application:,,,/UI/downarrow.png"));
				SearchDateArrow.Source = new BitmapImage(new Uri("pack://application:,,,/UI/downarrow.png"));
				SearchDevArrow.Source = new BitmapImage(new Uri("pack://application:,,,/UI/downarrow.png"));
			}
			else
			{
				SearchNameArrow.Source = new BitmapImage(new Uri("pack://application:,,,/UI/uparrow.png"));
				SearchDateArrow.Source = new BitmapImage(new Uri("pack://application:,,,/UI/uparrow.png"));
				SearchDevArrow.Source = new BitmapImage(new Uri("pack://application:,,,/UI/uparrow.png"));
			}

			switch (sortBy)
			{
				case "Name":
					SearchNameArrow.Visibility = Visibility.Visible;
					SearchDateArrow.Visibility = Visibility.Hidden;
					SearchDevArrow.Visibility = Visibility.Hidden;
					break;
				case "Date":
					SearchNameArrow.Visibility = Visibility.Hidden;
					SearchDateArrow.Visibility = Visibility.Visible;
					SearchDevArrow.Visibility = Visibility.Hidden;
					break;
				case "Dev":
					SearchNameArrow.Visibility = Visibility.Hidden;
					SearchDateArrow.Visibility = Visibility.Hidden;
					SearchDevArrow.Visibility = Visibility.Visible;
					break;
			}
		}

		private void RequestSort(object sender, RequestNavigateEventArgs e)
		{
			string sortBy = e.Uri.ToString();
			DoSort(sortBy);
		}

		private readonly List<string> funLabels = new List<string>
		{
			"Popolions rule!", "Transfering TP...", "Paying $50...", "Finding Talt...", "Bugging ersakoz...",
			"Threatening Tchuu...", "Finding more work for Elec...", "RIP Xanaxiel...",
			"Calling IMC to give us less FPS...", "Looking for Cmdr.LoadFail...", "Collecting Tanu Leaf...",
			"Praying for the Miko quest...", "Waiting for Coursing to run out...", "Declaring War...",
			"Using Black Card Album...", "Catching Golden Fish...", "Waiting in queue(2 out of 5)...",
			"Never gonna fix you up...", "Classic Inventory when?", "Extracting Skill Gems..."
		};

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (settings.TosFolder != ToSClientText.Text)
			{
				settings.TosFolder = ToSClientText.Text;
				settings.Save();
				ToSFolderCheck();
			}
		}

		private void LanguageSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			dynamic selectedItem = LanguageSelect.SelectedItem;
			string selectedLang = selectedItem.ToLower();

			if (AddonManager.Language.CurrentLanguage != selectedLang)
			{
				AddonManager.Language.CurrentLanguage = selectedLang;

				SetLabels();

				settings.Language = selectedLang.ToLower();
				settings.Save();
			}
		}


		private void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog dialog = new FolderBrowserDialog
			{
				ShowNewFolderButton = false,
				RootFolder = Environment.SpecialFolder.Desktop,
				SelectedPath = settings.TosFolder,
				Description = "Select the ToS directory."
			};

			DialogResult res = dialog.ShowDialog();


			switch (res)
			{
				case System.Windows.Forms.DialogResult.OK:
					ToSClientText.Text = dialog.SelectedPath;
					break;
				case System.Windows.Forms.DialogResult.Cancel:
					return;
			}
		}

		private int oldSelectedTab = 1;

		private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			dynamic selectedItem = tosTabs.SelectedItem;
			//string selectedTab = selectedItem.Header;

			if (tosTabs.SelectedIndex == oldSelectedTab) return;
			if (!hasValidToSFolder)
			{
				//Need to do this because even Microsoft isn't sure when SelectedIndex is set
				Dispatcher.BeginInvoke((Action) (() => tosTabs.SelectedIndex = oldSelectedTab));

				return;
			}

			oldSelectedTab = tosTabs.SelectedIndex;
		}

		private void DiscordButton_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://discord.gg/hgxRFwy");
		}

		private async void WaitForManagers()
		{
			//if (settings.FirstStart)
			{
				settings.FirstStart = false;
				await Task.Run(() => WaitForManagersTask());
			}

			//settings.FirstStart = false;
			//Database.Execute();
		}

		private static void OnTimedEvent(object source, ElapsedEventArgs e, MainWindow win)
		{
			win.LoadingFunLabel.Dispatcher.Invoke(DispatcherPriority.Normal,
				new Action(() =>
				{
					win.LoadingFunLabel.Content =
						win.funLabels[new Random(Guid.NewGuid().GetHashCode()).Next(0, win.funLabels.Count)];
				}));
		}

		public void WaitForManagersTask()
		{
			int fin = 0;
			while (fin < 1)
			{
				Task.Delay(10);
				fin = 0;

				if (tabManager != null)
					if (tabManager.isFinishedLoading)
						fin++;
			}


			//TimeSpan ts = TimeSpan.FromMilliseconds(500000);
			//if (t.Wait(ts))
			{
				LoadingCanvas.Dispatcher.Invoke(DispatcherPriority.Normal,
					new Action(() => { LoadingCanvas.Visibility = Visibility.Hidden; }));


				_timer.Stop();
				Debug.WriteLine(_timer.Elapsed);


				//DoSort("Name");
				tabManager?.DisplayAddons();
				


				if (settings.HasNewVersion())
					Dispatcher.BeginInvoke((Action) (() =>
							Settings.CauseError("A new version is available", "Update Available", "Update Later",
								"Update Now", () =>
								{
									Process.Start("AddonManagerUpdater.exe",
										"-version \"" + settings.GetVersion() + "\"");
									Environment.Exit(0);
								})
						));
			}
		}

		public void UpdateLoadingBarMax(int maxBar)
		{
			LoadingDataBar.Dispatcher.Invoke(DispatcherPriority.Normal,
				new Action(() => { LoadingDataBar.Maximum = LoadingDataBar.Maximum + maxBar; }));
		}

		public void UpdateLoadingBarCurrent(int current)
		{
			LoadingDataBar.Dispatcher.Invoke(DispatcherPriority.Normal,
				new Action(() => { LoadingDataBar.Value = LoadingDataBar.Value + current; }));
		}

		public void RebuildAddonTextList()
		{
			tabManager?.UpdateTabElements();
		}


		private void IToSSearchBar_TextChanged(object sender, TextChangedEventArgs e)
		{
		}

		private void IToSSearchButton_Click(object sender, RoutedEventArgs e)
		{
			//itos = 0
			tabManager.SearchListAsync(AddonSearchBar.Text);
		}

		private void IToSSearchBar_KeyDown(object sender, KeyEventArgs e)
		{
			//check for enter key
			if (e.Key == Key.Enter) IToSSearchButton_Click(this, new RoutedEventArgs());
		}


		private bool searchBarWaterMark;

		private void AddonSearchBar_GotFocus(object sender, RoutedEventArgs e)
		{
			if (!searchBarWaterMark && AddonSearchBar.Text == "Search...")
			{
				searchBarWaterMark = !searchBarWaterMark;
				AddonSearchBar.Text = "";
				AddonSearchBar.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
			}
		}

		private void AddonSearchBar_LostFocus(object sender, RoutedEventArgs e)
		{
			if (searchBarWaterMark && string.IsNullOrEmpty(AddonSearchBar.Text))
			{
				searchBarWaterMark = !searchBarWaterMark;
				AddonSearchBar.Text = "Search...";
				AddonSearchBar.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
			}
		}

		private bool firstScrollEvent;
		private void AddonCanvas_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (e.Source != null && e.Source.ToString() == "AddonManager.AddonControl")
				return;

			if (!(e.VerticalChange > 0)) return;
			if (e.VerticalOffset + e.ViewportHeight != e.ExtentHeight) return;

			tabManager.LoadNextPage(!firstScrollEvent);
			firstScrollEvent = true;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			throw new Exception("Oh no, this is totally a crash caused by something.");
		}

		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (!tabManager.isFinishedLoading)
			{
				groupCheck.IsChecked = !groupCheck.IsChecked;
				return;
			}

			if (groupCheck.IsChecked != null) settings.IsGrouped = groupCheck.IsChecked.Value;

			settings.Save();
		}

		private void DateBox_Checked(object sender, RoutedEventArgs e)
		{
			if (!tabManager.isFinishedLoading)
			{
				dateCheck.IsChecked = !dateCheck.IsChecked;
				return;
			}

			if (dateCheck.IsChecked != null) settings.LoadDates = dateCheck.IsChecked.Value;

			settings.Save();
		}

		private void DevCheck_Checked(object sender, RoutedEventArgs e)
		{
			developerTab.Visibility =
				developerTab.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
		}


		private void DevClearButton_Click(object sender, RoutedEventArgs e)
		{
			DeveloperTools.ClearAddons();
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();

			DialogResult res = dialog.ShowDialog();


			switch (res)
			{
				case System.Windows.Forms.DialogResult.OK:
					DeveloperTools.LoadAddon(dialog.FileName);
					break;
				case System.Windows.Forms.DialogResult.Cancel:
					return;
			}
		}

		private void FullModeButton_OnClickButton_Click(object sender, RoutedEventArgs e)
		{
			DoSort("Name", true);
			tabManager.SwapDisplayMode(0);
		}

		private void CompactModeButton_OnClickButton_Click(object sender, RoutedEventArgs e)
		{
			DoSort("Name", true);
			tabManager.SwapDisplayMode(1);
		}
	}

	public class ScrollViewerEx : ScrollViewer
	{
		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			if (Parent is UIElement parentElement)
			{
				if ((e.Delta > 0 && VerticalOffset == 0) ||
				    (e.Delta < 0 && VerticalOffset == ScrollableHeight))
				{
					e.Handled = true;

					MouseWheelEventArgs routedArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
					{
						RoutedEvent = MouseWheelEvent
					};
					parentElement.RaiseEvent(routedArgs);
				}
			}

			base.OnMouseWheel(e);
		}
	}
}