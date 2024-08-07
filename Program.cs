using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using DiscordRPC;

namespace AdvancedAntivirus
{
    internal class Program
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private static string WebhookUrl;
        private static readonly string SettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "antivirus_settings.txt");
        private static readonly string UserProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static readonly string DownloadsPath = Path.Combine(UserProfilePath, "Downloads");
        private static readonly string DesktopPath = Path.Combine(UserProfilePath, "Desktop");
        private static readonly string ProgramsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Program Files");

        private static readonly HashSet<string> SuspiciousExtensions = new HashSet<string>
        {
            ".exe", ".dll", ".bat", ".cmd", ".vbs", ".scr", ".pif", ".jar"
        };

        private static readonly HashSet<string> KnownMalwareSignatures = new HashSet<string>
        {
            "bbe8dba85e14c78e3f89af2c6a745aaeae1dedbb0762b94aea548610f7d862ac",
            "309d4552fb720d2fd8acec683332e278e1cf176604a734a3b87b412ad9acd1ac",
            "550b717e31ac80bb1a3c7a1980e01b930487558f7a8b6abf73bf11c8cb766e5a",
            "29c3d19d297b3ef44070eaa181925ccd24b78433611bd694629707c0e3222261",
            "cd7434cafaebb72991fc43e119c53cfcb0420e5ff5357b8e7641c057b9ee6b7b",
            "400617322c5d53d45bff5a1f43004650b1c5505bfd5b24374e312f3eddff37b6",
            "d15e223a03856420d2751a84a0a10dbc51fd65c5e63bb51ff719e35543b00b42",
            "339bb0afb889dead8194c01197add1225be46b566e9499b4b9f18ec49a90b3e7",
            "be945e34c392c6e10d0d4568d23dc4db2da16ae04853a6d461879ed2e18c4956",
            "c744bb712bd3a705e340234b2521fe2ffa5d4ce7da423f924f0e180c5d8da9b0",
            "754b76d8c8a5252ed8ea26ef8644920538dc0c40f7d24d84a157bece9c5f56b6",
            "c07225e09d12ffa61b7738f036be12dff42d69d8f233e502a71256e200a6b807"
        };

        private static readonly HashSet<string> SystemDirectories = new HashSet<string>
        {
            "C:\\Windows", "C:\\Program Files", "C:\\Program Files (x86)", "C:\\Users\\Default"
        };

        private static DiscordRpcClient rpcClient;

        private static async Task Main(string[] args)
        {
            EnsureSettingsFile();
            InitializeDiscordRPC();

            while (true)
            {
                await PerformFullScan();
                Console.WriteLine("Scan completed. Waiting for next scan...");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }

        private static void EnsureSettingsFile()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    Console.Write("Enter your Discord webhook URL: ");
                    WebhookUrl = Console.ReadLine();
                    File.WriteAllText(SettingsFilePath, WebhookUrl);
                }
                else
                {
                    WebhookUrl = File.ReadAllText(SettingsFilePath).Trim();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied to the path '{SettingsFilePath}': {ex.Message}");
                Environment.Exit(1);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IO error occurred: {ex.Message}");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static void InitializeDiscordRPC()
        {
            rpcClient = new DiscordRpcClient("1270709578518102046");
            rpcClient.Initialize();
            SetDiscordStatus("Idle");
        }

        private static void SetDiscordStatus(string status)
        {
            rpcClient.SetPresence(new RichPresence()
            {
                Details = $"Current Status: {status}",
                State = "Monitoring your system",
                Assets = new Assets()
                {
                    LargeImageKey = "antivirus",
                    LargeImageText = "Advanced Antivirus",
                    SmallImageKey = "nb_image",
                    SmallImageText = "Antivirus Status"
                }
            });
        }

        private static async Task PerformFullScan()
        {
            SetDiscordStatus("Scanning");

            var pathsToScan = new List<string> { DownloadsPath, DesktopPath, ProgramsPath };
            var reportFilePath = Path.Combine(UserProfilePath, "scan_report.txt");
            var report = new StringBuilder();
            report.AppendLine("**Full System Scan Report:**");

            var suspiciousFiles = new List<string>();

            try
            {
                foreach (var path in pathsToScan)
                {
                    report.AppendLine($"**Scanning Directory:** {path}");
                    await ScanDirectory(path, report, suspiciousFiles);
                }

                if (suspiciousFiles.Count > 10)
                {
                    var alertMessage = new StringBuilder();
                    alertMessage.AppendLine("! Attention !");
                    alertMessage.AppendLine("More than 10 viruses detected!");
                    alertMessage.AppendLine("Their directories:");

                    for (int i = 0; i < suspiciousFiles.Count; i++)
                    {
                        alertMessage.AppendLine($"{i + 1}. {suspiciousFiles[i]}");
                    }

                    await SendAlertToDiscord(alertMessage.ToString());
                }
                else if (report.Length == 0)
                {
                    report.AppendLine("**No suspicious files found.**");
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"**Error during scan:** {ex.Message}");
            }

            SetDiscordStatus("Idle");

            File.WriteAllText(reportFilePath, report.ToString());

            await SendReportToDiscord(reportFilePath);
        }

        private static async Task ScanDirectory(string path, StringBuilder report, List<string> suspiciousFiles)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    report.AppendLine($"**Directory does not exist:** {path}");
                    return;
                }

                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                     .Where(file => !IsSystemFile(file));

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);

                    if (fileInfo.Length < 1024 || fileInfo.Length > 20971520)
                    {
                        continue;
                    }

                    string fileHash = CalculateFileHash(fileInfo.FullName);
                    if (KnownMalwareSignatures.Contains(fileHash))
                    {
                        report.AppendLine($"**Suspicious File Detected:** {fileInfo.FullName}");
                        report.AppendLine($"**File Hash:** {fileHash}");
                        suspiciousFiles.Add(fileInfo.FullName);
                    }
                    else
                    {
                        report.AppendLine($"**File Hash:** {fileHash}");
                    }

                    await CheckFileBehavior(fileInfo, report);
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"**Error during directory scan:** {ex.Message}");
            }
        }

        private static bool IsSystemFile(string filePath)
        {
            return SystemDirectories.Any(dir => filePath.StartsWith(dir, StringComparison.OrdinalIgnoreCase));
        }

        private static string CalculateFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private static async Task CheckFileBehavior(FileInfo fileInfo, StringBuilder report)
        {
            try
            {
                var behavior = new StringBuilder();

                if (fileInfo.Length > 10485760)
                {
                    behavior.AppendLine($"**Large File Detected:** {fileInfo.FullName}");
                }

                if (behavior.Length > 0)
                {
                    report.AppendLine(behavior.ToString());
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"**Error checking file behavior:** {ex.Message}");
            }
        }

        private static async Task SendAlertToDiscord(string message)
        {
            try
            {
                var payload = new
                {
                    content = message
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await HttpClient.PostAsync(WebhookUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Alert successfully sent to Discord.");
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to send alert to Discord. Response: {responseBody}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error sending alert to Discord: {ex.Message}");
                await Task.Delay(3000);
            }
        }

        private static async Task SendReportToDiscord(string filePath)
        {
            try
            {
                using (var form = new MultipartFormDataContent())
                {
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        var fileContent = new StreamContent(fileStream);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                        form.Add(fileContent, "file", "scan_report.txt");

                        var response = await HttpClient.PostAsync(WebhookUrl, form);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("Report successfully sent to Discord.");
                        }
                        else
                        {
                            var responseBody = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Failed to send report to Discord. Response: {responseBody}");
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error sending report to Discord: {ex.Message}");
                await Task.Delay(3000);
            }
        }
    }
}