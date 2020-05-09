using System.Windows.Controls;

namespace AddonManager
{
	///	<summary>
	///	Interaction logic for CrashHandler.xaml
	///	</summary>
	public partial class CrashHandler
	{
		public CrashHandler()
		{
			InitializeComponent();
		}

		private string trace;

		public void Crash(string message, string stack)
		{
			trace = message + "\r\r" + stack;
			StackTrace.Text = trace;
		}

		private void StackTrace_TextChanged(object sender, TextChangedEventArgs e)
		{
			StackTrace.Text = trace;
		}
	}
}