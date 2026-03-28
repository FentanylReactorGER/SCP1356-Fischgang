using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.Events;
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

        private static Timer _timer;

        private static string _licenseKey;
        private static string _apiBaseUrl;
        private static string _sharedSecret;
        private static string _pluginName;
        private static string _pluginVersion;
        private static int _checkIntervalSeconds;
        private static string _serverDomain;

        public static bool IsLicensed { get; private set; }
        public static bool IsChecked { get; private set; } = false;
        public static string LastMessage { get; private set; } = "Noch nicht geprüft";

        public static void Start(
            string licenseKey,
            string apiBaseUrl,
            string sharedSecret,
            string pluginName,
            string pluginVersion,
            int checkIntervalSeconds,
            string serverDomain)
        {
            _licenseKey = (licenseKey ?? string.Empty).Trim();
            _apiBaseUrl = NormalizeBaseUrl(apiBaseUrl);
            _sharedSecret = (sharedSecret ?? string.Empty).Trim();
            _pluginName = string.IsNullOrWhiteSpace(pluginName) ? "UnknownPlugin" : pluginName.Trim();
            _pluginVersion = string.IsNullOrWhiteSpace(pluginVersion) ? "1.0.0" : pluginVersion.Trim();
            _checkIntervalSeconds = checkIntervalSeconds < 10 ? 10 : checkIntervalSeconds;
            _serverDomain = (serverDomain ?? string.Empty).Trim();

            _ = Task.Run(CheckLicenseAsync);

            _timer?.Dispose();
            _timer = new Timer(_ =>
            {
                _ = Task.Run(CheckLicenseAsync);
            }, null, TimeSpan.FromSeconds(_checkIntervalSeconds), TimeSpan.FromSeconds(_checkIntervalSeconds));
        }

        public static void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public static bool EnsureLicensed() => IsLicensed;

        private static async Task CheckLicenseAsync()
        {
            try
            {
                string endpoint = $"{_apiBaseUrl}/api/plugin/check";

                string serverIdentity = await GetServerIdentity();
                if (string.IsNullOrWhiteSpace(serverIdentity))
                {
                    SetState(false, "Keine Domain/IP verfügbar.");
                    return;
                }

                Log.Debug($"[LicenseManager] Sende Identity: {serverIdentity}");
                Log.Debug($"[LicenseManager] Endpoint: {endpoint}");

                var payload = new LicenseRequest
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

                    HttpResponseMessage response = await Client.SendAsync(request);
                    string body = await response.Content.ReadAsStringAsync();

                    Log.Debug($"[LicenseManager] ResponseCode: {(int)response.StatusCode}");
                    Log.Debug($"[LicenseManager] ResponseBody: {body}");

                    if (!response.IsSuccessStatusCode)
                    {
                        SetState(false, $"HTTP {(int)response.StatusCode}: {body}");
                        return;
                    }

                    var result = JsonConvert.DeserializeObject<LicenseResponse>(body);
                    if (result == null)
                    {
                        SetState(false, "Ungültige Server-Antwort.");
                        return;
                    }

                    SetState(result.Ok && result.IsWhitelisted, result.Message ?? "Keine Nachricht");
                }
            }
            catch (TaskCanceledException ex)
            {
                SetState(false, $"Timeout beim Verbinden zum Lizenzserver: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                SetState(false, $"HTTP Fehler: {ex.Message}");
            }
            catch (Exception ex)
            {
                SetState(false, $"Allgemeiner Fehler: {ex.Message}");
            }
        }

        private static async Task<string> GetServerIdentity()
        {
            if (!string.IsNullOrWhiteSpace(_serverDomain))
            {
                string resolvedDomainIp = Tools.GetIp(_serverDomain);

                for (int i = 0; i < 20; i++)
                {
                    string currentIp = ServerConsole.Ip?.Trim();

                    if (!string.IsNullOrWhiteSpace(currentIp) &&
                        !currentIp.Equals("none", StringComparison.OrdinalIgnoreCase) &&
                        !currentIp.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase) &&
                        (currentIp.Equals(_serverDomain, StringComparison.OrdinalIgnoreCase) ||
                         currentIp.Equals(resolvedDomainIp, StringComparison.OrdinalIgnoreCase)))
                    {
                        Log.Debug($"[LicenseManager] Sende Domain: {_serverDomain}");

                        if (!IsChecked)
                        {
                            Tools.Send($"Ein neuer Server Names: {Tools.ServerUtils.GetCleanServerName()} mit Domain: {_serverDomain} und IP: {resolvedDomainIp} hat sich versucht zu Registieren. Seine Config Lizenz: {_licenseKey}.");
                            IsChecked = true;
                        }

                        return _serverDomain;
                    }

                    await Task.Delay(50);
                }
            }

            for (int i = 0; i < 20; i++)
            {
                string currentIp = ServerConsole.Ip?.Trim();

                if (!string.IsNullOrWhiteSpace(currentIp) &&
                    !currentIp.Equals("none", StringComparison.OrdinalIgnoreCase) &&
                    !currentIp.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase))
                {
                    if (!IsChecked)
                    {
                        Tools.Send($"Ein neuer Server Names: {Tools.ServerUtils.GetCleanServerName()} mit IP/Domain: {currentIp}, Falls Domain mit IP: {Tools.GetIp(currentIp)} hat sich versucht zu Registieren. Seine Config Lizenz: {_licenseKey}.");
                        IsChecked = true;
                    }

                    return currentIp;
                }

                await Task.Delay(50);
            }

            return null;
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

        public static bool IsVerified = false; 
        private static void SetState(bool licensed, string message)
        {
            IsLicensed = licensed;
            LastMessage = message ?? "Keine Nachricht";

            if (licensed)
            {
                Log.Info($"[LicenseManager] Lizenz gültig: {LastMessage}");
                IsVerified = true;
            }
            else
            {
                Log.Warn($"[LicenseManager] Lizenz ungültig: {LastMessage}");
                if (IsVerified)
                {
                    Log.Error($"Server wurde soeben entfernt! Wenden sie sich an TristanLikesUran und deaktivieren sie dieses Plugin.");
                }
            }
            
        }

        private sealed class LicenseRequest
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

        private sealed class LicenseResponse
        {
            [JsonProperty("ok")]
            public bool Ok { get; set; }

            [JsonProperty("isWhitelisted")]
            public bool IsWhitelisted { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }
    }
}