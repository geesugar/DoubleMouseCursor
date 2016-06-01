using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using MouseEvent;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows;

namespace DoubleCursor
{
    class IPC
    {
        LocalIndicator localIndicator;
        RemoteIndicator remoteIndicator;
        LocalCursor localCursor;
        RemoteCursor remoteCursor;

        #region PInvoke

        [DllImport("user32.dll", EntryPoint = "mouse_event")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(ref MouseEvent.Hook.POINT point);

        private int MOUSEEVENTF_ABSOLUTE = 0x8000;
        private int MOUSEEVENTF_MOVE = 0x0001;

        #endregion

        private int port { set; get; }
        private bool isRuning = true;
        private static Hook mouseHook;

        public IPC(int port)
        {
            this.port = port;

            mouseHook = new Hook("Mouse hook.");
            mouseHook.mouseOwnChange += MouseOwnChange;
            mouseHook.mouseMoveEvent += MouseMoveEvent;

            localIndicator = new LocalIndicator();
            remoteIndicator = new RemoteIndicator();
            localIndicator.Show();
            remoteIndicator.Show();
          
            localCursor = new LocalCursor();
            MemoryStream localImagMemory = new MemoryStream();
            var localCursorImg = DoubleCursor.Properties.Resources.localCursor;
            localCursorImg.Save(localImagMemory, System.Drawing.Imaging.ImageFormat.Png);
            ImageSourceConverter converter = new ImageSourceConverter();
            ImageSource source = (ImageSource)converter.ConvertFrom(localImagMemory);
            Grid gd = localCursor.Content as Grid;
            Image localImg = gd.Children[0] as Image;
            localImg.Source = source;
            localCursor.Hide();

            remoteCursor = new RemoteCursor();
            MemoryStream remoteImgMemory = new MemoryStream();
            var remoteCursorImg = DoubleCursor.Properties.Resources.remoteCursor;
            remoteCursorImg.Save(remoteImgMemory, System.Drawing.Imaging.ImageFormat.Png);
            source = (ImageSource)converter.ConvertFrom(remoteImgMemory);
            gd = remoteCursor.Content as Grid;
            Image remoteImg = gd.Children[0] as Image;
            remoteImg.Source = source;
            remoteCursor.Show();
        }

        public void start()
        {
            MouseEvent.Hook.POINT mousePos = new MouseEvent.Hook.POINT(0, 0);
            GetCursorPos(ref mousePos); 
            var hLocalCursor = new WindowInteropHelper(localCursor);
            var hLocalIndicator = new WindowInteropHelper(localIndicator);
            var hRemoteCursor = new WindowInteropHelper(remoteCursor);
            var hRemoteIndicator = new WindowInteropHelper(remoteIndicator);
            SetWindowPos(hLocalCursor.Handle, -1, mousePos.X, mousePos.Y, 0, 0, 0x0040 | 0x0001);
            SetWindowPos(hLocalIndicator.Handle, -1, mousePos.X, mousePos.Y, 0, 0, 0x0040 | 0x0001);
            SetWindowPos(hRemoteCursor.Handle, -1, mousePos.X, mousePos.Y, 0, 0, 0x0040 | 0x0001);
            SetWindowPos(hRemoteIndicator.Handle, -1, mousePos.X, mousePos.Y, 0, 0, 0x0040 | 0x0001);
        }

        public void exitProcess()
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void MouseMoveEvent(bool isLocal, MouseEvent.Hook.POINT pt)
        {
            //Console.WriteLine("MouseMoveEvent  isLocal:{0} x:{1} y:{2}", isLocal, pt.X, pt.Y);
            if (isLocal)
            {
                var hLocalCursor = new WindowInteropHelper(localCursor);
                var hLocalIndicator = new WindowInteropHelper(localIndicator);
                SetWindowPos(hLocalCursor.Handle, -1, pt.X, pt.Y, 0, 0, 0x0040 | 0x0001);
                SetWindowPos(hLocalIndicator.Handle, -1, pt.X, pt.Y, 0, 0, 0x0040 | 0x0001);
            }
            else
            {
                var hRemoteCursor = new WindowInteropHelper(remoteCursor);
                var hRemoteIndicator = new WindowInteropHelper(remoteIndicator);
                SetWindowPos(hRemoteCursor.Handle, -1, pt.X, pt.Y, 0, 0, 0x0040 | 0x0001);
                SetWindowPos(hRemoteIndicator.Handle, -1, pt.X, pt.Y, 0, 0, 0x0040 | 0x0001);
            }
        }

        /// <summary>
        /// Change cursor own status
        /// </summary>
        /// <param name="isLocal">true: remote->local; false: local->remote</param>
        /// <param name="pt">move cursor to the position</param>
        private void MouseOwnChange(bool isLocal, MouseEvent.Hook.POINT pt){
            //move cursor to pt
            Console.WriteLine("move cursor to x:{0} y:{1}", pt.X, pt.Y);
            int x = (int)(pt.X * 65535 / SystemParameters.PrimaryScreenWidth);
            int y = (int)(pt.Y * 65535 / SystemParameters.PrimaryScreenHeight);
            mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, x, y, 0, 0);

            if (isLocal)
            {
                localCursor.Hide();
                remoteCursor.Show();
            }
            else
            {
                localCursor.Show();
                remoteCursor.Hide();
            }
        }
    }
}
