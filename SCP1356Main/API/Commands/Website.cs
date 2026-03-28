using Exiled.API.Features;
using MEC;
using Newtonsoft.Json;
using ProjectMER.Features.Objects;
using SCP1356Main.API.Schematic.HealthObject;
using SCP1356Main.Configs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using UnityEngine;

#nullable disable
namespace SCP1356Main.API.Commands
{
  public class Website
  {
    private CoroutineHandle _coroutine;
    private HttpClient _client;
    private int _lastHealth = -1;
    private float _lastMaxHealth = -1f;
    private string _lastRoom = string.Empty;
    private float _lastSendTime = 0.0f;

    public void SubEvents()
    {
      this._client = new HttpClient();
      this._client.DefaultRequestHeaders.Clear();
      this._client.DefaultRequestHeaders.Add("Authorization", "SECRET_KEY");
      this._coroutine = Timing.RunCoroutine(this.UpdateLoop());
    }

    public void UnSubEvents()
    {
      Timing.KillCoroutines(new CoroutineHandle[1]
      {
        this._coroutine
      });
      this._client?.Dispose();
      this._client = (HttpClient)null;
    }

    private IEnumerator<float> UpdateLoop()
    {
      while (true)
      {
        this.UpdateDataSafe();
        yield return Timing.WaitForSeconds(2f);
      }
    }

    private void UpdateDataSafe()
    {
      try
      {
        SchematicObject scP1356 = Plugin.Singleton.SchematicSetup.SCP1356;
        if (scP1356 == null)
          return;
        HealthComponent componentInChildren = ((Component)scP1356).GetComponentInChildren<HealthComponent>();
        float num = componentInChildren != null ? componentInChildren.Health : 0.0f;
        float scP1356Health = ((Plugin<Config>)Plugin.Singleton).Config.SCP1356Health;
        Plugin singleton = Plugin.Singleton;
        string room = singleton.Translation.RoomTypesCustomLanguage[singleton.BreachAPI.CurrentRoom] ?? "UNKNOWN";
        int health = Mathf.RoundToInt(num);
        if (this._lastHealth == health &&
            (double)Math.Abs(this._lastMaxHealth - scP1356Health) <= 0.009999999776482582 &&
            !(this._lastRoom != room) && (double)Time.time - (double)this._lastSendTime <= 30.0)
          return;
        this._lastHealth = health;
        this._lastMaxHealth = scP1356Health;
        this._lastRoom = room;
        this._lastSendTime = Time.time;
        this.SendData(health, scP1356Health, room);
      }
      catch (Exception ex)
      {
        Log.Error($"Website-Fehler: {ex}");
      }
    }

    private async void SendData(int health, float maxHealth, string room)
    {
      try
      {
        var data = new
        {
          health = health,
          maxHealth = maxHealth,
          location = room,
        };
        string json = JsonConvert.SerializeObject((object)data);
        StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage httpResponseMessage =
          await this._client.PostAsync("http://m26g23tvsv4vpvyy.myfritz.net:3000/update", (HttpContent)content);
        data = null;
        json = (string)null;
        content = (StringContent)null;
      }
      catch (Exception ex)
      {
        Log.Error($"HTTP-Fehler: {ex}");
      }
    }
  }
}
