using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

//These are all taken from MSDN for custom window style

namespace AddonManager
{
#pragma warning disable IDE0019 // Use pattern matching
	public class WindowCloseCommand : ICommand
	{

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public event EventHandler CanExecuteChanged;

		public void Execute(object parameter)
		{
			var window = parameter as Window;

			if (window != null)
			{
				window.Close();
			}
		}
	}

	public static class ControlDoubleClickBehavior
	{
		public static ICommand GetExecuteCommand(DependencyObject obj)
		{
			return (ICommand)obj.GetValue(ExecuteCommand);
		}

		public static void SetExecuteCommand(DependencyObject obj, ICommand command)
		{
			obj.SetValue(ExecuteCommand, command);
		}

		public static readonly DependencyProperty ExecuteCommand = DependencyProperty.RegisterAttached("ExecuteCommand",
			typeof(ICommand), typeof(ControlDoubleClickBehavior),
			new UIPropertyMetadata(null, OnExecuteCommandChanged));

		public static Window GetExecuteCommandParameter(DependencyObject obj)
		{
			return (Window)obj.GetValue(ExecuteCommandParameter);
		}

		public static void SetExecuteCommandParameter(DependencyObject obj, ICommand command)
		{
			obj.SetValue(ExecuteCommandParameter, command);
		}

		public static readonly DependencyProperty ExecuteCommandParameter = DependencyProperty.RegisterAttached("ExecuteCommandParameter",
			typeof(Window), typeof(ControlDoubleClickBehavior));

		private static void OnExecuteCommandChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var control = sender as Control;

			if (control != null)
			{
				control.MouseDoubleClick += Control_MouseDoubleClick;
			}
		}

		static void Control_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var control = sender as Control;

