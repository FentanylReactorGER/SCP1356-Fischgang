using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Toys;
using Exiled.Events.EventArgs.Map;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.HumeShield;
using ProjectMER.Features.Objects;
using SCP1356Main.API.Extensions;
using SCP1356Main.Configs;
using UnityEngine;
using Light = Exiled.API.Features.Toys.Light;

namespace SCP1356Main.API.Breach
{
    public class BreachAPI
    {
        private static readonly Config Config = Plugin.Singleton.Config;
        private static readonly Translation Translation = Plugin.Singleton.Translation;
        private readonly HashSet<(int playerId, RoomType roomType)> _usedPairs = new();
        private CoroutineHandle _breachCoroutine;
        private bool _breachRunning;
        

        private Player _lastPlayer;
        private RoomType _lastRoom;
        public RoomType CurrentRoom { get; set; } 

        public void SubEvents()
        {
            Exiled.Events.Handlers.Map.Decontaminating += TryBreach;
            Exiled.Events.Handlers.Warhead.Detonated += SCP1356Warhead;
        }

        public void UnsubEvents()
        {
            Exiled.Events.Handlers.Map.Decontaminating -= TryBreach;
            Exiled.Events.Handlers.Warhead.Detonated -= SCP1356Warhead;
        }

        private void SCP1356Warhead()
        {
            if (!Plugin.Singleton.Detector.SCP1356Contained && CurrentRoom != RoomType.Surface)
            {
                Exiled.API.Features.Cassie.MessageTranslated(
                    Translation.SCP1356CassieMessageWarhead,
                    Translation.SCP1356CassieMessageWrheadTranslated
                );
                return;
            }
            else if (CurrentRoom == RoomType.Surface)
            {
                foreach (var r in Config.BreachRoomLists)
                {
                    if (r.RoomType != RoomType.Surface)
                    {
                        Config.BreachRoomLists.Remove(r);
                        Log.Debug("Removing all Non-Surface Spawn...");
                    }
                }
                return;
            }
            
        }
        
        private void TryBreach(DecontaminatingEventArgs ev)
        {
            if (_breachRunning)
                return;

            if (Config.BreachChance > 100)
            {
                Log.Warn("Breach Chance ist über 100%, überarbeite deine Config!");
                return;
            }

            if (UnityEngine.Random.Range(0, 100) > Config.BreachChance)
            {
                Exiled.API.Features.Cassie.MessageTranslated(
                    Translation.SCP1356CassieMessageContainDecon,
                    Translation.SCP1356CassieMessageContainDeconTranslation
                );
                return;
            }
            

            Log.Info("Starting SCP-1356 breach.");
            _breachRunning = true;

            Timing.CallDelayed(30f, () =>
            {
                Exiled.API.Features.Cassie.MessageTranslated(
                    Translation.SCP1356CassieMessageBreach,
                    Translation.SCP1356CassieMessageTranslatedBreach
                );
                var ray = Plugin.Singleton.SchematicSetup.radiation;
                if (!Plugin.Singleton.Detector.SCP1356ChamberOpen)
                {
                    ray.SetRadiationSettings(ray._rMax * 2, ray._maxParticlesPerTick * 2);
                }
                _breachCoroutine = Timing.RunCoroutine(
                    SCP1356Breach(Config.BreachRoomLists, Plugin.Singleton.SchematicSetup.SCP1356)
                );
            });
        }
        
        public Tools.BreachRoomList GetBestRoom(List<Tools.BreachRoomList> rooms, float maxRange = 20f)
        {
            if (rooms == null || rooms.Count == 0)
                return null;

            Tools.BreachRoomList bestRoom = null;
            float bestDistance = float.MaxValue;

            foreach (var player in Player.List)
            {
                if (player == null || !player.IsAlive)
                    continue;

                foreach (var room in rooms)
                {
                    Room r = Room.Get(room.RoomType);
                    if (r == null)
                        continue;

                    Vector3 roomPos = r.Transform.TransformPoint(
                        new Vector3(room.PosX, room.PosY, room.PosZ));

                    float distance = Vector3.Distance(player.Position, roomPos);

                    // ✅ Only consider if player is "in range" of that room
                    if (distance > maxRange)
                        continue;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestRoom = room;
                    }
                }
            }

            // ❗ If no player is near ANY room → random fallback
            if (bestRoom == null)
            {
                bestRoom = rooms[UnityEngine.Random.Range(0, rooms.Count)];
            }

