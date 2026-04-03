using Exiled.API.Features;
using JetBrains.Annotations;
using ProjectMER.Features.Objects;
using SCP1356Main.API.Extensions;

namespace SCP1356Main.API.Events
{
    public class SCP1356Killed
    {
        [CanBeNull] public Player Player { get; }
    
        public SchematicObject Schematic { get; }
        public Tools.DeathTypesSCP1356 DeathType { get; }
        
    
        public SCP1356Killed([CanBeNull] Player player, SchematicObject schematic, Tools.DeathTypesSCP1356 deathType)
        {
            Player = player;
            Schematic = schematic;
            DeathType = deathType;
        }
    }
}