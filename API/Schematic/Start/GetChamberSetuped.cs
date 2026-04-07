using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using MEC;
using ProjectMER.Events.Arguments;
using ProjectMER.Features.Objects;
using SCP1356Main.Configs;
using ProjectMER.Events.Arguments;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using SCP1356Main.API.Extensions;
using SCP1356Main.API.Schematic.HealthObject;
using UnityEngine;
using Camera = Exiled.API.Features.Camera;
using Door = Exiled.API.Features.Doors.Door;
using Generator = Exiled.API.Features.Generator;
using LightSourceToy = AdminToys.LightSourceToy;
using Map = Exiled.API.Features.Map;
using Projectile = Exiled.API.Features.Pickups.Projectiles.Projectile;
using Room = Exiled.API.Features.Room;

namespace SCP1356Main.API.Schematic.Start
{
    public class GetChamberSetuped
    {
        private Config _config = Plugin.Singleton.Config;
        private Translation _translation = Plugin.Singleton.Translation;
        private Plugin _plugin = Plugin.Singleton;
        public List<Door> SCP1356ChamberDoors = new List<Door>();
        public List<AdminToys.LightSourceToy> SCP1356ChamberLights = new();
        public List<Camera> SCP1356ChamberCams = new List<Camera>();
        private List<Light> SecondLights = new  List<Light>();
        public Elevator Elevator { get; set; }
        public Animator LeverAnimator { get; set; }
        private Door ChamberDoor { get; set; }
        public SchematicObject SCP1356Chamber { get; set; }
        public IReadOnlyCollection<RoomLightController> RoomLightControllers { get; set; }
        public void SubEvents()
        {
            Elevator = new Elevator();
            Elevator.SubEvents();
            Exiled.Events.Handlers.Map.Generated += OnGenerated;
            Exiled.Events.Handlers.Server.RoundStarted += OnRound;
            Exiled.Events.Handlers.Player.PickingUpItem += ButtonIntract;
            LabApi.Events.Handlers.PlayerEvents.ThrewProjectile += OnProjectileExploding;
            ProjectMER.Events.Handlers.Schematic.SchematicSpawned += OnSchemeSpawned;
        }

        public void UnsubEvents()
        {
            Elevator.UnsubEvents();
            Elevator = null;
            Exiled.Events.Handlers.Map.Generated -= OnGenerated;
            LabApi.Events.Handlers.PlayerEvents.ThrewProjectile -= OnProjectileExploding;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRound;
            Exiled.Events.Handlers.Player.PickingUpItem -= ButtonIntract;
            ProjectMER.Events.Handlers.Schematic.SchematicSpawned -= OnSchemeSpawned;
        }

        private void ButtonIntract(PickingUpItemEventArgs ev)
        {
            if (ev.Pickup.Base.name.Contains("1356DoorClose") &&LeverAnimator != null)
            {
                ev.IsAllowed = false;
                LeverAnimator.Play("LeverToggleOff");
                ChamberDoor.IsOpen = false;
            }
            if (ev.Pickup.Base.name.Contains("1356DoorOpen")&& LeverAnimator != null)
            {
                ev.IsAllowed = false;
                LeverAnimator.Play("LeverToggleOn");
                ChamberDoor.IsOpen = true;

            }
        }
        
        private void OnGenerated()
        {
          //  Log.Debug(Room.Get(RoomType.HczTestRoom).Doors.Count);
            SCP1356Chamber = RoomReplacer.ReplaceRoom(_config.SCP1356ChambersRoomType, _config.SCP1356ChamberName, _config.SCP1356ChambersPos, _config.SCP1356ChambersRot);
            
        }

        private HashSet<Collider> _chamberColliders;

        private void CacheChamberColliders()
        {
            _chamberColliders = SCP1356Chamber
                .GetComponentsInChildren<Collider>(true)
                .ToHashSet();
        }

        private void OnProjectileExploding(PlayerThrewProjectileEventArgs ev)
        {
            if (ev.ThrowableItem.Type != ItemType.SCP2176)
                return;

            if (_chamberColliders == null || _chamberColliders.Count == 0)
                CacheChamberColliders();

            Vector3 position = ev.Projectile.Position;
            Collider[] hits = Physics.OverlapSphere(position, 3f);

            bool hitChamber = false;

            foreach (Collider hit in hits)
            {
                Log.Debug(hit);
                if (_chamberColliders.Contains(hit))
                {
                    hitChamber = true;
                    break;
                }
            }

            if (!hitChamber)
                return;

            foreach (var door in SCP1356ChamberDoors)
            {
                door.Lock(DoorLockType.Lockdown2176);
                door.IsOpen = false;

                Timing.CallDelayed(10f, () =>
                {
                    if (door == null) 
                        return;

                    door.IsOpen = true;
                    door.Unlock();
                });
            }
            foreach (var light in SCP1356ChamberLights)
            {
                if (light == null)
                    continue;

                var localLight = light; // 🔥 FIX
                var oldIntensity = localLight.NetworkLightIntensity;

                localLight.NetworkLightIntensity = 0f;

                Timing.CallDelayed(10f, () =>
                {
                    if (localLight == null)
                        return;

                    localLight.NetworkLightIntensity = oldIntensity;
                });
            }
            foreach (var light in SecondLights)
            {
                if (light == null)
                    continue;

                var localLight = light; // 🔥 FIX
                var oldIntensity = localLight.intensity;

                localLight.intensity = 0f;

                Timing.CallDelayed(10f, () =>
                {
                    if (localLight == null)
                        return;

                    localLight.intensity = oldIntensity;
                });
            }
        }
        

