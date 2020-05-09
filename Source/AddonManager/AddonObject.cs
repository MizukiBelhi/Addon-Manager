using System.Collections.Generic;
using Semver;

namespace AddonManager
{
#pragma warning disable 660,661
	public class AddonObject
#pragma warning restore 660,661
	{
		public int Id { get; set; }

#pragma warning disable IDE1006 // Naming Styles
		public string repo { get; set; }
		public AddonsObject addon { get; set; }
		public bool isInstalled { get; set; }
		public bool hasUpdate { get; set; }
		public bool isOutdated { get; set; }
		public bool isReported { get; set; }
		public bool isNewest { get; set; }
		public bool isUnknown { get; set; }
		public bool isUnknownInstalled { get; set; }
		public bool isDownloading { get; set; }
		public List<ManagersDependency> dependencies { get; set; }
#pragma warning restore IDE1006 // Naming Styles

		public bool IsQueued;
		public SemVersion semversion;
		public long date = 0;
		public string readdate = "";

		public void SetDefaults()
		{
			isNewest = true;
			isInstalled = true;
			isUnknown = true;
			isUnknownInstalled = true;
			isOutdated = false;
			addon.name = addon.file;
			addon.description = "";
			repo = "unknown/unknown";
			IsQueued = false;
		}

		public bool HasError()
		{
			return isReported || isOutdated || isUnknown;
		}

		public void SetupBroken()
		{
			if (semversion == null)
				InitSemver();

			isReported = BrokenAddons.IsBrokenAddon(this);
			isOutdated = BrokenAddons.IsOutdatedAddon(this);
		}

		public void InitSemver()
		{
			semversion = SemVersion.Parse(addon.fileVersion.Remove(0, 1));
		}

		public bool IsNewerThan(AddonObject other)
		{
			if (semversion == null)
				InitSemver();
			if (other.semversion == null)
				other.InitSemver();

			if (semversion.CompareTo(other.semversion) < 1)
				return false;

			return true;
		}

		public bool IsOlderThan(AddonObject other)
		{
			if (semversion == null)
				InitSemver();
			if (other.semversion == null)
				other.InitSemver();

			if (semversion.CompareTo(other.semversion) > -1)
				return false;

			return true;
		}

		public bool IsSameAs(AddonObject other)
		{
			if (semversion == null)
				InitSemver();
			if (other.semversion == null)
				other.InitSemver();

			if (semversion.CompareTo(other.semversion) != 0)
				return false;

			return true;
		}

		public static bool operator <(AddonObject a, AddonObject b)
		{
			return a.IsOlderThan(b);
		}

		public static bool operator >(AddonObject a, AddonObject b)
		{
			return a.IsNewerThan(b);
		}

		public static bool operator <=(AddonObject a, AddonObject b)
		{
			return !(a > b);
		}

		public static bool operator >=(AddonObject a, AddonObject b)
		{
			return !(a < b);
		}

		public static bool operator ==(AddonObject a, AddonObject b)
		{
			if (ReferenceEquals(a, null))
				return ReferenceEquals(b, null);
			return !ReferenceEquals(b, null) && a.IsSameAs(b);
		}

		public static bool operator !=(AddonObject a, AddonObject b)
		{
			if (ReferenceEquals(a, null))
				return !ReferenceEquals(b, null);
			if (ReferenceEquals(b, null))
				return true;

			return !a.IsSameAs(b);
		}
	}
}