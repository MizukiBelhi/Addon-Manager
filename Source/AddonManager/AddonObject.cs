using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Semver;

namespace AddonManager
{
	public class AddonObject
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

		public bool IsQueued = false;
		public SemVersion semversion = null;

		public void SetDefaults()
		{
			this.isNewest = true;
			this.isInstalled = true;
			this.isUnknown = true;
			this.isUnknownInstalled = true;
			this.isOutdated = false;
			this.addon.name = this.addon.file;
			this.addon.description = "";
			this.repo = "unknown/unknown";
			this.IsQueued = false;
		}

		public bool HasError()
		{
			return this.isReported || this.isOutdated || this.isUnknown;
		}

		public void InitSemver()
		{
			semversion = SemVersion.Parse(this.addon.fileVersion.Remove(0, 1));
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
			return (!(a > b));
		}
		public static bool operator >=(AddonObject a, AddonObject b)
		{
			return (!(a < b));
		}
		public static bool operator ==(AddonObject a, AddonObject b)
		{
			if (object.ReferenceEquals(a, null))
				return object.ReferenceEquals(b, null);
			if (object.ReferenceEquals(b, null))
				return object.ReferenceEquals(a, null);

			return a.IsSameAs(b);
		}
		public static bool operator !=(AddonObject a, AddonObject b)
		{
			if (object.ReferenceEquals(a, null))
				return !object.ReferenceEquals(b, null);
			if (object.ReferenceEquals(b, null))
				return !object.ReferenceEquals(a, null);

			return !a.IsSameAs(b);
		}
	}

}
