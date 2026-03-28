using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Features;
using Newtonsoft.Json.Linq;
using SCP1356Main.Configs;

namespace SCP1356Main.API.Commands
{
    internal static class UpdateChecker
    {
        private const string ApiUrl = "https://api.github.com/repos/FentanylReactorGER/SCP1356-Fischgang/releases/latest";

        private static bool _registered;
        private static int _downloadingFlag;

        private static Config Cfg => Plugin.Singleton?.Config;
        private static string CurrentVersion => Plugin.Singleton?.Version.ToString() ?? "0.0.0";
        private static string PluginPath => Path.Combine(Paths.Plugins, "SCP1356Main.dll");

        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        static UpdateChecker()
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("SCP1356-Updater/1.0");
            Http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        public static void Register()
        {
            if (_registered || Cfg == null || !Cfg.EnableAutoUpdate)
                return;
            
            _registered = true;

            if (Cfg.EnableLogging)
                Log.Debug("[Updater] Registriert.");
            Task.Run(CheckAsync);
        }

        public static void Unregister()
        {
            if (!_registered)
                return;
            
            _registered = false;
            Interlocked.Exchange(ref _downloadingFlag, 0);

            if (Cfg?.EnableLogging == true)
                Log.Debug("[Updater] Deregistriert.");
        }
        

        private static async Task CheckAsync()
        {
            if (Cfg == null || !Cfg.EnableAutoUpdate)
                return;

            try
            {
                if (Cfg.EnableLogging)
                    Log.Info("[Updater] Suche nach Updates...");

                string json = await Http.GetStringAsync(ApiUrl);
                var obj = JObject.Parse(json);

                string latest = NormalizeVersion(obj["tag_name"]?.ToString());

                if (string.IsNullOrWhiteSpace(latest))
                {
                    Log.Warn("[Updater] Version konnte nicht gelesen werden.");
                    return;
                }

                var assets = obj["assets"];

                string dllUrl = assets
                    ?.Where(a =>
                    {
                        var name = a["name"]?.ToString() ?? "";
                        return name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                               && name.IndexOf("scp1356", StringComparison.OrdinalIgnoreCase) >= 0;
                    })
                    .Select(a => a["browser_download_url"]?.ToString())
                    .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));

                if (!IsNewer(CurrentVersion, latest))
                {
                    if (Cfg.EnableLogging)
                        Log.Info("[Updater] Plugin ist aktuell.");
                    return;
                }

                Log.Warn($"[Updater] Neue Version gefunden: {latest} (aktuell: {CurrentVersion})");

                if (Cfg.NotifyOnly)
                {
                    Log.Info("[Updater] Nur Benachrichtigung aktiviert.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(dllUrl))
                {
                    Log.Error("[Updater] Keine DLL im Release gefunden.");
                    return;
                }

                if (Interlocked.CompareExchange(ref _downloadingFlag, 1, 0) != 0)
                {
                    Log.Debug("[Updater] Download läuft bereits.");
                    return;
                }

                try
                {
                    if (await DownloadAndReplaceAsync(dllUrl, PluginPath, latest))
                    {
                        if (Cfg.RestartNextRound)
                        {
                            Log.Warn("[Updater] Neustart nächste Runde (rnr).");
                            Server.ExecuteCommand("rnr");
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _downloadingFlag, 0);
                }
            }
            catch (Exception ex)
            {
                Log.Error("[Updater] Fehler: " + ex.Message);
            }
        }

        private static string NormalizeVersion(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return string.Empty;

            tag = tag.Trim();
            if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                tag = tag.Substring(1);

            return tag;
        }

        private static bool IsNewer(string current, string latest)
        {
            if (Version.TryParse(current, out var c) && Version.TryParse(latest, out var l))
                return l > c;

            return false;
        }

        private static async Task<bool> DownloadAndReplaceAsync(string url, string path, string version)
        {
            try
            {
                Log.Info($"[Updater] Lade Version {version} herunter...");

                var data = await Http.GetByteArrayAsync(url);

                if (data == null || data.Length == 0)
                {
                    Log.Error("[Updater] Datei ist leer.");
                    return false;
                }

                if (Cfg.EnableBackup && File.Exists(path))
                {
                    string backup = path + ".backup";
                    File.Copy(path, backup, true);
                    Log.Warn($"[Updater] Backup erstellt: {backup}");
                }

                File.WriteAllBytes(path, data);

                Log.Warn($"[Updater] Update erfolgreich installiert: {version}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("[Updater] Download fehlgeschlagen: " + ex.Message);
                return false;
            }
        }
    }
}