using System;
using System.Collections.Generic;
using System.Linq;
using Semver;

namespace AddonManager
{
	internal static class BrokenAddons
	{
		public static List<BrokenAddon> brokenAddons = new List<BrokenAddon>();
		public static string ToSVersion = "";

		public static void Load()
		{
			string broken = DownloadManager.GetBrokenAddons();
			BrokenObject brokenObj = JsonManager.LoadString<BrokenObject>(broken);

			brokenAddons = brokenObj.addons;
			ToSVersion = brokenObj.tosversion;
		}

		public static bool IsBrokenAddon(AddonObject obj)
		{
			return (from brk in brokenAddons
				let semver = SemVersion.Parse(brk.version)
				where obj.addon.file == brk.file &&
				      string.Equals(obj.repo.Split('/')[0], brk.author, StringComparison.CurrentCultureIgnoreCase) &&
				      obj.semversion.CompareTo(semver) == 0
				select brk).Any();
		}

		public static bool IsOutdatedAddon(AddonObject obj)
		{
			if (!string.IsNullOrEmpty(obj.addon.tosversion))
				return int.Parse(ToSVersion) > int.Parse(obj.addon.tosversion);
			
			return false;
		}
	}

	public class BrokenAddon
	{
		public string file { get; set; }
		public string version { get; set; }
		public string author { get; set; }
	}

	public class BrokenObject
	{
		public string tosversion { get; set; }
		public List<BrokenAddon> addons { get; set; }
	}
}