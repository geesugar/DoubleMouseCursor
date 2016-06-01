using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MouseEvent
{
    public class Hook
    {
        #region PInvoke
        private enum HookType : int
        {
            WH_JOURNALRECORD = 0,
            WH_JOURNALPLAYBACK = 1,
            WH_KEYBOARD = 2,
            WH_GETMESSAGE = 3,
            WH_CALLWNDPROC = 4,
            WH_CBT = 5,
            WH_SYSMSGFILTER = 6,
            WH_MOUSE = 7,
            WH_HARDWARE = 8,
            WH_DEBUG = 9,
            WH_SHELL = 10,
            WH_FOREGROUNDIDLE = 11,
            WH_CALLWNDPROCRET = 12,
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14
        }

        public struct KBDLLHOOKSTRUCT
        {
            public UInt32 vkCode;
            public UInt32 scanCode;
            public UInt32 flags;
            public UInt32 time;
            public IntPtr extraInfo;
        }

        public struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        public struct KBDLLMOUSEHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        
        private int WM_LBUTTONDOWN = 0x0201;
        private int WM_LBUTTONUP = 0x0202;
        private int WM_MOUSEMOVE = 0x0200;
        private int WM_MOUSEWHEEL = 0x020A;
        private int WM_MOUSEHWHEEL = 0x020E;
        private int WM_RBUTTONDOWN = 0x0204;
        private int WM_RBUTTONUP = 0x0205;

        private int LLMHF_INJECTED = 0x00000001;
        private int LLMHF_LOWER_IL_INJECTED = 0x00000002;

        private POINT localPoint = new POINT(0, 0);
        private POINT remotePoint = new POINT(0, 0);
        private POINT prePoint = new POINT(0, 0);

        private int WM_KEYDOWN = 0x100;
        private int WM_KEYUP = 0x101;

        private int WM_SYSKEYDOWN = 0x0104;
        private int WM_SYSKEYUP = 0x105;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(HookType idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll")]
        private static extern int UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref KBDLLMOUSEHOOKSTRUCT lParam);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(ref POINT point);

        private delegate int HookProc(int code, IntPtr wParam, ref KBDLLMOUSEHOOKSTRUCT lParam);
        #endregion

        public string name { get; private set; }

        //isLocal:true, 本地鼠标移动 false，远程鼠标移动
        public delegate void MouseMoveEventDelegate(bool isLocal, POINT pt);
        public MouseMoveEventDelegate mouseMoveEvent = delegate { };

        //isLocal: true, 将状态转换为本地状态 false，将状态转换为remote状态
        public delegate void MouseOwnChangeDelegate(bool isLocal, POINT pt);
        public MouseOwnChangeDelegate mouseOwnChange = delegate { };

        HookProc _hookproc;
        IntPtr _hhook;

        bool _ispaused = false;
        public bool isPaused
        {
            get
            {
                return _ispaused;
            }
            set
            {
                if (value != isPaused && value == true)
                    stopHook();

                if (value != _ispaused && value == false)
                    startHook();

                _ispaused = value;
            }
        }

        public Hook(string name)
        {
            this.name = name;
            GetCursorPos(ref localPoint);
            GetCursorPos(ref remotePoint);
            GetCursorPos(ref prePoint); 
            startHook();
        }

        //cursor拥有状态，true为本地拥有，false为remote拥有
        private bool cursorLocalOwn = true;

        //更改鼠标拥有状态
        //输入：isLocal: 是否为本地点击事件
        //返回：false：继续响应鼠标点击事件， true：不响应鼠标点击事件
        public bool changeCursorOwn(bool isLocal){
            Console.WriteLine("isLocal " + isLocal);
            if (isLocal)
            {
                if (cursorLocalOwn)
                    return false;
                else
                {
                    //remote ---> local
                    cursorLocalOwn = true;
                    //存储remotePoint;
                    GetCursorPos(ref remotePoint); 
                    mouseOwnChange(true, localPoint);
                    return true;
                }
            }
            else
            {
                if (!cursorLocalOwn)
                    return false;
                else
                {
                    //local ---> remote
                    cursorLocalOwn = false;
                    //存储localPoint;
                    GetCursorPos(ref localPoint); 
                    mouseOwnChange(false, remotePoint);
                    return true;
                }

            }
        }

        private void startHook(){
            _hookproc = new HookProc(mouseHookCallback);
            _hhook = SetWindowsHookEx(HookType.WH_MOUSE_LL, _hookproc, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);

            if (_hhook == null || _hhook == IntPtr.Zero)
            {
                Win32Exception LastError = new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private void stopHook()
        {
            UnhookWindowsHookEx(_hhook);
        }

        private int mouseHookCallback(int code, IntPtr wParam, ref KBDLLMOUSEHOOKSTRUCT lParam)
        {
            //result为0表示将该消息传递给windows消息处理函数，result为1表示丢弃该消息。
            int result = 0;
            try
            {
                if (((lParam.flags & LLMHF_INJECTED) == 0) && ((lParam.flags & LLMHF_LOWER_IL_INJECTED) == 0))
                {
                    //本地鼠标
                    if (wParam.ToInt32() == WM_LBUTTONDOWN)
                    {
                        Console.WriteLine("A flags: " + lParam.flags);
                        //鼠标点击
                        result = changeCursorOwn(true) ? 1: 0;
                    }
                    else if(wParam.ToInt32() == WM_MOUSEMOVE){                        
                        if (cursorLocalOwn)
                        {
                            mouseMoveEvent(true, lParam.pt); //鼠标移动, 本地鼠标
                        }
                        else
                        {
                            int dx = lParam.pt.X - prePoint.X;
                            int dy = lParam.pt.Y - prePoint.Y;
                            localPoint.X = localPoint.X + dx;
                            localPoint.Y = localPoint.Y + dy;
                            mouseMoveEvent(true, localPoint);
                            result = 1;   //鼠标不移动
                        }
                    }
                }
                else
                {
                    //远程鼠标
                    if (wParam.ToInt32() == WM_LBUTTONDOWN)
                    {
                        //鼠标点击
                        Console.WriteLine("B flags: " + lParam.flags);
                        result = changeCursorOwn(false) ? 1 : 0;
                    }
                    else if (wParam.ToInt32() == WM_MOUSEMOVE)
                    {
                        if (!cursorLocalOwn)
                        {
                            mouseMoveEvent(false, lParam.pt);
                        }
                        else
                        {
                            //int dx = lParam.pt.X - prePoint.X;
                            //int dy = lParam.pt.Y - prePoint.Y;
                            //remotePoint.X = remotePoint.X + dx;
                            //remotePoint.Y = remotePoint.Y + dy;
                            //Console.WriteLine("x:{0} X:{1} dx:{2} y:{3} y:{4} dy:{5}", lParam.pt.X, prePoint.X, dx, lParam.pt.Y, prePoint.Y, dy);
                            mouseMoveEvent(false, lParam.pt);
                            result = 1;   //鼠标不移动
                        }
                    }
                }
            }
            finally
            {
                CallNextHookEx(IntPtr.Zero, code, wParam, ref lParam);
            }
            GetCursorPos(ref prePoint); 
            return result;
        }

        ~Hook()
        {
            stopHook();
        }
    }
}
