using System;
using System.Net.Http;
using System.Text;
using Exiled.API.Enums;
using Exiled.API.Features;
using Newtonsoft.Json;

namespace SCP1356Main.API.Commands
{
    public class Scp1356StatusService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiToken;

        public Scp1356StatusService(string baseUrl, string apiToken)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _apiToken = apiToken;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            
            _httpClient.DefaultRequestHeaders.Remove("x-api-token");
            _httpClient.DefaultRequestHeaders.Add("x-api-token", _apiToken);
        }

        public async void SetHp(float hp)
        {
            await PostAsync("/api/set/hp", new
            {
                hp = hp
            });
        }
        /// <summary>
        /// String Breach Status
        /// </summary>
        public async void SetBreachStatus(string breachStatus)
        {
            await PostAsync("/api/set/breach-status", new
            {
                breachStatus = breachStatus ?? "UNKNOWN"
            });
        }

        /// <summary>
        /// Räume = Jetziger, Vorheriger
        /// </summary>
        public async void SetRooms(RoomType currentRoom, RoomType previousRoom)
        {
            await PostAsync("/api/set/rooms", new
            {
                currentRoom = Plugin.Singleton.Translation.RoomTypesCustomLanguage[currentRoom],
                previousRoom = Plugin.Singleton.Translation.RoomTypesCustomLanguage[previousRoom]
            });
        }
        public async void SetRoomString(string currentRoom, string previousRoom)
        {
            await PostAsync("/api/set/rooms", new
            {
                currentRoom,
                previousRoom
            });
        }
        /// <summary>
        /// Gibt einen Custom Log
        /// </summary>
        public async void AddRemark(string message)
        {
            await PostAsync("/api/add-remark", new
            {
                message = message
            });
        }
        
        /// <summary>
        /// activity = 0 bis 10
        /// durationSeconds = wie lange diese Aktivität gehalten wird
        /// </summary>
        public async void SetActivity(int activity, int durationSeconds)
        {
            if (activity < 0)
                activity = 0;

            if (activity > 10)
                activity = 10;

            if (durationSeconds < 0)
                durationSeconds = 0;

            if (durationSeconds > 60)
                durationSeconds = 60;

            await PostAsync("/api/set/activity", new
            {
                activity = activity,
                duration = durationSeconds
            });
        }

        private async System.Threading.Tasks.Task PostAsync(string endpoint, object payload)
        {
            try
            {
                string json = JsonConvert.SerializeObject(payload);
                using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await _httpClient.PostAsync(_baseUrl + endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    Exiled.API.Features.Log.Warn($"SCP-1356 Status API error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Exiled.API.Features.Log.Error($"SCP-1356 Status API request failed: {ex}");
            }
        }
    }
}