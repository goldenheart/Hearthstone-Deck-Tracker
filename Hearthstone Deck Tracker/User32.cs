﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Hearthstone_Deck_Tracker
{
    internal class User32
    {
        private const int WsExTransparent = 0x00000020;
        private const int GwlExstyle = (-20);
        public const int SwRestore = 9;

        [DllImport("user32.dll")]
        public static extern IntPtr GetClientRect(IntPtr hWnd, ref Rect rect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            int extendedStyle = GetWindowLong(hwnd, GwlExstyle);
            SetWindowLong(hwnd, GwlExstyle, extendedStyle | WsExTransparent);
        }

        public static bool IsForegroundWindow(String lpWindowName)
        {
            return GetForegroundWindow() == FindWindow("UnityWndClass", lpWindowName);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        
        [Flags]
        public enum MouseEventFlags : uint
        {
            LeftDown = 0x00000002,
            LeftUp = 0x00000004,
            RightDown = 0x00000008,
            RightUp = 0x00000010
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out MousePoint lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct MousePoint
        {
            public int X;
            public int Y;
        }
        
        public static Point GetMousePos()
        {
            var p = new MousePoint();
            GetCursorPos(out p);
            return new Point(p.X, p.Y);
        }
        public static IntPtr GetHearthstoneWindow()
        {
	        return FindWindow("UnityWndClass", "Hearthstone");
        }
        public static Rectangle GetHearthstoneRect(bool dpiScaling)
        {
        	// Returns the co-ordinates of Hearthstone's client area in screen co-ordinates
            var hsHandle = GetHearthstoneWindow();
        	var rect = new Rect();
            var ptUL = new Point();
            var ptLR = new Point();
            
            GetClientRect(hsHandle, ref rect);
            
            ptUL.X = rect.left;
            ptUL.Y = rect.top;
 
            ptLR.X = rect.right;
            ptLR.Y = rect.bottom;
            
            ClientToScreen(hsHandle, ref ptUL);
            ClientToScreen(hsHandle, ref ptLR);
 
            if (dpiScaling)
            {
                ptUL.X = (int) (ptUL.X / Helper.DpiScalingX);
            	ptUL.Y = (int) (ptUL.Y / Helper.DpiScalingY);
                ptLR.X = (int) (ptLR.X / Helper.DpiScalingX);
            	ptLR.Y = (int) (ptLR.Y / Helper.DpiScalingY);
            }
            
            return new Rectangle(ptUL.X, ptUL.Y, ptLR.X - ptUL.X, ptLR.Y - ptUL.Y);
        }

        public static void BringHsToForeground()
        {
			var hsHandle = GetHearthstoneWindow();
            SetForegroundWindow(hsHandle);
        }

        public static void FlashHs()
        {
			var hsHandle = GetHearthstoneWindow();
            FlashWindow(hsHandle, false);
        }


        //http://joelabrahamsson.com/detecting-mouse-and-keyboard-input-with-net/
        public class WindowsHookHelper
        {
            public delegate IntPtr HookDelegate(
                Int32 code, IntPtr wParam, IntPtr lParam);

            [DllImport("User32.dll")]
            public static extern IntPtr CallNextHookEx(
                IntPtr hHook, Int32 nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("User32.dll")]
            public static extern IntPtr UnhookWindowsHookEx(IntPtr hHook);


            [DllImport("User32.dll")]
            public static extern IntPtr SetWindowsHookEx(
                Int32 idHook, HookDelegate lpfn, IntPtr hmod,
                Int32 dwThreadId);
        }



        public class MouseInput : IDisposable
		{
			public event EventHandler<EventArgs> LmbDown;
			public event EventHandler<EventArgs> LmbUp;
			public event EventHandler<EventArgs> MouseMoved;

            private WindowsHookHelper.HookDelegate mouseDelegate;
            private IntPtr mouseHandle;
            private const Int32 WH_MOUSE_LL = 14;
			private const Int32 WM_LBUTTONDOWN = 0x201;
			private const Int32 WM_LBUTTONUP = 0x0202;

            private bool disposed;

            public MouseInput()
            {
                mouseDelegate = MouseHookDelegate;
                mouseHandle = WindowsHookHelper.SetWindowsHookEx(WH_MOUSE_LL, mouseDelegate, IntPtr.Zero, 0);
            }

            private IntPtr MouseHookDelegate(Int32 code, IntPtr wParam, IntPtr lParam)
            {
                if (code < 0)
                    return WindowsHookHelper.CallNextHookEx(mouseHandle, code, wParam, lParam);


	            switch (wParam.ToInt32())
	            {
		            case WM_LBUTTONDOWN:
			            if (LmbDown != null)
				            LmbDown(this, new EventArgs());
			            break;
		            case WM_LBUTTONUP:
			            if (LmbUp != null)
				            LmbUp(this, new EventArgs());
			            break;
		            default:
			            if (MouseMoved != null)
				            MouseMoved(this, new EventArgs());
			            break;
	            }

	            return WindowsHookHelper.CallNextHookEx(mouseHandle, code, wParam, lParam);
            }

            public void Dispose()
            {
                Dispose(true);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (mouseHandle != IntPtr.Zero)
                        WindowsHookHelper.UnhookWindowsHookEx(mouseHandle);

                    disposed = true;
                }
            }

            ~MouseInput()
            {
                Dispose(false);
            }
        }
    }
}