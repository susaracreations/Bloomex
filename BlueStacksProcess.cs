using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace BlueStacks4FPSTweaker
{
    public static class BlueStacksProcess
    {
        // Prioritize Bluestacks.exe (the main GUI launcher) over HD-Player.exe!
        private static readonly string[] ExecutableCandidates = new string[]
        {
            "Bluestacks.exe",
            "BlueStacks.exe",
            "HD-RunApp.exe",
            "HD-Player.exe"
        };

        private static readonly string[] ProcessNames = new string[]
        {
            "Bluestacks",
            "BlueStacks",
            "HD-Player"
        };

        public static bool IsRunning(out int processId)
        {
            processId = 0;
            foreach (string name in ProcessNames)
            {
                Process[] procs = Process.GetProcessesByName(name);
                if (procs != null && procs.Length > 0)
                {
                    processId = procs[0].Id;
                    return true;
                }
            }
            return false;
        }

        public static string FindExecutable(string installDir, out string arguments)
        {
            arguments = string.Empty;

            // 1. Check inside provided install directory
            if (!string.IsNullOrEmpty(installDir) && Directory.Exists(installDir))
            {
                // First check Bluestacks.exe
                string bs = Path.Combine(installDir, "Bluestacks.exe");
                if (File.Exists(bs)) return bs;

                string bsUpper = Path.Combine(installDir, "BlueStacks.exe");
                if (File.Exists(bsUpper)) return bsUpper;

                string runApp = Path.Combine(installDir, "HD-RunApp.exe");
                if (File.Exists(runApp))
                {
                    arguments = "-json \"{\\\"app_icon_url\\\":\\\"\\\",\\\"app_name\\\":\\\"BlueStacks\\\",\\\"app_url\\\":\\\"\\\",\\\"app_pkg\\\":\\\"com.bluestacks.home\\\"}\"";
                    return runApp;
                }

                string hdPlayer = Path.Combine(installDir, "HD-Player.exe");
                if (File.Exists(hdPlayer))
                {
                    arguments = "-vmname Android";
                    return hdPlayer;
                }
            }

            // 2. Check standard desktop shortcut
            string publicDesktopLnk = @"C:\Users\Public\Desktop\BlueStacks.lnk";
            if (File.Exists(publicDesktopLnk))
            {
                return publicDesktopLnk;
            }

            // 3. Fallback search paths across standard drives
            string[] defaultDirs = new string[]
            {
                @"C:\Program Files\BlueStacks\",
                @"C:\Program Files (x86)\BlueStacks\",
                @"D:\Program Files\BlueStacks\",
                @"D:\BlueStacks\",
                @"E:\Program Files\BlueStacks\",
                @"E:\BlueStacks\"
            };

            foreach (string dir in defaultDirs)
            {
                if (Directory.Exists(dir))
                {
                    string candidate = Path.Combine(dir, "Bluestacks.exe");
                    if (File.Exists(candidate)) return candidate;

                    string candidateUpper = Path.Combine(dir, "BlueStacks.exe");
                    if (File.Exists(candidateUpper)) return candidateUpper;

                    string hdPlayer = Path.Combine(dir, "HD-Player.exe");
                    if (File.Exists(hdPlayer))
                    {
                        arguments = "-vmname Android";
                        return hdPlayer;
                    }
                }
            }

            return null;
        }

        public static bool Launch(string installDir, out string error)
        {
            error = string.Empty;
            string args;
            string exe = FindExecutable(installDir, out args);

            if (string.IsNullOrEmpty(exe))
            {
                error = "BlueStacks 4 launcher (Bluestacks.exe) not found at: " + (installDir ?? "unknown location");
                return false;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = exe;
                if (!string.IsNullOrEmpty(args))
                {
                    psi.Arguments = args;
                }
                
                string workDir = Path.GetDirectoryName(exe);
                if (Directory.Exists(workDir))
                {
                    psi.WorkingDirectory = workDir;
                }

                psi.UseShellExecute = true;
                Process p = Process.Start(psi);
                if (p == null && !exe.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    error = "Failed to spawn process.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = "Launch failed: " + ex.Message;
                return false;
            }
        }

        public static bool Stop(string installDir, out string error)
        {
            error = string.Empty;
            try
            {
                // Try HD-Quit.exe first
                if (!string.IsNullOrEmpty(installDir))
                {
                    string quitExe = Path.Combine(installDir, "HD-Quit.exe");
                    if (File.Exists(quitExe))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = quitExe;
                        psi.WorkingDirectory = installDir;
                        psi.UseShellExecute = false;
                        psi.CreateNoWindow = true;
                        Process p = Process.Start(psi);
                        if (p != null)
                        {
                            p.WaitForExit(3000);
                        }
                    }
                }

                // Force terminate any lingering BlueStacks processes
                foreach (string name in ProcessNames)
                {
                    Process[] procs = Process.GetProcessesByName(name);
                    foreach (Process p in procs)
                    {
                        try
                        {
                            if (!p.HasExited)
                            {
                                p.Kill();
                                p.WaitForExit(1500);
                            }
                        }
                        catch { }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                error = "Error stopping BlueStacks: " + ex.Message;
                return false;
            }
        }

        public static bool Restart(string installDir, out string error)
        {
            Stop(installDir, out error);
            Thread.Sleep(1500);
            return Launch(installDir, out error);
        }
    }
}
