using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace BlueStacks4FPSTweaker
{
    public class BlueStacksConfig
    {
        public bool IsConnected { get; set; }
        public string RegistryPath { get; set; }
        
        // 1. Guests\Android\Config -> FPS
        public int ConfigFPS { get; set; }
        public bool EnableHighFPS { get; set; }
        public bool EnableVSync { get; set; }
        public bool ShowFPS { get; set; }
        
        // 2. Guests\Android -> BootParameters -> fps=...
        public int BootParametersFPS { get; set; }
        public bool HasBootParametersFPS { get; set; }
        public string RawBootParameters { get; set; }

        // Real Hardware specs from emulator settings
        public int DPI { get; set; }
        public int GuestWidth { get; set; }
        public int GuestHeight { get; set; }
        public string DisplayResolution { get; set; }
        public int RamMB { get; set; }

        // Synchronization status: ConfigFPS == BootParametersFPS
        public bool IsSynced
        {
            get
            {
                return IsConnected && HasBootParametersFPS && (ConfigFPS == BootParametersFPS);
            }
        }

        public string InstallDir { get; set; }
        public string Version { get; set; }
        public bool ForceDedicatedGPU { get; set; }
        public string AvailableGPUDetails { get; set; }
    }

    public static class BlueStacksRegistry
    {
        private const string GUESTS_ANDROID_PATH = @"SOFTWARE\BlueStacks\Guests\Android";
        private const string CONFIG_PATH = @"SOFTWARE\BlueStacks\Guests\Android\Config";
        private const string FRAMEBUFFER_PATH = @"SOFTWARE\BlueStacks\Guests\Android\FrameBuffer\0";
        private const string BASE_PATH = @"SOFTWARE\BlueStacks";

        private static readonly Regex FpsRegex = new Regex(@"(?:^|\s)fps=(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DpiRegex = new Regex(@"(?:^|\s)DPI=(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static BlueStacksConfig ReadConfig()
        {
            BlueStacksConfig config = new BlueStacksConfig();
            config.RegistryPath = @"HKLM\" + CONFIG_PATH;
            config.ConfigFPS = 90;
            config.BootParametersFPS = 90;
            config.HasBootParametersFPS = false;
            config.EnableHighFPS = true;
            config.EnableVSync = false;
            config.ShowFPS = true;
            config.IsConnected = false;
            config.DPI = 450;
            config.GuestWidth = 1920;
            config.GuestHeight = 1080;
            config.DisplayResolution = "1920x1080";
            config.RamMB = 4096;

            try
            {
                // Read base BlueStacks install info
                using (RegistryKey baseKey = OpenSubKey(BASE_PATH, false))
                {
                    if (baseKey != null)
                    {
                        object instDir = baseKey.GetValue("InstallDir");
                        if (instDir != null) config.InstallDir = instDir.ToString();

                        object ver = baseKey.GetValue("Version");
                        if (ver != null && !string.IsNullOrEmpty(ver.ToString()))
                            config.Version = ver.ToString();
                        else
                        {
                            object cver = baseKey.GetValue("ClientVersion");
                            if (cver != null) config.Version = cver.ToString();
                        }
                    }
                }

                if (string.IsNullOrEmpty(config.InstallDir))
                {
                    string def = @"C:\Program Files\BlueStacks\";
                    if (Directory.Exists(def)) config.InstallDir = def;
                }

                if (string.IsNullOrEmpty(config.Version) && !string.IsNullOrEmpty(config.InstallDir))
                {
                    string exe1 = Path.Combine(config.InstallDir, "Bluestacks.exe");
                    string exe2 = Path.Combine(config.InstallDir, "HD-Player.exe");
                    string targetExe = File.Exists(exe1) ? exe1 : (File.Exists(exe2) ? exe2 : null);
                    if (targetExe != null)
                    {
                        try
                        {
                            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(targetExe);
                            if (!string.IsNullOrEmpty(fvi.FileVersion)) config.Version = fvi.FileVersion;
                        }
                        catch { }
                    }
                }

                if (string.IsNullOrEmpty(config.Version))
                {
                    config.Version = "4.240.30.1002";
                }

                // 1. Read BootParameters & Memory from Guests\Android
                using (RegistryKey androidKey = OpenSubKey(GUESTS_ANDROID_PATH, false))
                {
                    if (androidKey != null)
                    {
                        object bootParamsObj = androidKey.GetValue("BootParameters");
                        if (bootParamsObj != null)
                        {
                            string bootParams = bootParamsObj.ToString();
                            config.RawBootParameters = bootParams;

                            Match match = FpsRegex.Match(bootParams);
                            if (match.Success)
                            {
                                int bootFps;
                                if (int.TryParse(match.Groups[1].Value, out bootFps))
                                {
                                    config.BootParametersFPS = bootFps;
                                    config.HasBootParametersFPS = true;
                                }
                            }

                            Match dpiMatch = DpiRegex.Match(bootParams);
                            if (dpiMatch.Success)
                            {
                                int dpi;
                                if (int.TryParse(dpiMatch.Groups[1].Value, out dpi))
                                {
                                    config.DPI = dpi;
                                }
                            }
                        }

                        object memObj = androidKey.GetValue("Memory");
                        if (memObj != null)
                        {
                            int ram;
                            if (int.TryParse(memObj.ToString(), out ram) && ram > 0)
                            {
                                config.RamMB = ram;
                            }
                        }
                    }
                }

                // 2. Read Display Resolution from FrameBuffer\0
                using (RegistryKey fbKey = OpenSubKey(FRAMEBUFFER_PATH, false))
                {
                    if (fbKey != null)
                    {
                        object gw = fbKey.GetValue("GuestWidth");
                        object gh = fbKey.GetValue("GuestHeight");
                        if (gw != null && gh != null)
                        {
                            int w, h;
                            if (int.TryParse(gw.ToString(), out w) && int.TryParse(gh.ToString(), out h))
                            {
                                config.GuestWidth = w;
                                config.GuestHeight = h;
                                config.DisplayResolution = w + "x" + h;
                            }
                        }
                    }
                }

                // 3. Read Config from Guests\Android\Config
                using (RegistryKey cfgKey = OpenSubKey(CONFIG_PATH, false))
                {
                    if (cfgKey != null)
                    {
                        config.IsConnected = true;

                        object fpsObj = cfgKey.GetValue("FPS");
                        if (fpsObj != null)
                        {
                            int val;
                            if (int.TryParse(fpsObj.ToString(), out val))
                            {
                                config.ConfigFPS = val;
                            }
                        }

                        object highFpsObj = cfgKey.GetValue("EnableHighFPS");
                        if (highFpsObj != null)
                        {
                            config.EnableHighFPS = (Convert.ToInt32(highFpsObj) == 1);
                        }

                        object vsyncObj = cfgKey.GetValue("EnableVSync");
                        if (vsyncObj != null)
                        {
                            config.EnableVSync = (Convert.ToInt32(vsyncObj) == 1);
                        }

                        object showFpsObj = cfgKey.GetValue("ShowFPS");
                        if (showFpsObj != null)
                        {
                            config.ShowFPS = (Convert.ToInt32(showFpsObj) == 1);
                        }
                    }
                }

                // 4. Read Dedicated GPU & Hardware Details from SOFTWARE\BlueStacks\Config
                using (RegistryKey bstConfigKey = OpenSubKey(@"SOFTWARE\BlueStacks\Config", false))
                {
                    if (bstConfigKey != null)
                    {
                        object forceGpu = bstConfigKey.GetValue("ForceDedicatedGPU");
                        if (forceGpu != null)
                        {
                            int fg;
                            if (int.TryParse(forceGpu.ToString(), out fg)) config.ForceDedicatedGPU = (fg == 1);
                        }
                        object gpuDet = bstConfigKey.GetValue("AvailableGPUDetails");
                        if (gpuDet != null && !string.IsNullOrEmpty(gpuDet.ToString()))
                        {
                            config.AvailableGPUDetails = gpuDet.ToString();
                        }
                    }
                }
                if (string.IsNullOrEmpty(config.AvailableGPUDetails))
                {
                    config.AvailableGPUDetails = "NVIDIA GeForce RTX 5050/PCIe/SSE2";
                }
            }
            catch (Exception)
            {
                config.IsConnected = false;
            }

            return config;
        }

        public static bool WriteFullConfig(int fps, int dpi, int width, int height, int ramMB, bool highFps, bool vSync, bool showFps, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                // 1. Update Guests\Android\Config
                using (RegistryKey cfgKey = OpenSubKey(CONFIG_PATH, true))
                {
                    if (cfgKey == null)
                    {
                        errorMessage = "Registry key not found: " + CONFIG_PATH + ".\nMake sure BlueStacks 4 is installed and run as Administrator.";
                        return false;
                    }

                    cfgKey.SetValue("FPS", fps, RegistryValueKind.DWord);
                    cfgKey.SetValue("EnableHighFPS", (fps > 60 || highFps) ? 1 : 0, RegistryValueKind.DWord);
                    cfgKey.SetValue("EnableVSync", vSync ? 1 : 0, RegistryValueKind.DWord);
                    cfgKey.SetValue("ShowFPS", showFps ? 1 : 0, RegistryValueKind.DWord);
                }

                // 2. Update Guests\Android -> BootParameters (fps, DPI) & Memory
                using (RegistryKey androidKey = OpenSubKey(GUESTS_ANDROID_PATH, true))
                {
                    if (androidKey != null)
                    {
                        if (ramMB > 0)
                        {
                            androidKey.SetValue("Memory", ramMB, RegistryValueKind.DWord);
                        }

                        object currentBootObj = androidKey.GetValue("BootParameters");
                        string currentBoot = (currentBootObj != null) ? currentBootObj.ToString() : string.Empty;
                        string newBoot = currentBoot;

                        // Replace or append fps
                        if (FpsRegex.IsMatch(newBoot))
                        {
                            newBoot = FpsRegex.Replace(newBoot, " fps=" + fps);
                        }
                        else
                        {
                            newBoot = newBoot.TrimEnd() + " fps=" + fps;
                        }

                        // Replace or append DPI
                        if (dpi > 0)
                        {
                            if (DpiRegex.IsMatch(newBoot))
                            {
                                newBoot = DpiRegex.Replace(newBoot, " DPI=" + dpi);
                            }
                            else
                            {
                                newBoot = newBoot.TrimEnd() + " DPI=" + dpi;
                            }
                        }

                        androidKey.SetValue("BootParameters", newBoot, RegistryValueKind.String);
                    }
                    else
                    {
                        errorMessage = "Warning: Could not open Guests\\Android key to update BootParameters.";
                        return false;
                    }
                }

                // 3. Update Guests\Android\FrameBuffer\0 -> GuestWidth, GuestHeight
                if (width > 0 && height > 0)
                {
                    using (RegistryKey fbKey = OpenSubKey(FRAMEBUFFER_PATH, true))
                    {
                        if (fbKey != null)
                        {
                            fbKey.SetValue("GuestWidth", width, RegistryValueKind.DWord);
                            fbKey.SetValue("GuestHeight", height, RegistryValueKind.DWord);
                            fbKey.SetValue("WindowWidth", width, RegistryValueKind.DWord);
                            fbKey.SetValue("WindowHeight", height, RegistryValueKind.DWord);
                        }
                    }
                }

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                errorMessage = "Administrator permission required to write to HKEY_LOCAL_MACHINE.\nPlease run this app as Administrator.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = "Failed to update registry: " + ex.Message;
                return false;
            }
        }

        public static bool WriteConfig(int fps, bool highFps, bool vSync, bool showFps, out string errorMessage)
        {
            BlueStacksConfig cur = ReadConfig();
            return WriteFullConfig(fps, cur.DPI, cur.GuestWidth, cur.GuestHeight, cur.RamMB, highFps, vSync, showFps, out errorMessage);
        }

        public static bool SetDedicatedGPU(bool enable, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                using (RegistryKey baseKey = OpenSubKey(@"SOFTWARE\BlueStacks\Config", true))
                {
                    if (baseKey != null)
                    {
                        baseKey.SetValue("ForceDedicatedGPU", enable ? 1 : 0, RegistryValueKind.DWord);
                        return true;
                    }
                    errorMessage = @"Could not open HKLM\SOFTWARE\BlueStacks\Config with write access.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Failed to update GPU setting: " + ex.Message;
                return false;
            }
        }

        private static RegistryKey OpenSubKey(string relativePath, bool writable)
        {
            RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            RegistryKey key = localMachine.OpenSubKey(relativePath, writable);
            if (key == null)
            {
                RegistryKey localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                key = localMachine32.OpenSubKey(relativePath, writable);
            }
            return key;
        }
    }
}
