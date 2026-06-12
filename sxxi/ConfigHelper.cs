using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace sxxi
{
    public static class ConfigHelper
    {
        private const int ProcessWaitDelayMs = 2000;
        private const string SteamExeName = "steam.exe";
        private const string SteamWebHelperName = "steamwebhelper.exe";
        private const string SteamRegistryPath = @"SOFTWARE\Valve\Steam";

        private static string GetAppDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static string GetBackupPath()
        {
            string backupDir = Path.Combine(GetAppDirectory(), "backup");
            Directory.CreateDirectory(backupDir);
            return backupDir;
        }

        public static string ParseEya(string eya)
        {
            var tokenArr = eya.Split('.');
            if (tokenArr.Length != 3)
            {
                return null;
            }

            string base64 = tokenArr[1];
            int padding = base64.Length % 4;
            if (padding != 0)
            {
                base64 += new string('=', 4 - padding);
            }

            try
            {
                byte[] data = Convert.FromBase64String(base64);
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        public static async Task<string> DoLoginAsync(string accountName, string token)
        {
            try
            {
                if (accountName.Contains('@'))
                {
                    accountName = accountName.Split('@')[0];
                }

                string crc32AccountName = ComputeCrc32(accountName) + "1";
                string jsonData = ParseEya(token);
                if (string.IsNullOrEmpty(jsonData))
                {
                    return "Failed to parse token";
                }

                var jsonDoc = JsonDocument.Parse(jsonData);
                string steamId = jsonDoc.RootElement.GetProperty("sub").GetString();

                string mtbf = GenerateRandomDigits(9);
                string jwt = SteamEncrypt(token, accountName);
                string path = await GetSteamInstallPathAsync();
                string localVdfPath = GetLocalVdfPath();

                if (File.Exists(localVdfPath))
                {
                    File.Delete(localVdfPath);
                }

                Directory.CreateDirectory(Path.Combine(path, "config"));

                using (var key = Registry.CurrentUser.OpenSubKey(SteamRegistryPath, true))
                {
                    key?.SetValue("AutoLoginUser", accountName);
                }

                string config = BuildConfig(mtbf, steamId, accountName);
                string loginUsers = BuildLoginUsers(steamId, accountName);
                string local = BuildLocal(crc32AccountName, jwt);

                RemoveReadonly(Path.Combine(path, "config", "config.vdf"));
                File.WriteAllText(Path.Combine(path, "config", "config.vdf"), config, Encoding.UTF8);

                RemoveReadonly(Path.Combine(path, "config", "loginusers.vdf"));
                File.WriteAllText(Path.Combine(path, "config", "loginusers.vdf"), loginUsers, Encoding.UTF8);

                if (File.Exists(localVdfPath))
                {
                    RemoveReadonly(localVdfPath);
                    File.Delete(localVdfPath);
                }
                File.WriteAllText(localVdfPath, local, Encoding.UTF8);

                LaunchProcess(Path.Combine(path, SteamExeName));
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return ex.Message;
            }
        }

        private static int GetPid(string processName)
        {
            var processes = Process.GetProcessesByName(processName.Replace(".exe", ""));
            return processes.Length > 0 ? processes[0].Id : 0;
        }

        private static string ReadRegistryValue(string keyPath, string valueName)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                var value = key?.GetValue(valueName);
                return value?.ToString() ?? string.Empty;
            }
        }

        public static async Task<string> GetSteamInstallPathAsync()
        {
            int steamPid = GetPid(SteamExeName);
            string steamPath;

            if (steamPid != 0)
            {
                using (var process = Process.GetProcessById(steamPid))
                {
                    steamPath = process.MainModule.FileName;
                }
                await KillSteamAsync();
            }
            else
            {
                steamPath = ReadRegistryValue(@"Software\Classes\steam\Shell\Open\Command", "");
                steamPath = steamPath.Replace("\"", "");
                if (steamPath.Length > 6)
                    steamPath = steamPath.Substring(0, steamPath.Length - 6);
            }

            if (steamPath.Length > 9)
                steamPath = steamPath.Substring(0, steamPath.Length - 9);

            return steamPath;
        }

        private static string ComputeCrc32(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            uint crc = 0xFFFFFFFF;

            foreach (byte b in bytes)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
                }
            }

            crc ^= 0xFFFFFFFF;
            return crc.ToString("x8").TrimStart('0');
        }

        private static string SteamEncrypt(string token, string accountName)
        {
            byte[] dataToEncrypt = Encoding.UTF8.GetBytes(token);
            byte[] entropy = Encoding.UTF8.GetBytes(accountName);

            byte[] encryptedData = ProtectedData.Protect(
                dataToEncrypt,
                entropy,
                DataProtectionScope.CurrentUser
            );

            return BitConverter.ToString(encryptedData).Replace("-", "").ToLower();
        }

        private static string GenerateRandomDigits(int length)
        {
            var random = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => (char)('0' + random.Next(10))).ToArray());
        }

        private static string BuildLoginUsers(string steamId, string accountName)
        {
            return $@"users
{{
    {steamId}
    {{
        AccountName    ""{accountName}""
        PersonaName    ""alterra.lol""
        RememberPassword    ""1""
        WantsOfflineMode    ""0""
        SkipOfflineModeWarning    ""0""
        AllowAutoLogin    ""1""
        MostRecent    ""1""
        Timestamp    ""{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}""
    }}
}}";
        }

        private static string BuildConfig(string mtbf, string steamId, string accountName)
        {
            return $@"InstallConfigStore
{{
    Software
    {{
        Valve
        {{
            Steam
            {{
                AutoUpdateWindowEnabled    ""0""
                Accounts
                {{
                    {accountName}
                    {{
                        SteamID    ""{steamId}""
                    }}
                }}
                MTBF    ""{mtbf}""
                CellIDServerOverride    ""170""
                Rate    ""30000""
            }}
        }}
    }}
}}";
        }

        private static string BuildLocal(string crc32, string jwt)
        {
            return $@"MachineUserConfigStore
{{
    Software
    {{
        Valve
        {{
            Steam
            {{
                ConnectCache
                {{
                    {crc32}    ""{jwt}""
                }}
            }}
        }}
    }}
}}";
        }

        private static string GetLocalVdfPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appDataPath, "Steam", "local.vdf");
        }

        private static void RemoveReadonly(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                }
                catch (Exception ex)
                {
                    Logger.Error($"RemoveReadonly failed for {path}: {ex.Message}");
                }
            }
        }

        public static async Task ResetSteamAsync()
        {
            string path = await GetSteamInstallPathAsync();

            string[] directories = {
                Path.Combine(path, "userdata"),
                Path.Combine(path, "config")
            };

            foreach (var directory in directories)
            {
                if (Directory.Exists(directory))
                {
                    try
                    {
                        Directory.Delete(directory, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to delete {directory}: {ex.Message}");
                    }
                }
            }

            string localVdfPath = GetLocalVdfPath();
            if (File.Exists(localVdfPath))
            {
                File.Delete(localVdfPath);
            }

            LaunchProcess(Path.Combine(path, SteamExeName));
        }

        public static string GetCurrentAccount()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(SteamRegistryPath))
                {
                    return key?.GetValue("AutoLoginUser")?.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        public static async Task KillSteamAsync()
        {
            int steamPid = GetPid(SteamExeName);
            if (steamPid != 0)
            {
                RunTaskkill(SteamExeName);
                RunTaskkill(SteamWebHelperName);
                await Task.Delay(ProcessWaitDelayMs);
            }
        }

        private static void RunTaskkill(string processName)
        {
            try
            {
                using (var proc = new Process())
                {
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/f /im {processName}",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    proc.Start();
                    proc.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"taskkill {processName} failed: {ex.Message}");
            }
        }

        public static bool SaveCurrentAccounts(out string error)
        {
            error = null;
            try
            {
                string username = GetCurrentAccount();
                string vdfPath = GetLocalVdfPath();
                string path = GetSteamInstallPathAsync().GetAwaiter().GetResult();
                string backupPath = GetBackupPath();

                string userFile = Path.Combine(backupPath, "saved_user.txt");
                if (File.Exists(userFile))
                {
                    File.Delete(userFile);
                }

                CopyFileSafe(Path.Combine(path, "config", "config.vdf"),
                             Path.Combine(backupPath, "config.vdf"));

                CopyFileSafe(Path.Combine(path, "config", "loginusers.vdf"),
                             Path.Combine(backupPath, "loginusers.vdf"));

                CopyFileSafe(vdfPath, Path.Combine(backupPath, "local.vdf"));

                if (!string.IsNullOrEmpty(username))
                {
                    File.WriteAllText(userFile, username);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                error = ex.Message;
                return false;
            }
        }

        public static async Task<bool> RestoreSavedAccountsAsync(out string error)
        {
            error = null;
            await KillSteamAsync();
            try
            {
                string vdfPath = GetLocalVdfPath();
                string path = await GetSteamInstallPathAsync();
                string backupPath = GetBackupPath();

                CopyFileSafe(Path.Combine(backupPath, "config.vdf"),
                             Path.Combine(path, "config", "config.vdf"));

                CopyFileSafe(Path.Combine(backupPath, "loginusers.vdf"),
                             Path.Combine(path, "config", "loginusers.vdf"));

                CopyFileSafe(Path.Combine(backupPath, "local.vdf"), vdfPath);

                await StartSteamAsync();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                error = ex.Message;
                return false;
            }
        }

        public static async Task StartSteamAsync()
        {
            string path = await GetSteamInstallPathAsync();
            LaunchProcess(Path.Combine(path, SteamExeName));
        }

        private static void CopyFileSafe(string source, string destination)
        {
            try
            {
                File.Copy(source, destination, true);
            }
            catch (Exception ex)
            {
                Logger.Error($"Copy failed: {source} -> {destination}: {ex.Message}");
            }
        }

        private static void LaunchProcess(string fileName)
        {
            using (var proc = new Process())
            {
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = true
                };
                proc.Start();
            }
        }
    }
}