            return bestRoom;
        }
        private bool DelaySwap { get; set; } = false;
        public Tools.BreachRoomList SelectedForce { get; set; } 
        private  Tools.BreachRoomList selected { get; set; } 
        public IEnumerator<float> SCP1356Breach(List<Tools.BreachRoomList> roomLists, SchematicObject scp)
        {
            if (roomLists == null || roomLists.Count == 0 || scp == null)
                yield break;

            while (!Round.IsEnded && !Plugin.Singleton.Detector.SCP1356Contained)
            {
                if (SelectedForce == null)
                {
                    selected = GetBestRoom(roomLists);
                    Log.Debug($"SCP-1356 breach Selected Force Room: {selected.RoomType}");
                    SelectedForce = null;
                }
                else if (SelectedForce != null)
                {
                    selected = SelectedForce;
                }

                if (selected == null || DelaySwap)
                {
                    Log.Debug($"SCP-1356 breach Delayed by 5 Seconds, selected might be null or DelaySwap bool: {DelaySwap} might be true?");
                    yield return Timing.WaitForSeconds(5f);
                    continue;
                }

                Room room = Room.Get(selected.RoomType);
                if (room == null)
                {
                    yield return Timing.WaitForSeconds(5f);
                    continue;
                }
                CurrentRoom = selected.RoomType;
                Vector3 pos = room.Transform.TransformPoint(
                    new Vector3(selected.PosX, selected.PosY, selected.PosZ));

                Quaternion rot = room.Transform.rotation *
                                 Quaternion.Euler(selected.RotX, selected.RotY, selected.RotZ);

                // Move SCP
                scp.Position = pos;
                scp.Rotation = rot;

                Log.Debug($"SCP moved to {selected.RoomType}");

                if (selected.EventType != null && selected.EventType.Count > 0)
                {
                    yield return Timing.WaitForSeconds(UnityEngine.Random.Range(0f, 15f));

                    // 1. Execute all 100% events
                    foreach (var ev in selected.EventType)
                    {
                        if (ev.EventChance >= 100)
                        {
                            HandleEvent(ev.EventType, room, scp);
                        }
                    }

                    // 2. Filter out 100% events
                    var remainingEvents = selected.EventType
                        .Where(e => e.EventChance < 100)
                        .ToList();

                    if (remainingEvents.Count == 0)
                    {
                        yield return Timing.WaitForSeconds(20f);
                        continue;
                    }

                    // 3. Weighted random from remaining
                    int totalWeight = remainingEvents.Sum(e => e.EventChance);

                    if (totalWeight <= 0)
                        yield break;

                    int roll = UnityEngine.Random.Range(0, totalWeight);
                    int current = 0;

                    foreach (var ev in remainingEvents)
                    {
                        current += ev.EventChance;

                        if (roll < current)
                        {
                            HandleEvent(ev.EventType, room, scp);
                            break;
                        }
                    }
                }

                yield return Timing.WaitForSeconds(UnityEngine.Random.Range(20f, 40f));
            }

            Log.Info("SCP-1356 breach ended.");
            _breachRunning = false;
        }

        private void HandleEvent(Tools.EventTypesScp1356 ev, Room room, SchematicObject scp)
        {
            switch (ev)
            {
                case Tools.EventTypesScp1356.LightFlicker:
                    Log.Debug($"[HandleSCP1356Events] Event: {ev.ToString()} at: {room.Name}");
                    Timing.RunCoroutine(FlickerCoroutine(5f, room));;
                    break;
                
                case Tools.EventTypesScp1356.LockDoors:
                    Log.Debug($"[HandleSCP1356Events] Event: {ev.ToString()} at: {room.Name}");
                    LockDoors(room, 15f);
                    break;

                case Tools.EventTypesScp1356.ThrowObjects:
                    Log.Debug($"[HandleSCP1356Events] Event: {ev.ToString()} at: {room.Name}");
                    ThrowObjects(scp.Position, 15f);
                    break;
                
                case Tools.EventTypesScp1356.Hunt:
                    Hunt(scp, 5f, 30f, room);
                    break;

                case Tools.EventTypesScp1356.Rotate:
                    Timing.RunCoroutine(RotateCoroutine(scp, 30f));
                    break;
                case Tools.EventTypesScp1356.Glow:
                    var lightSource = Light.Create(scp.Position);
                    lightSource.Color = new Color(0.2f, 1f, 0.35f);
                    lightSource.Intensity = 0.2f;
                    lightSource.Range = 1.5f;

                    Timing.RunCoroutine(GlowCoroutine(scp, lightSource, 15f));
                    break;
                default:
                    Log.Warn($"Unhandled SCP1356 event: {ev}");
                    break;
            }
        }

        private IEnumerator<float> GlowCoroutine(SchematicObject scp, Light lightSource, float duration)
        {
            float elapsed = 0f;

            const float startIntensity = 0.2f;
            const float endIntensity = 3.5f;

            const float startRange = 1.5f;
            const float endRange = 6f;

            Color startColor = new Color(0.15f, 0.6f, 0.2f);
            Color endColor = new Color(0.45f, 1f, 0.55f);

            DelaySwap = true;

            while (elapsed < duration && scp is not null && lightSource is not null)
            {
                float t = elapsed / duration;

                // Licht am SCP halten
                lightSource.Position = scp.Position + Vector3.up * 1.0f;

                // von schwach zu stark
                lightSource.Intensity = Mathf.Lerp(startIntensity, endIntensity, t);
                lightSource.Range = Mathf.Lerp(startRange, endRange, t);

                // leicht stärker/grüner werden
                lightSource.Color = Color.Lerp(startColor, endColor, t);

                elapsed += Time.deltaTime;
                yield return Timing.WaitForOneFrame;
            }

            if (lightSource is not null)
            {
                lightSource.Destroy();
                DelaySwap  = false;
            }
        }
        
