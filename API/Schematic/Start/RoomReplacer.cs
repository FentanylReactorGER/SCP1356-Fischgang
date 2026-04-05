using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace SCP1356Main.API.Schematic.Start
{
    public class RoomReplacer
    {
        private static readonly Dictionary<RoomType, CachedRoomData> CachedRooms = new();

        public static SchematicObject ReplaceRoom(RoomType roomType, string schematicName, Vector3 pos, Vector3 rot)
        {
            Room room = Room.Get(roomType);

            if (room == null)
            {
                if (TryReplaceCachedRoom(roomType, schematicName, pos, rot, out CachedRoomData cachedRoom))
                    return null;

                Log.Debug($"[RoomReplacer] Replaced cached room {roomType} with schematic {schematicName}.");
                return null;
            }

            DestroyRoom(room);

            Vector3 finalPosition = room.Position + pos;
            Vector3 finalRotation = room.Rotation.eulerAngles + rot;

            SchematicObject schematic = ObjectSpawner.SpawnSchematic(
                schematicName,
                finalPosition,
                Quaternion.Euler(finalRotation),
                Vector3.one);

            if (schematic == null)
            {
                Log.Warn($"[RoomReplacer] Failed to spawn schematic '{schematicName}' for room '{roomType}'.");
                return null;
            }

            CachedRoomData roomData = new CachedRoomData(room.Position, room.Rotation.eulerAngles, schematic);

            if (CachedRooms.ContainsKey(roomType))
                CachedRooms[roomType] = roomData;
            else
                CachedRooms.Add(roomType, roomData);

            Log.Debug($"[RoomReplacer] Room {roomType} replaced with schematic {schematicName} at {finalPosition} / {finalRotation}.");
            return schematic;
        }

        private static bool TryReplaceCachedRoom(RoomType roomType, string schematicName, Vector3 pos, Vector3 rot, out CachedRoomData cachedRoomData)
        {
            if (!CachedRooms.TryGetValue(roomType, out cachedRoomData))
                return true;

            Vector3 finalPosition = cachedRoomData.Position + pos;
            Vector3 finalRotation = cachedRoomData.Rotation + rot;

            cachedRoomData.Schematic?.Destroy();

            cachedRoomData.Schematic = ObjectSpawner.SpawnSchematic(
                schematicName,
                finalPosition,
                Quaternion.Euler(finalRotation),
                Vector3.one);

            if (cachedRoomData.Schematic == null)
            {
                Log.Warn($"[RoomReplacer] Failed to respawn cached schematic '{schematicName}' for room '{roomType}'.");
                return true;
            }

            return false;
        }

        private static void DestroyRoom(Room room)
        {
            foreach (Component component in room.GameObject.GetComponentsInChildren<Component>())
            {
                try
                {
                    if (component == null)
                        continue;

                    if (component.name.Contains("SCP-079") || component.name.Contains("CCTV"))
                        continue;

                    if (component.GetComponentsInParent<Component>()
                        .Any(x => x != null && (x.name.Contains("SCP-079") || x.name.Contains("CCTV"))))
                        continue;

                    Object.Destroy(component);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private class CachedRoomData
        {
            public CachedRoomData(Vector3 position, Vector3 rotation, SchematicObject schematic)
            {
                Position = position;
                Rotation = rotation;
                Schematic = schematic;
            }

            public Vector3 Position { get; set; }
            public Vector3 Rotation { get; set; }
            public SchematicObject Schematic { get; set; }
        }
    }
}