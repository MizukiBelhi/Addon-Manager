using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
//using Markdig;
using MarkdownSharp;

namespace AddonManager
{
	/// <summary>
	/// Interaction logic for readmeWindow.xaml
	/// </summary>
	public partial class readmeWindow : Window
	{
		public readmeWindow()
		{
			InitializeComponent();

		}

		public void DisplayReadme(string text)
		{
			Markdown mk = new Markdown();
			string result = mk.Transform(text);

			readmeBlock.NavigateToString("<!DOCTYPE html><html><head><meta charset = \"UTF-8\"></head><body bgcolor=\"#2B2B2B\" style=\"color:white;\">" + result+ "</body></html>");

		}

		private void ReadmeBlock_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{
		}
	}
}