        private void OnRound()
        {
            Timing.CallDelayed(2f, () =>
            {
                ChamberDoor = Door.GetClosest(_plugin.SchematicSetup.SCP1356.Position, out var distance);
                SCP1356ChamberDoors.Remove(ChamberDoor);
                ChamberDoor.Lock(DoorLockType.Isolation);
            });
            if (Generator.List.Count <= 2)
            {
                var pos = GetWorldData(SCP1356Chamber,new Vector3(3.308f, 0.322f, -3.294f), true);
                var rot = Quaternion.Euler(GetWorldData(SCP1356Chamber,new Vector3(0, 180, 0), false));
                Log.Debug(SCP1356Chamber.Position+ " " + pos + " " + rot);
                PrefabHelper.Spawn(PrefabType.GeneratorStructure, pos, rot);
                
                Log.Debug("Spawned Generator!");
            }

          var cam=  CameraToy.Create(GetWorldData(SCP1356Chamber, new Vector3(-0.137f, 40.444f, -7.326f), true),
                Quaternion.Euler(GetWorldData(SCP1356Chamber, new Vector3(-180, -180, 180), false)));
        }
        
        public static Vector3 GetWorldData(SchematicObject s, Vector3 localPoint, bool Position)
        {
            if (Position)
            {
                Transform root = s.transform;
                Vector3 worldPos = root.TransformPoint(localPoint);

                Log.Debug("Local Pos: "+  localPoint +" World Position: " + worldPos + " Scheme: " +s.Position);
                return worldPos;
            }

            var Rot = s.transform.rotation * Quaternion.Euler(localPoint);
            
            return Rot.eulerAngles;
        }
        public static Vector3 GetLocalData(SchematicObject s, Vector3 worldPoint, bool Position)
        {
            if (Position)
            {
                return s.transform.InverseTransformPoint(worldPoint);
            }
            
            return Quaternion.Inverse(s.transform.rotation) * worldPoint;
        }
        
        public List<Light> GetLightsInRange(Vector3 position, float range)
        {
            var result = new List<Light>();
            float rangeSqr = range * range;

            var lights = UnityEngine.Object.FindObjectsOfType<Light>();

            foreach (var light in lights)
            {
                if (light == null)
                    continue;

                float distSqr = (light.transform.position - position).sqrMagnitude;

                if (distSqr <= rangeSqr)
                {
                    result.Add(light);
                }
            }
            Log.Debug("Found " + result.Count + " lights");
            return result;
        }
        
        private void OnSchemeSpawned(SchematicSpawnedEventArgs ev)
        {
            if (ev.Schematic.Name.Contains(_config.SCP1356ChamberName))
            {
                Timing.CallDelayed(1f, () =>
                
                {
                    var transforms = ev.Schematic.GetComponentsInChildren<Transform>();
                    foreach (var transform in transforms)
                    {
                        switch (transform.name)
                        {
                            case "Main Elevator":
                                Elevator.ElvMovement = transform.GetComponent<Animator>();
                                Log.Debug(transform.name);
                                break;
                            case "Car":
                                Elevator.Cabin = transform.GetComponent<Animator>();
                                Log.Debug(transform.name);
                                break;
                            case "1":
                                Elevator.DoorDownController = transform.GetComponent<Animator>();
                                Log.Debug(transform.name);
                                break;
                            case "2":
                                Elevator.DoorUpController = transform.GetComponent<Animator>();
                                Log.Debug(transform.name);
                                break;
                        }

                        if (transform.name == "PlayerTransform")
                        {
                            Elevator.PlayerTransform = transform;
                            Log.Debug("PlayerTrans");
                        }
                        
                        if (transform.name == "Lever")
                        {
                            var animator = transform.GetComponent<Animator>();

                            if (animator != null)
                            {
                                Log.Debug("Animator gefunden! Im Lever");
                                LeverAnimator = animator;
                                animator.Play("LeverToggleOff");

                            }
                            else
                            {
                                Log.Warn("Kein Animator auf Lever!  Im Transform");
                            }
                        }
                        if (transform.name.Contains("health"))
                        {
                            Log.Debug("Health gefunden!");
                            var child = transform.GetChild(0);
                            child.gameObject.AddHealth(350, SimpleDeathType.Destroy);
                        }
                    }
                });
            }
        }
    }
}