using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BlueStacks4FPSTweaker
{
    public static class GameModeEngine
    {
        // =========================================================================
        // CONSTANTS & PINVOKE
        // =========================================================================
        public const int HOTKEY_ID_GAMEMODE = 9001;
        public const int HOTKEY_ID_CLEANCACHE = 9002;

        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint uMilliseconds);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetTimerResolution(uint desiredResolution, bool setResolution, out uint currentResolution);

        [DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern bool SetThreadPriority(IntPtr hThread, int nPriority);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private const int THREAD_SET_INFORMATION = 0x0020;
        private const int THREAD_PRIORITY_TIME_CRITICAL = 15;
        private const int THREAD_PRIORITY_HIGHEST = 2;
        private const int THREAD_PRIORITY_NORMAL = 0;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const byte VK_A = 0x41;
        private const byte VK_D = 0x44;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private static readonly UIntPtr SNAP_TAG = new UIntPtr(0x534E4150); // "SNAP" signature

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        // =========================================================================
        // STATE & OPTIONS (BoosterX Advanced Tweaks)
        // =========================================================================
        public static bool IsGameModeActive { get; private set; }

        // Card 1: Shell & System Profile
        public static bool OptDisableExplorer = true;
        public static bool OptDisableDwm = true;
        public static bool OptTimerResolution = true;
        public static bool OptCpuFocus = true;            // 100% Game CPU Focus (SystemResponsiveness = 0)

        // Card 2: Engine & Input Tuning
        public static bool OptCleanCacheOnLaunch = true;
        public static bool OptSnapTap = true;             // A+D Counter-Strafe SOCD
        public static bool OptDisableRiThreads = true;    // Disable RI Threads / Input Message Priority
        public static bool OptPCoreAffinity = true;       // P-Core CPU Affinity Lock
        public static bool OptPriorityBoost = true;       // High Priority Class

        // Restoration Backups
        private static bool _explorerWasKilled = false;
        private static bool _timerResSet = false;
        private static IntPtr _hookId = IntPtr.Zero;
        private static LowLevelKeyboardProc _hookProc;

        private static object _origSystemResp = null;
        private static object _origNetThrottle = null;
        private static bool _sysProfileTweaked = false;
        private static long _origAffinityMask = 0;

        // Snap Tap state tracking
        private static bool _physicallyHoldingA = false;
        private static bool _physicallyHoldingD = false;

        static GameModeEngine()
        {
            _hookProc = HookCallback;
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Deactivate();
        }

        // =========================================================================
        // ACTIVATION & RESTORATION
        // =========================================================================
        public static void Activate()
        {
            if (IsGameModeActive) return;
            IsGameModeActive = true;

            try
            {
                // 1. High precision timer resolution (0.5ms)
                if (OptTimerResolution)
                {
                    TimeBeginPeriod(1);
                    uint cur;
                    try { NtSetTimerResolution(5000, true, out cur); } catch { }
                    _timerResSet = true;
                }

                // 2. 100% Game CPU Focus (SystemResponsiveness = 0 & NetworkThrottlingIndex = 0xFFFFFFFF)
                if (OptCpuFocus)
                {
                    ApplySystemProfileTweaks(true);
                }

                // 3. Auto Cache Purge on launch
                if (OptCleanCacheOnLaunch)
                {
                    PurgeGameCaches();
                }

                // 4. Process Priority Boost (HD-Player.exe)
                if (OptPriorityBoost)
                {
                    BoostBlueStacksPriority();
                }

                // 5. P-Core CPU Affinity Lock (Anti-Stutter)
                if (OptPCoreAffinity)
                {
                    ApplyPCoreAffinity(true);
                }

                // 6. Disable RI Threads / Input Polling Priority
                if (OptDisableRiThreads)
                {
                    ApplyRiThreadTuning(true);
                }

                // 7. Snap Tap (A+D SOCD Counter-Strafe)
                if (OptSnapTap)
                {
                    StartSnapTapHook();
                }

                // 8. Disable Explorer (Kill taskbar & shell)
                if (OptDisableExplorer)
                {
                    KillExplorer();
                }
            }
            catch { }
        }

        public static void Deactivate()
        {
            if (!IsGameModeActive) return;
            IsGameModeActive = false;

            try
            {
                // 1. Stop Snap Tap Hook
                StopSnapTapHook();

                // 2. Restore Explorer
                if (_explorerWasKilled)
                {
                    RestoreExplorer();
                }

                // 3. Restore Timer Resolution
                if (_timerResSet)
                {
                    TimeEndPeriod(1);
                    uint cur;
                    try { NtSetTimerResolution(156250, false, out cur); } catch { }
                    _timerResSet = false;
                }

                // 4. Restore System Profile Tweaks (SystemResponsiveness)
                if (_sysProfileTweaked)
                {
                    ApplySystemProfileTweaks(false);
                }

                // 5. Restore BlueStacks Priority to Normal
                RestoreBlueStacksPriority();

                // 6. Restore CPU Affinity to all cores
                ApplyPCoreAffinity(false);

                // 7. Restore RI Threads Tuning
                ApplyRiThreadTuning(false);
            }
            catch { }
        }

        public static void Toggle()
        {
            if (IsGameModeActive)
                Deactivate();
            else
                Activate();
        }

        // =========================================================================
        // 100% CPU FOCUS & MULTIMEDIA SYSTEM PROFILE
        // =========================================================================
        private static void ApplySystemProfileTweaks(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            _origSystemResp = key.GetValue("SystemResponsiveness", 20);
                            _origNetThrottle = key.GetValue("NetworkThrottlingIndex", 10);

                            key.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                            key.SetValue("NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord);
                            _sysProfileTweaked = true;
                        }
                        else
                        {
                            if (_origSystemResp != null) key.SetValue("SystemResponsiveness", _origSystemResp, RegistryValueKind.DWord);
                            if (_origNetThrottle != null) key.SetValue("NetworkThrottlingIndex", _origNetThrottle, RegistryValueKind.DWord);
                            _sysProfileTweaked = false;
                        }
                    }
                }

                using (RegistryKey gamesKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", true))
                {
                    if (gamesKey != null)
                    {
                        if (enable)
                        {
                            gamesKey.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                            gamesKey.SetValue("Priority", 6, RegistryValueKind.DWord);
                            gamesKey.SetValue("Scheduling Category", "High", RegistryValueKind.String);
                            gamesKey.SetValue("SFIO Priority", "High", RegistryValueKind.String);
                        }
                        else
                        {
                            gamesKey.SetValue("Priority", 2, RegistryValueKind.DWord);
                            gamesKey.SetValue("Scheduling Category", "Medium", RegistryValueKind.String);
                            gamesKey.SetValue("SFIO Priority", "Normal", RegistryValueKind.String);
                        }
                    }
                }
            }
            catch { }
        }

        // =========================================================================
        // P-CORE CPU AFFINITY LOCK (ANTI-STUTTER)
        // =========================================================================
        private static void ApplyPCoreAffinity(bool enable)
        {
            try
            {
                int totalLogicalCores = Environment.ProcessorCount;
                long mask = 0;

                if (enable)
                {
                    // For modern multi-core systems, assign the primary performance cores
                    // (Avoid scheduling Android threads onto slower E-cores or high-latency CCDs)
                    if (totalLogicalCores >= 16)
                    {
                        // 8 P-Cores with HyperThreading = Cores 0-15
                        mask = 0xFFFFL;
                    }
                    else if (totalLogicalCores >= 12)
                    {
                        // 6 P-Cores with HyperThreading = Cores 0-11
                        mask = 0x0FFFL;
                    }
                    else if (totalLogicalCores >= 8)
                    {
                        // 4 P-Cores with HyperThreading = Cores 0-7
                        mask = 0x00FFL;
                    }
                    else
                    {
                        // All cores
                        mask = (1L << totalLogicalCores) - 1;
                    }
                }
                else
                {
                    // Restore to all logical cores
                    mask = (1L << totalLogicalCores) - 1;
                }

                string[] targets = { "HD-Player", "HD-Frontend", "BlueStacks" };
                foreach (string t in targets)
                {
                    foreach (Process p in Process.GetProcessesByName(t))
                    {
                        try
                        {
                            if (_origAffinityMask == 0)
                            {
                                _origAffinityMask = (long)p.ProcessorAffinity;
                            }
                            p.ProcessorAffinity = (IntPtr)mask;
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        // =========================================================================
        // DISABLE RI THREADS / INPUT POLLING PRIORITY BOOST
        // =========================================================================
        private static void ApplyRiThreadTuning(bool enable)
        {
            try
            {
                string[] targets = { "HD-Player", "HD-Frontend" };
                int priority = enable ? THREAD_PRIORITY_TIME_CRITICAL : THREAD_PRIORITY_NORMAL;

                foreach (string t in targets)
                {
                    foreach (Process p in Process.GetProcessesByName(t))
                    {
                        try
                        {
                            foreach (ProcessThread pt in p.Threads)
                            {
                                IntPtr hThread = OpenThread(THREAD_SET_INFORMATION, false, (uint)pt.Id);
                                if (hThread != IntPtr.Zero)
                                {
                                    SetThreadPriority(hThread, priority);
                                    CloseHandle(hThread);
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        // =========================================================================
        // EXPLORER CONTROLLER
        // =========================================================================
        private static void KillExplorer()
        {
            try
            {
                Process[] exps = Process.GetProcessesByName("explorer");
                if (exps.Length > 0)
                {
                    _explorerWasKilled = true;
                    ProcessStartInfo psi = new ProcessStartInfo("taskkill", "/f /im explorer.exe")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                }
            }
            catch { }
        }

        private static void RestoreExplorer()
        {
            try
            {
                Process[] exps = Process.GetProcessesByName("explorer");
                if (exps.Length == 0)
                {
                    string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                    string expPath = Path.Combine(winDir, "explorer.exe");
                    if (File.Exists(expPath))
                    {
                        Process.Start(new ProcessStartInfo(expPath) { UseShellExecute = true });
                    }
                }
            }
            catch { }
            finally
            {
                _explorerWasKilled = false;
            }
        }

        // =========================================================================
        // PROCESS PRIORITY BOOST
        // =========================================================================
        private static void BoostBlueStacksPriority()
        {
            try
            {
                string[] targets = { "HD-Player", "HD-Frontend", "BlueStacks" };
                foreach (string t in targets)
                {
                    foreach (Process p in Process.GetProcessesByName(t))
                    {
                        try
                        {
                            p.PriorityClass = ProcessPriorityClass.High;
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static void RestoreBlueStacksPriority()
        {
            try
            {
                string[] targets = { "HD-Player", "HD-Frontend", "BlueStacks" };
                foreach (string t in targets)
                {
                    foreach (Process p in Process.GetProcessesByName(t))
                    {
                        try
                        {
                            p.PriorityClass = ProcessPriorityClass.Normal;
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        // =========================================================================
        // CACHE & STANDBY RAM PURGE
        // =========================================================================
        public static void PurgeGameCaches()
        {
            try
            {
                Process[] allProcs = Process.GetProcesses();
                foreach (Process p in allProcs)
                {
                    try
                    {
                        EmptyWorkingSet(p.Handle);
                    }
                    catch { }
                }

                string temp = Path.GetTempPath();
                if (Directory.Exists(temp))
                {
                    foreach (string f in Directory.GetFiles(temp))
                    {
                        try { File.Delete(f); } catch { }
                    }
                }

                string pData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string bsLogs = Path.Combine(pData, @"BlueStacks\Logs");
                if (Directory.Exists(bsLogs))
                {
                    foreach (string f in Directory.GetFiles(bsLogs))
                    {
                        try { File.Delete(f); } catch { }
                    }
                }
            }
            catch { }
        }

        // =========================================================================
        // SNAP TAP (A+D SOCD COUNTER-STRAFE)
        // =========================================================================
        private static void StartSnapTapHook()
        {
            if (_hookId != IntPtr.Zero) return;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static void StopSnapTapHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
            _physicallyHoldingA = false;
            _physicallyHoldingD = false;
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && OptSnapTap && IsGameModeActive)
            {
                KBDLLHOOKSTRUCT hookStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                if (hookStruct.dwExtraInfo == SNAP_TAG)
                {
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                uint vk = hookStruct.vkCode;
                int msg = wParam.ToInt32();

                if (vk == VK_A || vk == VK_D)
                {
                    bool isKeyDown = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                    bool isKeyUp = (msg == WM_KEYUP || msg == WM_SYSKEYUP);

                    if (vk == VK_A)
                    {
                        if (isKeyDown)
                        {
                            if (!_physicallyHoldingA)
                            {
                                _physicallyHoldingA = true;
                                if (_physicallyHoldingD)
                                {
                                    keybd_event(VK_D, 0, KEYEVENTF_KEYUP, SNAP_TAG);
                                }
                            }
                        }
                        else if (isKeyUp)
                        {
                            _physicallyHoldingA = false;
                            if (_physicallyHoldingD)
                            {
                                keybd_event(VK_D, 0, 0, SNAP_TAG);
                            }
                        }
                    }
                    else if (vk == VK_D)
                    {
                        if (isKeyDown)
                        {
                            if (!_physicallyHoldingD)
                            {
                                _physicallyHoldingD = true;
                                if (_physicallyHoldingA)
                                {
                                    keybd_event(VK_A, 0, KEYEVENTF_KEYUP, SNAP_TAG);
                                }
                            }
                        }
                        else if (isKeyUp)
                        {
                            _physicallyHoldingD = false;
                            if (_physicallyHoldingA)
                            {
                                keybd_event(VK_A, 0, 0, SNAP_TAG);
                            }
                        }
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // =========================================================================
        // GLOBAL HOTKEYS REGISTRATION
        // =========================================================================
        public static void RegisterHotkeys(IntPtr hWnd)
        {
            try
            {
                RegisterHotKey(hWnd, HOTKEY_ID_GAMEMODE, MOD_CONTROL | MOD_SHIFT, (uint)Keys.O);
                RegisterHotKey(hWnd, HOTKEY_ID_CLEANCACHE, MOD_CONTROL | MOD_SHIFT, (uint)Keys.P);
            }
            catch { }
        }

        public static void UnregisterHotkeys(IntPtr hWnd)
        {
            try
            {
                UnregisterHotKey(hWnd, HOTKEY_ID_GAMEMODE);
                UnregisterHotKey(hWnd, HOTKEY_ID_CLEANCACHE);
            }
            catch { }
        }
    }
}
