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

namespace AddonManager
{
	public delegate void ErrorButtonCallback();
	/// <summary>
	/// Interaction logic for errorMessage.xaml
	/// </summary>
	public partial class errorMessage : Window
	{
		private ErrorButtonCallback cb;

		public errorMessage()
		{
			InitializeComponent();

			errorbutton.Visibility = Visibility.Hidden;
		}

		public void Error(string message)
		{
			errorlabel.Content = message;
		}

		public void ShowButton(string buttonText, ErrorButtonCallback callback)
		{
			errorbutton.Visibility = Visibility.Visible;
			errorbutton.Content = buttonText;
			cb = callback;
		}

		public void ChangeFirstButton(string buttonText)
		{
			okButton.Content = buttonText;
		}

		public void ChangeTitle(string titleText)
		{
			titleLabel.Content = titleText;
		}

		private void Errorbutton_Click(object sender, RoutedEventArgs e)
		{
			cb();
			this.Close();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
