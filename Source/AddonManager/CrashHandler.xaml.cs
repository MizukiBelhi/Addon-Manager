using	System;
using	System.Collections.Generic;
using	System.Linq;
using	System.Text;
using	System.Threading.Tasks;
using	System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AddonManager
{
	///	<summary>
	///	Interaction logic for CrashHandler.xaml
	///	</summary>
	public partial class CrashHandler : Window
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