			if (control != null)
			{
				var command = control.GetValue(ExecuteCommand) as ICommand;
				var commandParameter = control.GetValue(ExecuteCommandParameter);

				if (command.CanExecute(e))
				{
					command.Execute(commandParameter);
				}
			}
		}
	}

	public static class SystemMenuManager
	{
		public static void ShowMenu(Window targetWindow, Point menuLocation)
		{
			if (targetWindow == null)
				throw new ArgumentNullException("TargetWindow is null.");

			int x, y;

			try
			{
				x = Convert.ToInt32(menuLocation.X);
				y = Convert.ToInt32(menuLocation.Y);
			}
			catch (OverflowException)
			{
				x = 0;
				y = 0;
			}

			uint WM_SYSCOMMAND = 0x112, TPM_LEFTALIGN = 0x0000, TPM_RETURNCMD = 0x0100;

			IntPtr window = new WindowInteropHelper(targetWindow).Handle;

			IntPtr wMenu = NativeMethods.GetSystemMenu(window, false);

			int command = NativeMethods.TrackPopupMenuEx(wMenu, TPM_LEFTALIGN | TPM_RETURNCMD, x, y, window, IntPtr.Zero);

			if (command == 0)
				return;

			NativeMethods.PostMessage(window, WM_SYSCOMMAND, new IntPtr(command), IntPtr.Zero);
		}
	}

	public static class ShowSystemMenuBehavior
	{
		#region TargetWindow 

		public static Window GetTargetWindow(DependencyObject obj)
		{
			return (Window)obj.GetValue(TargetWindow);
		}

		public static void SetTargetWindow(DependencyObject obj, Window window)
		{
			obj.SetValue(TargetWindow, window);
		}

		public static readonly DependencyProperty TargetWindow = DependencyProperty.RegisterAttached("TargetWindow", typeof(Window), typeof(ShowSystemMenuBehavior));

		#endregion

		#region LeftButtonShowAt 

		public static UIElement GetLeftButtonShowAt(DependencyObject obj)
		{
			return (UIElement)obj.GetValue(LeftButtonShowAt);
		}

		public static void SetLeftButtonShowAt(DependencyObject obj, UIElement element)
		{
			obj.SetValue(LeftButtonShowAt, element);
		}

		public static readonly DependencyProperty LeftButtonShowAt = DependencyProperty.RegisterAttached("LeftButtonShowAt",
			typeof(UIElement), typeof(ShowSystemMenuBehavior),
			new UIPropertyMetadata(null, LeftButtonShowAtChanged));

		#endregion

		#region RightButtonShow 

		public static bool GetRightButtonShow(DependencyObject obj)
		{
			return (bool)obj.GetValue(RightButtonShow);
		}

		public static void SetRightButtonShow(DependencyObject obj, bool arg)
		{
			obj.SetValue(RightButtonShow, arg);
		}

		public static readonly DependencyProperty RightButtonShow = DependencyProperty.RegisterAttached("RightButtonShow",
			typeof(bool), typeof(ShowSystemMenuBehavior),
			new UIPropertyMetadata(false, RightButtonShowChanged));

		#endregion

		#region LeftButtonShowAt 

		static void LeftButtonShowAtChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var element = sender as UIElement;

			if (element != null)
			{
				element.MouseLeftButtonDown += LeftButtonDownShow;
			}
		}

		static bool leftButtonToggle = true;

		static void LeftButtonDownShow(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (leftButtonToggle)
			{
				var element = ((UIElement)sender).GetValue(LeftButtonShowAt);

				var showMenuAt = ((Visual)element).PointToScreen(new Point(0, 0));

				var targetWindow = ((UIElement)sender).GetValue(TargetWindow) as Window;

				SystemMenuManager.ShowMenu(targetWindow, showMenuAt);

				leftButtonToggle = !leftButtonToggle;
			}
			else
			{
				leftButtonToggle = !leftButtonToggle;
			}
		}

		#endregion

		#region RightButtonShow handlers 

		private static void RightButtonShowChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var element = sender as UIElement;

			if (element != null)
			{
				element.MouseRightButtonDown += RightButtonDownShow;
			}
		}

		static void RightButtonDownShow(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var element = (UIElement)sender;

			var targetWindow = element.GetValue(TargetWindow) as Window;

			var showMenuAt = targetWindow.PointToScreen(Mouse.GetPosition((targetWindow)));

			SystemMenuManager.ShowMenu(targetWindow, showMenuAt);
		}

		#endregion
	}

	internal static class NativeMethods
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		[DllImport("user32.dll")]
		internal static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

		[DllImport("user32.dll")]
		internal static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		internal static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
	}

	public class WindowMinimizeCommand : ICommand
	{

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public event EventHandler CanExecuteChanged;

		public void Execute(object parameter)
		{
			var window = parameter as Window;

			if (window != null)
			{
				window.WindowState = WindowState.Minimized;
			}
		}
	}

	public static class WindowResizeBehavior
	{
		public static Window GetTopLeftResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(TopLeftResize);
		}

		public static void SetTopLeftResize(DependencyObject obj, Window window)
		{
			obj.SetValue(TopLeftResize, window);
		}

		public static readonly DependencyProperty TopLeftResize = DependencyProperty.RegisterAttached("TopLeftResize",
			typeof(Window), typeof(WindowResizeBehavior),
			new UIPropertyMetadata(null, OnTopLeftResizeChanged));

		private static void OnTopLeftResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var thumb = sender as Thumb;

			if (thumb != null)
			{
				thumb.DragDelta += DragTopLeft;
			}
		}

		public static Window GetTopRightResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(TopRightResize);
		}

		public static void SetTopRightResize(DependencyObject obj, Window window)
		{
			obj.SetValue(TopRightResize, window);
		}

		public static readonly DependencyProperty TopRightResize = DependencyProperty.RegisterAttached("TopRightResize",
			typeof(Window), typeof(WindowResizeBehavior),
			new UIPropertyMetadata(null, OnTopRightResizeChanged));

		private static void OnTopRightResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var thumb = sender as Thumb;

			if (thumb != null)
			{
				thumb.DragDelta += DragTopRight;
			}
		}

		public static Window GetBottomRightResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(BottomRightResize);
		}

		public static void SetBottomRightResize(DependencyObject obj, Window window)
		{
			obj.SetValue(BottomRightResize, window);
		}

		public static readonly DependencyProperty BottomRightResize = DependencyProperty.RegisterAttached("BottomRightResize",
			typeof(Window), typeof(WindowResizeBehavior),
			new UIPropertyMetadata(null, OnBottomRightResizeChanged));

		private static void OnBottomRightResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var thumb = sender as Thumb;

			if (thumb != null)
			{
				thumb.DragDelta += DragBottomRight;
			}
		}

		public static Window GetBottomLeftResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(BottomLeftResize);
		}

		public static void SetBottomLeftResize(DependencyObject obj, Window window)
		{
			obj.SetValue(BottomLeftResize, window);
		}

		public static readonly DependencyProperty BottomLeftResize = DependencyProperty.RegisterAttached("BottomLeftResize",
			typeof(Window), typeof(WindowResizeBehavior),
			new UIPropertyMetadata(null, OnBottomLeftResizeChanged));

		private static void OnBottomLeftResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var thumb = sender as Thumb;

			if (thumb != null)
			{
				thumb.DragDelta += DragBottomLeft;
			}
		}

		public static Window GetLeftResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(LeftResize);
		}

		public static void SetLeftResize(DependencyObject obj, Window window)
		{
			obj.SetValue(LeftResize, window);
		}

		public static readonly DependencyProperty LeftResize = DependencyProperty.RegisterAttached("LeftResize",
			typeof(Window), typeof(WindowResizeBehavior),
			new UIPropertyMetadata(null, OnLeftResizeChanged));

		private static void OnLeftResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var thumb = sender as Thumb;

			if (thumb != null)
			{
				thumb.DragDelta += DragLeft;
			}
		}

		public static Window GetRightResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(RightResize);
		}

		public static void SetRightResize(DependencyObject obj, Window window)
		{
			obj.SetValue(RightResize, window);
		}

		public static readonly DependencyProperty RightResize = DependencyProperty.RegisterAttached("RightResize",
			typeof(Window), typeof(WindowResizeBehavior),
			new UIPropertyMetadata(null, OnRightResizeChanged));

		private static void OnRightResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var thumb = sender as Thumb;

			if (thumb != null)
			{
				thumb.DragDelta += DragRight;
			}
		}

		public static Window GetTopResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(TopResize);
		}

		public static void SetTopResize(DependencyObject obj, Window window)
		{
			obj.SetValue(TopResize, window);
		}

		public static readonly DependencyProperty TopResize = DependencyProperty.RegisterAttached("TopResize",
			typeof(Window), typeof(WindowResizeBehavior),
			new UIPropertyMetadata(null, OnTopResizeChanged));

		private static void OnTopResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var thumb = sender as Thumb;

			if (thumb != null)
			{
				thumb.DragDelta += DragTop;
			}
		}

		public static Window GetBottomResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(BottomResize);
		}

		public static void SetBottomResize(DependencyObject obj, Window window)
		{
			obj.SetValue(BottomResize, window);
		}

		public static readonly DependencyProperty BottomResize = DependencyProperty.RegisterAttached("BottomResize",
			typeof(Window), typeof(WindowResizeBehavior),
			new UIPropertyMetadata(null, OnBottomResizeChanged));

		private static void OnBottomResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var thumb = sender as Thumb;

			if (thumb != null)
			{
				thumb.DragDelta += DragBottom;
			}
		}

		private static void DragLeft(object sender, DragDeltaEventArgs e)
		{
			var thumb = sender as Thumb;
			var window = thumb.GetValue(LeftResize) as Window;

			if (window != null)
			{
				var horizontalChange = window.SafeWidthChange(e.HorizontalChange, false);
				window.Width -= horizontalChange;
				window.Left += horizontalChange;

			}
		}

		private static void DragRight(object sender, DragDeltaEventArgs e)
		{
			var thumb = sender as Thumb;
			var window = thumb.GetValue(RightResize) as Window;

			if (window != null)
			{
				var horizontalChange = window.SafeWidthChange(e.HorizontalChange);
				window.Width += horizontalChange;
			}
		}

		private static void DragTop(object sender, DragDeltaEventArgs e)
		{
			var thumb = sender as Thumb;
			var window = thumb.GetValue(TopResize) as Window;

			if (window != null)
			{
				var verticalChange = window.SafeHeightChange(e.VerticalChange, false);
				window.Height -= verticalChange;
				window.Top += verticalChange;
			}
		}

		private static void DragBottom(object sender, DragDeltaEventArgs e)
		{
			var thumb = sender as Thumb;
			var window = thumb.GetValue(BottomResize) as Window;

			if (window != null)
			{
				var verticalChange = window.SafeHeightChange(e.VerticalChange);
				window.Height += verticalChange;
			}
		}

		private static void DragTopLeft(object sender, DragDeltaEventArgs e)
		{
			var thumb = sender as Thumb;

			var window = thumb.GetValue(TopLeftResize) as Window;

			if (window != null)
			{
				var verticalChange = window.SafeHeightChange(e.VerticalChange, false);
				var horizontalChange = window.SafeWidthChange(e.HorizontalChange, false);

				window.Width -= horizontalChange;
				window.Left += horizontalChange;
				window.Height -= verticalChange;
				window.Top += verticalChange;
			}
		}

		private static void DragTopRight(object sender, DragDeltaEventArgs e)
		{
			var thumb = sender as Thumb;
			var window = thumb.GetValue(TopRightResize) as Window;

			if (window != null)
			{
				var verticalChange = window.SafeHeightChange(e.VerticalChange, false);
				var horizontalChange = window.SafeWidthChange(e.HorizontalChange);

				window.Width += horizontalChange;
				window.Height -= verticalChange;
				window.Top += verticalChange;
			}
		}

		private static void DragBottomRight(object sender, DragDeltaEventArgs e)
		{
			var thumb = sender as Thumb;
			var window = thumb.GetValue(BottomRightResize) as Window;

			if (window != null)
			{
				var verticalChange = window.SafeHeightChange(e.VerticalChange);
				var horizontalChange = window.SafeWidthChange(e.HorizontalChange);

				window.Width += horizontalChange;
				window.Height += verticalChange;
			}
		}

		private static void DragBottomLeft(object sender, DragDeltaEventArgs e)
		{
			var thumb = sender as Thumb;
			var window = thumb.GetValue(BottomLeftResize) as Window;

			if (window != null)
			{
				var verticalChange = window.SafeHeightChange(e.VerticalChange);
				var horizontalChange = window.SafeWidthChange(e.HorizontalChange, false);

				window.Width -= horizontalChange;
				window.Left += horizontalChange;
				window.Height += verticalChange;
			}
		}

		private static double SafeWidthChange(this Window window, double change, bool positive = true)
		{
			var result = positive ? window.Width + change : window.Width - change;

			if (result <= window.MinWidth)
			{
				return 0;
			}
			else if (result >= window.MaxWidth)
			{
				return 0;
			}
			else if (result < 0)
			{
				return 0;
			}
			else
			{
				return change;
			}
		}

		private static double SafeHeightChange(this Window window, double change, bool positive = true)
		{
			var result = positive ? window.Height + change : window.Height - change;

			if (result <= window.MinHeight)
			{
				return 0;
			}
			else if (result >= window.MaxHeight)
			{
				return 0;
			}
			else if (result < 0)
			{
				return 0;
			}
			else
			{
				return change;
			}
		}
	}

	public class WindowMaximizeCommand : ICommand
	{

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public event EventHandler CanExecuteChanged;

		public void Execute(object parameter)
		{
			var window = parameter as Window;

			if (window != null)
			{
				if (window.WindowState == WindowState.Maximized)
				{
					window.WindowState = WindowState.Normal;
				}
				else
				{
					window.WindowState = WindowState.Maximized;
				}
			}
		}
	}

	public static class WindowDragBehavior
	{
		public static Window GetLeftMouseButtonDrag(DependencyObject obj)
		{
			return (Window)obj.GetValue(LeftMouseButtonDrag);
		}

		public static void SetLeftMouseButtonDrag(DependencyObject obj, Window window)
		{
			obj.SetValue(LeftMouseButtonDrag, window);
		}

		public static readonly DependencyProperty LeftMouseButtonDrag = DependencyProperty.RegisterAttached("LeftMouseButtonDrag",
			typeof(Window), typeof(WindowDragBehavior),
			new UIPropertyMetadata(null, OnLeftMouseButtonDragChanged));

		private static void OnLeftMouseButtonDragChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var element = sender as UIElement;

			if (element != null)
			{
				element.MouseLeftButtonDown += ButtonDown;

			}
		}

		private static void ButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var element = sender as UIElement;

			var targetWindow = element.GetValue(LeftMouseButtonDrag) as Window;

			if (targetWindow != null)
			{
				targetWindow.DragMove();
			}
		}
	}
#pragma warning restore IDE0019 // Use pattern matching
}
