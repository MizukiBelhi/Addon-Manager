using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			return this.isReported || this.isOutdated || this.isReported || this.isUnknown;
		}
	}

}
