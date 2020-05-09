using System.Windows;

namespace AddonManager
{
	public delegate void ErrorButtonCallback();

	/// <summary>
	/// Interaction logic for errorMessage.xaml
	/// </summary>
	public partial class errorMessage
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
			Close();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}