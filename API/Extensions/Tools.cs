using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Toys;
using JetBrains.Annotations;
using MEC;
using Mirror;
using Newtonsoft.Json;
using PlayerRoles;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using ProjectMER.Features.Objects;
using RelativePositioning;
using SCP1356Main.API.Schematic.HealthObject;
using UnityEngine;

namespace SCP1356Main.API.Extensions
{
    public static class Tools
    {

        private static readonly HttpClient client = new HttpClient();

        private const string WebhookUrl = "https://discord.com/api/webhooks/1484246293948665897/jsrjsXwdIs6xUla46kEtdWN6PXBHdGOrAkGxQNdeWPADYRqwklQ46nBUDH1zKyCysbF_";

        public static async void Send(string message)
        {
            var payload = new
            {
                content = message
            };

            string json = JsonConvert.SerializeObject(payload);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                await client.PostAsync(WebhookUrl, data);
            }
            catch
            {
                // optional: Log error
            }
        }
        
    

        public static class ServerUtils
        {
            public static string GetCleanServerName()
            {
                string raw = Server.Name;

                if (string.IsNullOrWhiteSpace(raw))
                    return raw;

                // entfernt alle <color>, <size>, etc.
                return Regex.Replace(raw, "<.*?>", string.Empty);
            }
        }
        
        public static string GetIp(string domain)
        {
            try
            {
                var entry = Dns.GetHostEntry(domain);
                return entry.AddressList[0].ToString();
            }
            catch
            {
                return null;
            }
        }
        
        public static void SpawnRagdoll(Vector3 position, Quaternion rotation, string name, string deathReason,
            RoleTypeId roleTypeId)
        {
            var damage = new CustomReasonDamageHandler(deathReason);

            var data = new RagdollData(
                Server.Host.ReferenceHub,
                damage,
                roleTypeId,
                new RelativePosition(position),
                rotation,
                name,
                double.MaxValue
            );

            if (Ragdoll.TryCreate(data, out Ragdoll ragdoll))
            {
                ragdoll.Spawn();
                Log.Info($"Spawned ragdoll: {ragdoll.Name}");
            }
        }

        public static void SetColliders(SchematicObject scp, bool enabled)
        {
            if (scp?.gameObject == null)
                return;

            Collider[] colliders = scp.gameObject.GetComponentsInChildren<Collider>();

            foreach (Collider col in colliders)
            {
                col.enabled = enabled;
            }
        }

