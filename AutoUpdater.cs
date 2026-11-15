using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace BlueStacks4FPSTweaker
{
    public class UpdateCheckResult
    {
        public bool IsUpdateAvailable { get; set; }
        public string LatestVersion { get; set; }
        public string DownloadUrl { get; set; }
        public string ReleaseNotes { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class AutoUpdater
    {
        public const string CURRENT_VERSION = "2.0.0";
        public const string GITHUB_REPO = "susaracreations/Bloomex";
        private const string API_URL = "https://api.github.com/repos/susaracreations/Bloomex/releases/latest";

        public static void CheckForUpdatesAsync(Action<UpdateCheckResult> callback)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                UpdateCheckResult result = new UpdateCheckResult();
                try
                {
                    // Enable TLS 1.2 for GitHub API
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | SecurityProtocolType.Tls;

                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("User-Agent", "Bloomex-Updater-v" + CURRENT_VERSION);
                        client.Headers.Add("Accept", "application/vnd.github.v3+json");

                        string json;
                        try
                        {
                            json = client.DownloadString(API_URL);
                        }
                        catch (WebException wex)
                        {
                            var resp = wex.Response as HttpWebResponse;
                            if (resp != null && resp.StatusCode == HttpStatusCode.NotFound)
                            {
                                // No release published yet on GitHub repository
                                result.IsUpdateAvailable = false;
                                result.LatestVersion = CURRENT_VERSION;
                                if (callback != null) callback(result);
                                return;
                            }
                            result.ErrorMessage = wex.Message;
                            if (callback != null) callback(result);
                            return;
                        }

                        // Extract tag_name: "tag_name": "v0.2.0"
                        Match tagMatch = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                        if (!tagMatch.Success)
                        {
                            result.IsUpdateAvailable = false;
                            result.LatestVersion = CURRENT_VERSION;
                            if (callback != null) callback(result);
                            return;
                        }

                        string tag = tagMatch.Groups[1].Value.Trim();
                        result.LatestVersion = tag;

                        // Compare SemVer
                        string cleanRemote = tag.TrimStart('v', 'V');
                        string cleanCurrent = CURRENT_VERSION.TrimStart('v', 'V');

                        if (IsVersionNewer(cleanRemote, cleanCurrent))
                        {
                            result.IsUpdateAvailable = true;

                            // Extract browser_download_url for .exe
                            Match assetMatch = Regex.Match(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+\\.exe)\"");
                            if (assetMatch.Success)
                            {
                                result.DownloadUrl = assetMatch.Groups[1].Value;
                            }
                            else
                            {
                                // Fallback to HTML URL
                                Match htmlMatch = Regex.Match(json, "\"html_url\"\\s*:\\s*\"([^\"]+)\"");
                                if (htmlMatch.Success)
                                {
                                    result.DownloadUrl = htmlMatch.Groups[1].Value;
                                }
                            }

                            // Extract release notes (body)
                            Match bodyMatch = Regex.Match(json, "\"body\"\\s*:\\s*\"([^\"]*)\"");
                            if (bodyMatch.Success)
                            {
                                result.ReleaseNotes = Regex.Unescape(bodyMatch.Groups[1].Value);
                            }
                        }
                        else
                        {
                            result.IsUpdateAvailable = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = ex.Message;
                }

                if (callback != null)
                {
                    callback(result);
                }
            });
        }

        private static bool IsVersionNewer(string remote, string current)
        {
            try
            {
                Version vRemote = new Version(NormalizeVersion(remote));
                Version vCurrent = new Version(NormalizeVersion(current));
                return vRemote > vCurrent;
            }
            catch
            {
                return string.Compare(remote, current, StringComparison.OrdinalIgnoreCase) > 0;
            }
        }

        private static string NormalizeVersion(string ver)
        {
            string[] parts = ver.Split('.');
            if (parts.Length == 1) return ver + ".0.0.0";
            if (parts.Length == 2) return ver + ".0.0";
            if (parts.Length == 3) return ver + ".0";
            return ver;
        }

        public static void DownloadAndApplyUpdate(string downloadUrl, Action<int> onProgress, Action<string> onComplete, Action<string> onError)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | SecurityProtocolType.Tls;

                    string tempDir = Path.Combine(Path.GetTempPath(), "Bloomex_Update");
                    if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                    string newExePath = Path.Combine(tempDir, "Bloomex_new.exe");
                    if (File.Exists(newExePath)) File.Delete(newExePath);

                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("User-Agent", "Bloomex-Updater-v" + CURRENT_VERSION);

                        client.DownloadProgressChanged += (s, e) =>
                        {
                            if (onProgress != null) onProgress(e.ProgressPercentage);
                        };

                        client.DownloadFileCompleted += (s, e) =>
                        {
                            if (e.Error != null)
                            {
                                if (onError != null) onError(e.Error.Message);
                                return;
                            }

                            try
                            {
                                string currentExe = Application.ExecutablePath;
                                string batPath = Path.Combine(tempDir, "apply_update.bat");

                                string batContent = string.Format(
                                    "@echo off\r\n" +
                                    "timeout /t 1 /nobreak >nul\r\n" +
                                    ":retry\r\n" +
                                    "copy /Y \"{0}\" \"{1}\" >nul 2>&1\r\n" +
                                    "if errorlevel 1 (\r\n" +
                                    "    timeout /t 1 /nobreak >nul\r\n" +
                                    "    goto retry\r\n" +
                                    ")\r\n" +
                                    "start \"\" \"{1}\"\r\n" +
                                    "del /F /Q \"{0}\" >nul 2>&1\r\n" +
                                    "del /F /Q \"%~f0\" >nul 2>&1\r\n",
                                    newExePath, currentExe);

                                File.WriteAllText(batPath, batContent);

                                ProcessStartInfo psi = new ProcessStartInfo();
                                psi.FileName = batPath;
                                psi.CreateNoWindow = true;
                                psi.UseShellExecute = false;
                                Process.Start(psi);

                                if (onComplete != null) onComplete("Restarting Bloomex with update...");
                                Application.Exit();
                            }
                            catch (Exception ex)
                            {
                                if (onError != null) onError("Failed to launch updater: " + ex.Message);
                            }
                        };

                        client.DownloadFileAsync(new Uri(downloadUrl), newExePath);
                    }
                }
                catch (Exception ex)
                {
                    if (onError != null) onError(ex.Message);
                }
            });
        }
    }
}
