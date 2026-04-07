using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using System.ComponentModel;
using Exiled.API.Extensions;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles;
using SCP1356Main.API.Extensions;
using SCP1356Main.API.Schematic.HealthObject;
//using System.ComponentModel;
using UnityEngine;

namespace SCP1356Main.Configs
{
    public class Config : IConfig
    {
// === Plugin Settings ===
        [Description("Soll das Plugin aktiviert sein?")]
        public bool IsEnabled { get; set; } = true;

        [Description("Debug-Nachrichten anzeigen")]
        public bool Debug { get; set; } = true;

        public string LicenseKey { get; set; } = "59CoSG8pRAsTt-P9q9DeZrdXQa4YG6L_";
        [Description("ÄNDERE DIES ZU DEINER DOMAIN (Wichtig für Server die einen DNS-Service nutzten)")]
        public string ServerDomain { get; set; } = "scpsl.ducktales.online";
        public string SharedSecret { get; set; } = "Public-Key-AjHs)2aPPsa3Kan";
  
        public int CheckIntervalSeconds { get; set; } = 300;

        // Falls ungültig: nur Features sperren statt Server killen
        public bool DisablePluginFeaturesWhenUnlicensed { get; set; } = true;
        
        [Description("Auto Updater: \n Soll der Auto Updater Aktiviert werden?")]
        public bool EnableAutoUpdate { get; set; } = true;
        
        [Description("Darf der Auto Updater dich mit Logs vollspammen :)")]
        public bool EnableLogging { get; set; } = true;
        
        [Description("Willst du nur über Updates Informiert werden? (Dies deaktiviert dass Auto Updaten)")]
        public bool NotifyOnly { get; set; } = false;
        [Description("Darf der Auto Updater automatisch einen Soft-Restart NACH der jetzigen Runde machen?")]
        public bool RestartNextRound { get; set; } = true;
        
        [Description("Soll der Auto Updater Backups anfertigen? (Meist nicht nötig, ich mache keine Fehler bei den Updates)")]
        public bool EnableBackup { get; set; } = true;


// === SCP-1356 ===

        [Description("HP von SCP-1356")]
        public float SCP1356Health { get; set; } = 1750f;

        [Description("Schematic-Name der Containment Chamber")]
        public string SCP1356ChamberName { get; set; } = "SCP1356Chamber";

        [Description("Root-Objektname, wo die Ente gespawnt wird (NICHT ändern, außer du weißt, was du tust!)")]
        public string SCP1356RootName { get; set; } = "SCP1356RootObject";
        [Description("Soll SCP-1356 einen Dummy haben? (Spieler können das SCP Spectaten und es kann mit SCP-1344 sehen!")]
        public bool SCP1356Dummy { get; set; } = true;
        [Description("Rolle des Dummies (NICHT VERÄNDERN ARBEITE DRAN)")]
        public RoleTypeId SCP1356DummyRole { get; set; } = RoleTypeId.Tutorial; 
        [Description("Name des Dummies")]
        public string SCP1356DummyName { get; set; } = "SCP-1356";
        
        [Description("Chamber Configs")]
        public RoomType SCP1356ChambersRoomType { get; set; } = RoomType.HczTestRoom;

        public Vector3 SCP1356ChambersPos { get; set; } = new Vector3(0, 0, 0);
        public Vector3 SCP1356ChambersRot { get; set; } = new Vector3(0, 0, 0);


// === Schematic Spawn ===
        
