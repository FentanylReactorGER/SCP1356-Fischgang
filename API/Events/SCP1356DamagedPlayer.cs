using System;
using Exiled.API.Features;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace SCP1356Main.API.Events
{
    public class SCP1356DamagedPlayer : EventArgs
    {
        public Player Player { get; }
    
        public Transform SourceTransform { get; }
    
        public float Damage { get; }
    
        public SCP1356DamagedPlayer(Player player, Transform sourceTransform, float damage)
        {
            Player = player;
            SourceTransform = sourceTransform;
            Damage = damage;
        }
    }
}