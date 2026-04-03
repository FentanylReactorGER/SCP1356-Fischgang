
using AdvancedMERTools;
using Exiled.API.Features;
using ProjectMER.Features.Objects;
using SCP1356Main.API.Schematic.HealthObject;

namespace SCP1356Main.API.Events
{
    public class DamagingSCP1356
    {
        public Player Player { get; }

        public SchematicObject SCP1356 { get; }

        public HealthComponent SCP1356Hitbox { get; }
        public int Damage { get; }

        public DamagingSCP1356(Player player, SchematicObject scp1356, int damage, HealthComponent Scp1356Hitbox)
        {
            Player = player;
            SCP1356 = scp1356;
            Damage = damage;
            SCP1356Hitbox = Scp1356Hitbox;
        }
    }
}