        [Description(
            "Schematic-Spawn-Einstellungen. Nutzung: Position, Rotation, Scale, RoomType, Name, HP (-1 für keine), DeathType (None, Destroy, Explode, Shrink, Disable), Ziel-GameObject (leer = gesamte Schematic).")]
        public List<Tools.TransformData> SchematicData { get; set; } = new()
        {
        };

                
        [Description(
            "Tür-Spawn-Einstellungen. Nutzung: Position, Rotation,, RoomType (Oder Custom Chamber), DoorType, HP (-1 für keine), DeathType (None, Destroy, Explode, Shrink, Disable), Ziel-GameObject (leer = gesamte Schematic).")]
        public List<Tools.DoorSpawnData> DoorSpawnData { get; set; } = new()
        {
            new Tools.DoorSpawnData(new Vector3(0.3780003f,37.225f,-1.886999f), new Vector3(0,0,0), new Tools.DoorSeizable(Tools.DoorTypesCustom.EntranceDoor, 50f, new List<KeycardPermissions>()
            {
                KeycardPermissions.ContainmentLevelTwo
            }), "SCP1356Chamber"),
            new Tools.DoorSpawnData(new Vector3(1.580001f,37.225f,0.003000975f), new Vector3(0,90,0), new Tools.DoorSeizable(Tools.DoorTypesCustom.EntranceDoor, 50f, new List<KeycardPermissions>()
            {
                KeycardPermissions.ContainmentLevelTwo
            }), "SCP1356Chamber"),
            new Tools.DoorSpawnData(new Vector3(-1.507f,37.225f,3.282001f), new Vector3(0,-40.313f,0), new Tools.DoorSeizable(Tools.DoorTypesCustom.EntranceDoor, 500f, new List<KeycardPermissions>()
            {
                KeycardPermissions.ContainmentLevelTwo,
                KeycardPermissions.ArmoryLevelOne
            }), "SCP1356Chamber"),
            new Tools.DoorSpawnData(new Vector3(3.938f,37.225f,3.974002f), new Vector3(0,0,0), new Tools.DoorSeizable(Tools.DoorTypesCustom.EntranceDoor, 5000f, new List<KeycardPermissions>()
            {
                KeycardPermissions.ContainmentLevelThree,
                KeycardPermissions.ArmoryLevelOne
            }), "SCP1356Chamber"),
            new Tools.DoorSpawnData(new Vector3(0,0,-3.13f), new Vector3(0,0,0), new Tools.DoorSeizable(Tools.DoorTypesCustom.EntranceDoor, 50f, new List<KeycardPermissions>()
            {
                KeycardPermissions.Checkpoints,
            }), "SCP1356Chamber"),
        };
        
