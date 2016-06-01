using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DoubleCursor
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {      
    }

    public static class EntryPoint
    {        
        [STAThread]
        public static void Main(string[] args)
        {
            bool isStart = false;
            System.Threading.Mutex m = new System.Threading.Mutex(true, "DoubleCursorStart", out isStart);
            if (!isStart)
            {
                Console.WriteLine("This module has instanced.");
                return;
            }

            int port = 6231;
            IPC ipc = new IPC(port);
            ipc.start();
 
            DoubleCursor.App app = new DoubleCursor.App();
            app.Run();
        }
    }
}
