using System;
using System.Collections.Generic;
using Exiled.API.Features;
using UnityEngine;
using Mirror;
using PlayerStatsSystem;
using InventorySystem.Items.ThrowableProjectiles;
using AdminToys;
using Exiled.API.Enums;
using SCP1356Main.API.Events;
using InventorySystem.Items.ThrowableProjectiles;
using UnityEngine;

namespace SCP1356Main.API.Schematic.HealthObject
{
    public enum SimpleDeathType
    {
        None,
        Destroy,
        Explode,
        Disable,
        Shrink
    }

    public class HealthComponent : NetworkBehaviour, IDestructible
    {
        public float MaxHealth;
        public float Health;
        public bool IsAlive = true;
        public SimpleDeathType DeathType;

        private bool _shrinking = false;

        public uint NetworkId => netId;
        public Vector3 CenterOfMass => transform.position;

        public void Init(float hp, SimpleDeathType deathType)
        {
            MaxHealth = hp;
            Health = hp;
            DeathType = deathType;

            RegisterToChildren();
        }

        // 🔗 Attach this to all primitive children so they receive damage
        private void RegisterToChildren()
        {
            foreach (var toy in GetComponentsInChildren<PrimitiveObjectToy>())
            {
                if (!toy.TryGetComponent(out HealthProxy proxy))
                    proxy = toy.gameObject.AddComponent<HealthProxy>();

                proxy.Parent = this;
            }
        }

        public bool Damage(float damage, DamageHandlerBase handler, Vector3 pos)
        {
            if (!IsAlive)
                return false;

            Player attacker = null;
            
            if (handler is AttackerDamageHandler atk)
                attacker = Player.Get(atk.Attacker.PlayerId);

            CustomEvents.InvokeHealthObjectDamaged(
                new HealthObjectDamaged(attacker, gameObject, this, (int)damage)
            );
            
            Health -= damage;

            if (Health <= 0)
            {
                IsAlive = false;
                
                CustomEvents.Invoke1356PlayerDamage(
                    new HealthObjectKilled(attacker, gameObject, this, (int)damage)
                );

                Die();
            }

            return true;
        }

        private void Die()
        {
            IsAlive = false;
            Log.Debug($"[HealthSystem] Schematic '{gameObject.name}' died at {transform.position}");

            switch (DeathType)
            {
                case SimpleDeathType.Destroy:
                    Destroy(gameObject);
                    break;

                case SimpleDeathType.Disable:
                    gameObject.SetActive(false);
                    break;

                case SimpleDeathType.Explode:
                    Explode();
                    Destroy(gameObject);
                    break;

                case SimpleDeathType.Shrink:
                    _shrinking = true;
                    break;
            }
        }

        private void Update()
        {
            if (_shrinking)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 5f);

                if (transform.localScale.magnitude <= 0.1f)
                    Destroy(gameObject);
            }
        }
// AI SLOP
        private void Explode()
        {
   
         //   ExplosionGrenade.Explode(Player.Get(ReferenceHub._hostHub).Footprint, transform.position, new ExplosionGrenade(),ExplosionType.Grenade);
        }
    }

    // 🔥 This forwards damage from primitives to parent
    public class HealthProxy : NetworkBehaviour, IDestructible
    {
        public HealthComponent Parent;

        public uint NetworkId => netId;
        public Vector3 CenterOfMass => transform.position;

        public bool Damage(float damage, DamageHandlerBase handler, Vector3 pos)
        {
            return Parent != null && Parent.Damage(damage, handler, pos);
        }
    }

    // ✅ EXTENSION METHOD (THIS IS WHAT YOU WANT)
    public static class HealthExtensions
    {
        public static HealthComponent AddHealth(this GameObject obj, float hp, SimpleDeathType deathType)
        {
            var health = obj.GetComponent<HealthComponent>();

            if (health == null)
                health = obj.AddComponent<HealthComponent>();

            health.Init(hp, deathType);
            return health;
        }
    }
}