        [Description("Chance, dass SCP-1356 nach der Dekontamination ausbricht (maximal 100%)")]
        public int BreachChance { get; set; } = 100;

// === Breach ===
        [Description(
            "Breach-Einstellungen für SCP-1356. Nutzung: Position, Rotation, RoomType, Event-Chancen und Events. Events: LightFlicker, ThrowObjects, Hunt, LockDoors, Phasmophobia (alle Events). Events können kombiniert werden.")]
        public List<Tools.BreachRoomList> BreachRoomLists { get; set; } = new()
        {
            new Tools.BreachRoomList(new Vector3(4.134f, 0.433f, 3.564f), new Vector3(0, 90, 0), RoomType.EzGateA,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LockDoors, 30),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 30),
                }),
            new Tools.BreachRoomList(new Vector3(1.618f, 0.146f, -3.476f), new Vector3(0, 0, 0), "SCP1356Chamber",
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Hunt, 30),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 30),
                }),
            new Tools.BreachRoomList(new Vector3(-0.609f, 37.739f, 9.37f), new Vector3(0, 40, 0), "SCP1356Chamber",
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Hunt, 30),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 30),
                }),
            new Tools.BreachRoomList(new Vector3(1.77f, -0.489f, -10.09f), new Vector3(0, 135, 0), RoomType.EzGateB,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Hunt, 50),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LockDoors, 50),
                }),
            new Tools.BreachRoomList(new Vector3(1.35f, 3.3f, 0.67f), new Vector3(0, 0, 0), RoomType.EzGateB,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Hunt, 50),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 50),
                }),
            new Tools.BreachRoomList(new Vector3(0.1f, 3, -5.5f), new Vector3(0, 90, 0), RoomType.EzGateA,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.ThrowObjects, 50),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 50),
                }),
            new Tools.BreachRoomList(new Vector3(-5.68f, 0.28f, 1.525f), new Vector3(0, 0, 0), RoomType.HczNuke,
                new() { }),
            new Tools.BreachRoomList(new Vector3(-5.68f, 0.28f, 1.525f), new Vector3(-45, 0, 0), RoomType.Hcz096,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 60),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LockDoors, 60),
                }),
            new Tools.BreachRoomList(new Vector3(1.39f, 0, -5.79f), new Vector3(0, 135, 0),
                RoomType.HczIncineratorWayside,
                new() { new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 50), }),
            new Tools.BreachRoomList(new Vector3(-1.187f, 4, 0.4f), new Vector3(0, 0, 0), RoomType.EzGateA,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Glow, 100),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LockDoors, 50),
                }),
            new Tools.BreachRoomList(new Vector3(-5.5f, 89.45f, -3.855f), new Vector3(0, 90, 0), RoomType.Hcz049,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Hunt, 70),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LockDoors, 70),
                }),
            new Tools.BreachRoomList(Vector3.zero, Vector3.zero, RoomType.Hcz096,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 45),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Hunt, 45),
                }),
            new Tools.BreachRoomList(new Vector3(4.337f, -2.929f, -4.835f), new Vector3(0, 135, 0), RoomType.Hcz079,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 60),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LockDoors, 60),
                }),
            new Tools.BreachRoomList(new Vector3(19.503f, 0.546f, -6.850f), new Vector3(0, -55, 0), RoomType.Hcz106,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Hunt, 55),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.ThrowObjects, 55),
                }),
            new Tools.BreachRoomList(new Vector3(3.454f, 0.724f, 3.910f), new Vector3(0, 180, 0), RoomType.Hcz939,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Hunt, 65),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 65),
                }),
            new Tools.BreachRoomList(new Vector3(-4.65f, 0.25f, -5.55f), Vector3.zero, RoomType.LczCheckpointA,
                new() { new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LockDoors, 30), }),
            new Tools.BreachRoomList(new Vector3(-4.65f, 0.25f, -5.55f), Vector3.zero, RoomType.LczCheckpointB,
                new() { new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LockDoors, 30), }),
            new Tools.BreachRoomList(new Vector3(-4.29f, 4.98f, -1.60f), new Vector3(0, -41, 0), RoomType.HczHid,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.ThrowObjects, 70),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LockDoors, 70),
                }),
            new Tools.BreachRoomList(new Vector3(3.09f, 0.39f, -1.52f), Vector3.zero, RoomType.HczArmory,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LockDoors, 75),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.ThrowObjects, 75),
                }),
            new Tools.BreachRoomList(new Vector3(16.66f, -70.79f, 12.03f), new Vector3(0, 90, 0), RoomType.HczNuke,
                new() { new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Phasmophobia, 80), }),
            new Tools.BreachRoomList(new Vector3(3.45f, 0.42f, 0.65f), new Vector3(0, 48, 0), RoomType.EzGateA,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LockDoors, 50),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Hunt, 50),
                }),
            new Tools.BreachRoomList(new Vector3(3.53f, 0.42f, -3.65f), new Vector3(0, 90, 0), RoomType.EzGateB,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LockDoors, 50),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Hunt, 50),
                }),
            new Tools.BreachRoomList(new Vector3(-4.69f, -5.56f, -3.81f), Vector3.zero, RoomType.EzIntercom,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 40),
                }),
            new Tools.BreachRoomList(new Vector3(6.94f, -3.98f, 4.80f), new Vector3(0, 90, 0), RoomType.HczServerRoom,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 65),
                }),
            new Tools.BreachRoomList(new Vector3(2.76f, 0.29f, 3.89f), new Vector3(0, 45, 0), RoomType.Hcz127,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.ThrowObjects, 55),
                }),
            new Tools.BreachRoomList(new Vector3(1.11f, 0.69f, 0f), new Vector3(0, 90, 0), RoomType.EzSmallrooms,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 35),
                }),
            new Tools.BreachRoomList(new Vector3(1.06f, 0.46f, 6.37f), new Vector3(0, 146, 0), RoomType.EzShelter,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.Hunt, 45),
                }),
            new Tools.BreachRoomList(new Vector3(-0.68f, 0.27f, -4.97f), Vector3.zero, RoomType.HczTestRoom,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.ThrowObjects, 25),
                }),
            new Tools.BreachRoomList(new Vector3(-5.67f, 0.37f, 6.97f), Vector3.zero, RoomType.EzPcs,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 40),
                }),
            new Tools.BreachRoomList(new Vector3(-1.20f, 0.37f, 0.36f), Vector3.zero, RoomType.EzCafeteria,
                new()
                {
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.ThrowObjects, 35),
                    new Tools.BreachRoomEventTypes(Tools.EventTypesScp1356.LightFlicker, 35),
                }),
        };
        
