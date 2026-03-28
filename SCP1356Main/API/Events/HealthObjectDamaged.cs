using System;
using Exiled.API.Features;
using SCP1356Main.API.Schematic.HealthObject;
using UnityEngine;

namespace SCP1356Main.API.Events
{
    public class HealthObjectDamaged : EventArgs
    {
        public Player Player { get; }

        public GameObject GameObject { get; }

        public HealthComponent HealthComponent { get; }
        public int Damage { get; }

        public HealthObjectDamaged(Player killer, GameObject gameObject, HealthComponent healthComponent, int damage)
        {
            Player = killer;
            GameObject = gameObject;
            Damage = damage;
            HealthComponent = healthComponent;
        }
    }
}