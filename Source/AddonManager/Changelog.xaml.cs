using MarkdownSharp;

namespace AddonManager
{
	/// <summary>
	/// Interaction logic for Changelog.xaml
	/// </summary>

	public partial class Changelog
	{
		public Changelog()
		{
			InitializeComponent();
		}

		private string changelogText = @"# Changelog

### v1.0.5
	Fixes:
	* Not updating addon data until the cache was cleared by windows.
	* When searching, the scrollbar no longer freaks out.
	* Addons are now properly removed from the search when installing.
	* If the list doesn't fill the entire page, addons are no longer stuck centered.

	Additions:
	* Broken Addons Support
	* Icon for Installed Addons Button.
	* Developer Tools.
	* More sort options.
	* Changelog is now displayed after installing a new version.

	Changes:
	* You can now scroll like expected through the list.
	* Language files were added as resource and are no longer required.
	* General Code Cleanup.

	Known Issues:
	* Under certain circumstances a phantom of a installed addon may remain in the list.

### v1.0.4
	Fixes:
	* Info window not properly rendering text and it now looks for more specific readme files.
	* Addons not getting properly removed from the main list (woops!)

	Additions:
	* Added Updater.

### v1.0.3
	Fixes:
	* Manager not deleting file when trying to install another version of an already installed addon.
	* Manager deleting addon folder when updating addon.
	* Search having duplicate entries when using certain search terms.

	Additions:
	* Added even more Error Messages.

	Changes:
	* Installed Addon list is no longer being fully refreshed when updating/downloading.
	* Changed how files are downloaded.

### v1.0.2
	Fixes:
	* Updating Addons because it wasn't getting the newest version correctly. (Thanks to Tchuu)

	Additions:
	* Added more Error Window popups.
	* Info button functionality.

### v1.0.1
	Fixes:
	* WebClient stopping to work when installing multiple addons. (Thanks to Tchuu)

	Additions:
	* Added Error window when trying to delete an addon when ToS is running.

### v1.0.0a
	* Version Check.
	* Win7 Support.";

		public void DisplayChangelog()
		{
			Markdown mk = new Markdown();
			string result = mk.Transform(changelogText);

			changelogBrowser.NavigateToString(
				"<!DOCTYPE html><html><head><meta charset = \"UTF*8\"></head><body bgcolor=\"#2B2B2B\" style=\"color:white;\">" +
				result + "</body></html>");
		}

	}
}