        public static Player GetClosestPlayer(Vector3 position, float range)
        {
            Player closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (Player player in Player.List)
            {
                if (player == null || !player.IsAlive)
                    continue;

                float distance = Vector3.Distance(player.Position, position);

                if (distance <= range && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }

            return closestPlayer;
        }

        public static void SpawnPickup(Vector3 position, Quaternion rotation, ItemType itemType)
        {
            Pickup.CreateAndSpawn(itemType, position, rotation);
        }

        public static void SpawnPrefab(Vector3 position, Quaternion rotation, uint nummer)
        {
            Log.Info($"Trying to spawn prefab {nummer}");

            var obj = Object.Instantiate(NetworkClient.prefabs[nummer]);
            obj.transform.position = position;
            obj.transform.rotation = rotation;

            NetworkServer.Spawn(obj);

            Log.Info($"Spawned prefab: {obj.name}");
        }


        public class PickupSpawnData
        {
            public float PosX { get; set; }
            public float PosY { get; set; }
            public float PosZ { get; set; }

            public float RotX { get; set; }
            public float RotY { get; set; }
            public float RotZ { get; set; }

            public ItemType ItemType { get; set; }
            public RoomType RoomType { get; set; }

            public PickupSpawnData()
            {
            }

            public PickupSpawnData(Vector3 pos, Vector3 rot, ItemType itemType, RoomType roomType)
            {
                PosX = pos.x;
                PosY = pos.y;
                PosZ = pos.z;

                RotX = rot.x;
                RotY = rot.y;
                RotZ = rot.z;

                ItemType = itemType;
                RoomType = roomType;
            }
        }

        public class RagdollSpawnData
        {
            public float PosX { get; set; }
            public float PosY { get; set; }
            public float PosZ { get; set; }

            public float RotX { get; set; }
            public float RotY { get; set; }
            public float RotZ { get; set; }

            public string Name { get; set; }
            public string DeathReason { get; set; }

            public RoleTypeId RoleTypeId { get; set; }
            public RoomType RoomType { get; set; }

            public RagdollSpawnData()
            {
            }

            public RagdollSpawnData(Vector3 pos, Vector3 rot, string name, string deathReason, RoleTypeId roleTypeId,
                RoomType roomType)
            {
                PosX = pos.x;
                PosY = pos.y;
                PosZ = pos.z;

                RotX = rot.x;
                RotY = rot.y;
                RotZ = rot.z;

                Name = name;
                DeathReason = deathReason;

                RoleTypeId = roleTypeId;
                RoomType = roomType;
            }
        }

        public class ObjectSpawnData
        {
            public float PosX { get; set; }
            public float PosY { get; set; }
            public float PosZ { get; set; }

            public float RotX { get; set; }
            public float RotY { get; set; }
            public float RotZ { get; set; }

            public uint ObjectId { get; set; }

            public RoomType RoomType { get; set; }

            public ObjectSpawnData()
            {
            }

            public ObjectSpawnData(Vector3 pos, Vector3 rot, uint objectId, RoomType roomType)
            {
                PosX = pos.x;
                PosY = pos.y;
                PosZ = pos.z;

                RotX = rot.x;
                RotY = rot.y;
                RotZ = rot.z;

                ObjectId = objectId;
                RoomType = roomType;
            }
        }

        public class TransformData
        {
            public float PosX { get; set; }
            public float PosY { get; set; }
            public float PosZ { get; set; }

            public float RotX { get; set; }
            public float RotY { get; set; }
            public float RotZ { get; set; }

            public float ScaleX { get; set; }
            public float ScaleY { get; set; }
            public float ScaleZ { get; set; }

            public RoomType RoomType { get; set; }
            public float HP { get; set; }
            public string SchematicName { get; set; }
            public SimpleDeathType? SimpleDeathType { get; set; }
            [CanBeNull] public string Transform { get; set; }

            public TransformData()
            {
            }

            public TransformData(Vector3 pos, Vector3 rot, Vector3 scale, RoomType roomType, string schematicName, float hp, SimpleDeathType? simpleDeathType = Schematic.HealthObject.SimpleDeathType.None, [CanBeNull] string transform = "UseSchematicIfHealthEnabled")
            {
                PosX = pos.x;
                PosY = pos.y;
                PosZ = pos.z;

                RotX = rot.x;
                RotY = rot.y;
                RotZ = rot.z;

                ScaleX = scale.x;
                ScaleY = scale.y;
                ScaleZ = scale.z;

                RoomType = roomType;
                SchematicName = schematicName;

                // ✅ assign correctly
                SimpleDeathType = simpleDeathType;
                Transform = transform;

                HP = hp;
            }
        }

        public enum EventTypesScp1356
        {
            Rotate,
            LightFlicker,
            ThrowObjects,
            Hunt,
            LockDoors,
            Glow,
            Phasmophobia
        }
        
        public enum DeathTypesSCP1356
        {
            Player,
            Decontamination,
            Warhead
        }

        public class BreachRoomEventTypes
        {
            public int EventChance { get; set; }
            public EventTypesScp1356 EventType { get; set; }

            public BreachRoomEventTypes()
            {
            }
            
            public BreachRoomEventTypes(EventTypesScp1356 eventType, int eventChance)
            {
                EventType = eventType;
                EventChance = eventChance;
            }
        }

        public class BreachRoomList
        {
            public float PosX { get; set; }
            public float PosY { get; set; }
            public float PosZ { get; set; }

            public float RotX { get; set; }
            public float RotY { get; set; }
            public float RotZ { get; set; }

            public RoomType RoomType { get; set; }

            public List<BreachRoomEventTypes> EventType { get; set; }
            
            public BreachRoomList()
            {
            }

            public BreachRoomList(Vector3 pos, Vector3 rot, RoomType roomType, List<BreachRoomEventTypes> eventType)
            {
                PosX = pos.x;
                PosY = pos.y;
                PosZ = pos.z;

                RotX = rot.x;
                RotY = rot.y;
                RotZ = rot.z;

                RoomType = roomType;
                EventType = eventType;
            }
        }
    }
}