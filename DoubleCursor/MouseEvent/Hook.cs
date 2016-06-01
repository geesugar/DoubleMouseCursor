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
        private POINT tmpPoint = new POINT(0, 0);

        private int WM_KEYDOWN = 0x100;
        private int WM_KEYUP = 0x101;

        private int WM_SYSKEYDOWN = 0x0104;
        private int WM_SYSKEYUP = 0x105;

        private int MOUSEEVENTF_ABSOLUTE = 0x8000;
        private int MOUSEEVENTF_MOVE = 0x0001;

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

        /// <summary>
        /// Mouse move event callback
        /// </summary>
        /// <param name="isLocal">true: local mouse move; false: remote mouse move</param>
        /// <param name="pt"></param>
        public delegate void MouseMoveEventDelegate(bool isLocal, POINT pt);
        public MouseMoveEventDelegate mouseMoveEvent = delegate { };

        /// <summary>
        /// Change mouse own callback (local or remote)
        /// </summary>
        /// <param name="isLocal">true: change status to local  false: change status to remote</param>
        /// <param name="pt"></param>
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

        //Which one owned the cursor (local or remote),  true: local; false: remote;
        private bool cursorLocalOwn = true;

        /// <summary>
        /// Mouse click, judged whether change cursor own
        /// </summary>
        /// <param name="isLocal">true: local mouse click; false: remote or inject mouse click event</param>
        /// <returns>false: system continue mouse event process; true: drop this click event</returns>
        private bool mouseClick(bool isLocal){
            Console.WriteLine("isLocal " + isLocal);
            if (isLocal)
            {
                if (cursorLocalOwn)
                    return false;
                else
                {
                    //remote ---> local
                    cursorLocalOwn = true;
                    //save remotePoint;
                    GetCursorPos(ref remotePoint);
                    tmpPoint.X = localPoint.X;
                    tmpPoint.Y = localPoint.Y;
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
                    //save localPoint;
                    GetCursorPos(ref localPoint);
                    prePoint.X = localPoint.X;
                    prePoint.Y = localPoint.Y;
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
            //result 0: windows continue process this event; 1: system drop this event
            int result = 0;
            try
            {
                if (((lParam.flags & LLMHF_INJECTED) == 0) && ((lParam.flags & LLMHF_LOWER_IL_INJECTED) == 0))
                {
                    //Local mouse event
                    if (wParam.ToInt32() == WM_LBUTTONDOWN)
                    {
                        //Mouse click
                        result = mouseClick(true) ? 1: 0;
                    }
                    else if(wParam.ToInt32() == WM_MOUSEMOVE){                        
                        if (cursorLocalOwn)
                        {
                            mouseMoveEvent(true, lParam.pt); //local mouse move
                            localPoint.X = lParam.pt.X;
                            localPoint.Y = lParam.pt.Y;
                        }
                        else
                        {
                            int dx = lParam.pt.X - prePoint.X;
                            int dy = lParam.pt.Y - prePoint.Y;
                            localPoint.X = localPoint.X + dx;
                            localPoint.Y = localPoint.Y + dy;
                            mouseMoveEvent(true, localPoint);
                            result = 1;   //mouse does not move
                        }
                    }
                }
                else
                {
                    
                    //Inject mouse event
                    if (wParam.ToInt32() == WM_MOUSEMOVE && lParam.pt.X <= localPoint.X + 1 && lParam.pt.X >= localPoint.X - 1
                        && lParam.pt.Y <= localPoint.Y + 1 && lParam.pt.Y >= localPoint.Y - 1)
                    {
                        Console.WriteLine("Inject mouse event {0} {1}", lParam.pt.X, lParam.pt.Y);
                        return result;
                    }
                    
                    if (wParam.ToInt32() == WM_LBUTTONDOWN)
                    {
                        //mouse click
                        result = mouseClick(false) ? 1 : 0;
                    }
                    else if (wParam.ToInt32() == WM_MOUSEMOVE)
                    {
                        if (!cursorLocalOwn)
                        {
                            remotePoint.X = lParam.pt.X;
                            remotePoint.Y = lParam.pt.Y;
                            mouseMoveEvent(false, lParam.pt);
                        }
                        else
                        {
                            mouseMoveEvent(false, lParam.pt);
                            result = 1;   //mouse does not move
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