        private void Hunt(SchematicObject scp, float dur, float range, Room room)
        {
            Player plyr = Player.List
                .Where(p => p.IsAlive && p.CurrentRoom == room)
                .OrderBy(p => Vector3.Distance(p.Position, room.Position))
                .FirstOrDefault();

            if (plyr == null)
            {
                Log.Debug($"SCP-1356 Hunt no players found");
                return;
            }

            DelaySwap = true;

            Vector3 scpPosOld = scp.Position;
            Quaternion scpRotOld = scp.Rotation;
            
            Tools.SetColliders(scp, false);

            // teleport instantly
            scp.Position = plyr.Position;

            // boost effects
            Plugin.Singleton.SchematicSetup.radiation._maxParticlesPerTick *= 2;

            LockDoors(room, dur);
            Timing.RunCoroutine(FlickerCoroutine(dur, room));
            ThrowObjects(scp.Position, range);
            
            Timing.RunCoroutine(FollowPlayerCoroutine(scp, plyr, dur));

            // restore after duration
            Timing.CallDelayed(dur, () =>
            {
                scp.Position = scpPosOld;
                scp.Rotation = scpRotOld;

                Plugin.Singleton.SchematicSetup.radiation._maxParticlesPerTick /= 2;
                Plugin.Singleton.SchematicSetup.radiation._rMax /= 2;

                Tools.SetColliders(scp, true);
                DelaySwap = false;
            });
        }
        
        private IEnumerator<float> RotateCoroutine(SchematicObject scp, float duration)
        {
            float elapsed = 0f;

            // random rotation speeds for each axis (feels more natural)
            float speedX = UnityEngine.Random.Range(20f, 60f);
            float speedY = UnityEngine.Random.Range(20f, 60f);
            float speedZ = UnityEngine.Random.Range(20f, 60f);

            while (elapsed < duration)
            {
                if (scp == null)
                    yield break;

                // apply rotation
                Vector3 rot = new Vector3(speedX, speedY, speedZ) * Time.deltaTime;
                scp.Rotation *= Quaternion.Euler(rot);

                yield return Timing.WaitForSeconds(0.02f); // smooth update
                elapsed += 0.02f;
            }
        }
        
        private IEnumerator<float> FollowPlayerCoroutine(SchematicObject scp, Player target, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (scp == null || target == null || !target.IsAlive)
                    yield break;

                // direction player is looking
                Vector3 forward = target.CameraTransform.forward;

                // offset in front of player
                Vector3 offset = forward * 1.5f; // distance in front
                Vector3 targetPos = target.Position + offset;

                // set SCP position
                scp.Position = targetPos;

                // make SCP face the player
                Vector3 lookDir = (target.Position - scp.Position).normalized;
                Quaternion rotation = Quaternion.LookRotation(lookDir);

                scp.Rotation = rotation;

                yield return Timing.WaitForSeconds(0.02f); // ~50 FPS update
                elapsed += 0.02f;
            }
        }
        private void ThrowObjects(Vector3 scpPos, float range)
        {
            int affectedCount = 0;
            List<string> affectedItems = new List<string>();

            foreach (Pickup pickup in Pickup.List)
            {
                if (pickup == null || pickup.Base == null)
                    continue;

                float distance = Vector3.Distance(pickup.Position, scpPos);

                if (distance <= range)
                {
                    Rigidbody rb = pickup.Base.gameObject.GetComponent<Rigidbody>();

                    if (rb != null)
                    {
                        Vector3 dir = (pickup.Position - scpPos).normalized;
                        float forceStrength = Mathf.Lerp(8f, 3f, distance / range);

                        Vector3 force = dir * forceStrength + Vector3.up * 5f;

                        rb.AddForce(force, ForceMode.Impulse);

                        // Debug tracking
                        affectedCount++;
                        affectedItems.Add(pickup.Type.ToString());
                    }
                }
            }

            // Logging
            if (affectedCount == 0)
            {
                Log.Debug($"[ThrowObjects] No items in range ({range}) at position {scpPos}");
            }
            else
            {
                Log.Debug($"[ThrowObjects] Threw {affectedCount} items.");
                Log.Debug($"[ThrowObjects] Items: {string.Join(", ", affectedItems)}");
            }
        }
        
        private void LockDoors(Room room, float dur)
        {
            DelaySwap = true;
            foreach (Door door in room.Doors)
            {
                door.Lock(DoorLockType.NoPower);
                Timing.CallDelayed(dur, () =>
                {
                    if (DelaySwap)
                    {
                        DelaySwap = false;
                    }
                    door.Unlock();
                });
            }
        }

        private IEnumerator<float> FlickerCoroutine(float duration, Room room)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (room == null)
                    yield break;

                // short blackout (this auto restores)
                float flickerTime = UnityEngine.Random.Range(0.05f, 0.25f);
                room.TurnOffLights(flickerTime);

                // wait random interval before next flicker
                float delay = UnityEngine.Random.Range(0.2f, 0.4f);
                yield return Timing.WaitForSeconds(delay);

                elapsed += delay;
            }
        }
    }
}