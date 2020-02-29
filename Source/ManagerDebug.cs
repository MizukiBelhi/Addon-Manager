using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Windows.Threading;

namespace AddonManager
{
	public class ManagerDebug : TraceListener
	{
		MainWindow mainWindow;

		public ManagerDebug()
		{
			mainWindow = (MainWindow)Application.Current.MainWindow;
		}

		public override void Write(string message)
		{

			//FieldInfo gridHolder = mainWindow.GetType().GetField("debugViewer", BindingFlags.Instance | BindingFlags.Public);
			//ScrollViewer viewer = (ScrollViewer)gridHolder.GetValue(mainWindow);
			
			FieldInfo logHolder = mainWindow.GetType().GetField("debugLog", BindingFlags.Instance | BindingFlags.Public);
			TextBlock log = (TextBlock)logHolder.GetValue(mainWindow);

			string currentTime = DateTime.Now.ToShortTimeString();


			log.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
			{
				log.Text += "["+currentTime+"] "+message;
			}));
			
		}

		public override void WriteLine(string message)
		{
			Write(message + "\r\n");
		}
	}

}
