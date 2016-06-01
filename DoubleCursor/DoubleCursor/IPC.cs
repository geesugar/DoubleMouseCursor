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

namespace DoubleCursor
{
    class IPC
    {
        LocalIndicator localIndicator;
        RemoteIndicator remoteIndicator;
        LocalCursor localCursor;
        RemoteCursor remoteCursor;

        #region PInvoke
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

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
            Thread ipcThread = new Thread(ipcThreadProc);
            ipcThread.Start();
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
                //remoteCursor.Show();
                //remoteIndicator.Show();
            }
        }

        //isLocal: true, 将状态转换为本地状态 false，将状态转换为remote状态
        private void MouseOwnChange(bool isLocal, MouseEvent.Hook.POINT pt){
            //移动cursor到pt
            mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, pt.X, pt.Y, 0, 0);

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

        private void remoteMove(int posx, int posy)
        {

        }

        private void remoteClick()
        {

        }

        private void ipcThreadProc(Object o)
        {
            Console.WriteLine("IPC thread start.... port:{0}", port);
            TcpClient client = null;
            NetworkStream stream = null;
            try
            {
                client = new TcpClient("localhost", port);
                stream = client.GetStream();
            }
            catch (SocketException excption)
            {
                Console.WriteLine(excption.StackTrace);
                //exitProcess();
                //return;
            }

            //while (isRuning)
            //{
            //    byte[] bytes = new Byte[1024];
            //    string data = string.Empty;
            //    int length = stream.Read(bytes, 0, bytes.Length);
            //    if (length > 0)
            //    {
            //        data = Encoding.Default.GetString(bytes, 0, length);
            //        //Console.WriteLine("receive data: " + data);
                   
            //        JObject obj = JObject.Parse(data);
            //        //Console.WriteLine("type: " + (string)obj["type"] + " length: " + obj.Count);
            //        string cmdType = (string)obj["type"];
            //        if (cmdType == "remoteMove")
            //        {
            //            int posx = (int)obj["posx"];
            //            int posy = (int)obj["posy"];
            //            if (posx >= 0 && posx <= 5000 && posy >= 0 && posy <= 5000)
            //            {
            //                remoteMove(posx, posy);
            //            }
            //        }
            //        else if (cmdType == "remoteClick")
            //        {
            //            remoteClick();
            //        }
            //    }
            //}
        }
    }
}
