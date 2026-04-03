using Exiled.API.Features;
using ProjectMER.Features.Objects;

namespace SCP1356Main.API.Events
{
    public class SCP1356KillingPlayer
    {
        public Player Player { get; }
        
    
        public SCP1356KillingPlayer(Player player)
        {
            Player = player;
        }
    }
}