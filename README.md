# Tree of Savior Addon Manager

Tree of Savior Addon Manager is an application that allows you to easily find and downloads addons and keep them up to date. This does all of the work for you so you can simply worry about playing the game.

# Download / Install

If you have previous addons installed from before using this, it's best to delete them all and start from scratch using this app. This includes the `addons` folder and all of the previous ipfs.

Grab the [latest release](https://github.com/MizukiBelhi/Addon-Manager/releases/latest), extract it, and run `AddonManager.exe`.

# FAQ

* Why does this exist?

The previous [Addon Manager](https://github.com/JTosAddon/Tree-of-Savior-Addon-Manager) runs on Electron, which is rather slow.
To preserve sanity of users we decided to bring the manager to C# and rebuild it from the ground up.


# Submitting Addons

## IToS

Make a pull request to [JToSAddon/Addons/tree/itos](https://github.com/JTosAddon/Addons/tree/itos)  in order to update `managers.json` to point to your addon repository. Example:

```json
{
	"sources" : [
		{
			"repo" : "Excrulon/Test-Addon"
		},
		{
			"repo" : "TehSeph/tos-addons"
		},
		{
			"repo" : "MizukiBelhi/ExtendedUI"
		},
		{
			"repo" : "Miei/TOS-lua"
		}
	]
}
```

Then, in your own repository where your addon lives, create an `addons.json` file that describes your packages.

```json
[
	{
		"tosversion" : "20171227",
		"name" : "Experience Viewer",
		"file" : "experienceviewer",
		"extension" : "ipf",
		"fileVersion" : "v1.0.0",
		"releaseTag" : "v1.0.0",
		"unicode" : "⛄",
		"description" : "Displays various experience values such as current experience, required experience, current percent, experience gained on last kill, kills til next level, experience per hour, and estimated time until level up.",
		"tags" : [
			"experience",
			"ui"
		]
	},
	{
		"tosversion" : "20171227",
		"name" : "Map Fog Viewer",
		"file" : "mapfogviewer",
		"extension" : "ipf",
		"fileVersion" : "v1.0.0",
		"releaseTag" : "v1.0.0",
		"unicode" : "⛄",
		"description" : "Displays the fog on the map as red tiles instead of the hard to see default fog. Makes exploration really easy!",
		"tags" : [
			"map",
			"minimap",
			"fog",
			"exploration"
		]
	}
]
```

`tosversion`: Date when you release your addon. Format: yearmonthday. Must be the same as the one in [broken-addons.json](https://github.com/MizukiBelhi/Addons/blob/master/broken-addons.json) or newer. Can be left out when you don't want date check to occur.

`name`: The name of your addon. This can be anything you want.

`releaseTag`: The tag name of your release.

`fileVersion`: The version of your addon. All `fileVersion`s need to follow [semantic versions](http://semver.org/) in order for updates to be processed properly.

`file`: The filename of your addon in the release, minus the extension. This should never change once submitted.

`extension`: The extension of your addon in the release. For now, only `ipf` is supported.

`unicode`: The unicode character you want to use in your downloaded addon filename.

`description`: A detailed description of your addon.

`tags`: A list of keywords that describes what your addon is for searching.

## JToS

Make a pull request to [JToSAddon/Addons](https://github.com/JToSAddon/Addons) in order to update `managers.json` to point to your addon repository.