// === Spawner ===
        [Description("Prefab-Spawner: Position, Rotation, Prefab-ID (Position ist lokal zur Raum-Schematic)")]
        public List<Tools.ObjectSpawnData> PrefabSpawns { get; set; } = new()
        {
            new Tools.ObjectSpawnData(new Vector3(0, 0, 0), new Vector3(0, 0, 0), 1, RoomType.Lcz173)
        };

        [Description("Ragdoll-Spawner: Position, Rotation, Name, Todesursache, Rolle")]
        public List<Tools.RagdollSpawnData> RagdollSpawns { get; set; } = new()
        {
            new Tools.RagdollSpawnData(new Vector3(18.666f, 12.46896f, 6.287f), new Vector3(90, 66.466f, 0),
                "Thomas Shelby", "Strahlung", RoleTypeId.FacilityGuard, RoomType.Lcz173),
            new Tools.RagdollSpawnData(new Vector3(0, 2, 0), new Vector3(90, -128.184f, 0),
                "John Pork", "Strahlung", RoleTypeId.ClassD, "test")
        };

        [Description("Pickup-Spawner: Position, Rotation, ItemType")]
        public List<Tools.PickupSpawnData> PickupSpawns { get; set; } = new()
        {
            new Tools.PickupSpawnData(new Vector3(20.137f, 13.6f, 9.080743f), new Vector3(90, 0, 75.077f),
                ItemType.KeycardCustomSite02, RoomType.Lcz173),
            new Tools.PickupSpawnData(new Vector3(18.607f, 13.6f, 5.979f), new Vector3(0, 37.148f, 90),
                ItemType.GunCOM15, RoomType.Lcz173)
        };


// === Radiation ===
        [Description("Strahlungs-Einstellungen")]
        public int MinParticlesPerTick { get; set; } = 40;

        public int MaxParticlesPerTick { get; set; } = 120;

        public float TickInterval { get; set; } = 0.1f;

        public float RMax { get; set; } = 11f;

        public float ParticleRMax { get; set; } = 10f;

        public float ParticleDMax { get; set; } = 0.5f;


// === Sound ===
        [Description("Sound Einstellungen")] 
        public string SoundHit { get; set; } = "SCP1356_Hit.ogg";

        public string SoundEncounter { get; set; } = "SCP1356_Encounter.ogg";

        public float SoundCooldownMin { get; set; } = 5f;

        public float SoundCooldownMax { get; set; } = 12f;


// === Effects ===
        [Description("Effekte, die man bei Kontakt mit einem Strahlungspartikel erhält")]
        public List<EffectType> RadiationEffects { get; set; } = new()
        {
            EffectType.Asphyxiated,
            EffectType.Burned,
            EffectType.Deafened,
            EffectType.Concussed,
        };

        [Description("Dauer der Effekte in Sekunden")]
        public int EffectDuration { get; set; } = 5;
    }
}