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
using Markdig;

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
			MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
			string result = Markdown.ToHtml(text, pipeline);

			readmeBlock.NavigateToString("<body style=\"background - color:black; color:white;\">"+result+"</body>");

		}

		private void ReadmeBlock_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{

		}
	}
}
