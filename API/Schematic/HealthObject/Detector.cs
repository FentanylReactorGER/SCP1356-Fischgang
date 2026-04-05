using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AdvancedMERTools;
using Exiled.API.Features;
using Exiled.API.Features.Core.Generic;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using PlayerRoles.RoleAssign;
using ProjectMER.Features.Objects;
using SCP1356Main.API.Events;
using SCP1356Main.Configs;
using UnityEngine;
using System.Text.RegularExpressions;
using String = System.String;

namespace SCP1356Main.API.Schematic.HealthObject
{
    public class Detector
    {

        private static readonly Translation Translation = Plugin.Singleton.Translation;

        private readonly Dictionary<RoleTypeId, string> RoleTranslations = Translation.RoleTranslations;

        private readonly Dictionary<RoleTypeId, string>
            RoleTranslationsGer = Translation.RoleTranslationsCustomLanguage;
        
        public bool SCP1356Contained { get; set; }
        public bool SCP1356ChamberOpen { get; set; }

        public void SubEvents()
        {
            SCP1356Contained = false;
            SCP1356ChamberOpen = false;
            CustomEvents.OnHealthObjectKilled += OnHealthObjectDestroyed;
            CustomEvents.HealthObjectDamagedEvent += HealthDamaged;
            CustomEvents.SCP1356DamagedPlayer += PlayerDamaged;
            Exiled.Events.Handlers.Warhead.Detonated += WarheadContain;
        }

        public void UnsubEvents()
        {
            CustomEvents.OnHealthObjectKilled -= OnHealthObjectDestroyed;
            CustomEvents.HealthObjectDamagedEvent -= HealthDamaged;
            CustomEvents.SCP1356DamagedPlayer -= PlayerDamaged;
            Exiled.Events.Handlers.Warhead.Detonated -= WarheadContain;
        }

        private bool cooldown = false;
        private void PlayerDamaged(object obj, SCP1356DamagedPlayer ev)
        {
            if (!cooldown)
            {
                var s = Plugin.Singleton;
                int value = Mathf.RoundToInt(UnityEngine.Random.Range(1f, ev.Damage));
                s._status.SetActivity(value, 2);
                Timing.RunCoroutine(CoolDown(2f));
            }
        }

        private IEnumerator<float> CoolDown(float time)
        {
            cooldown = true;
            yield return Timing.WaitForSeconds(time);
            cooldown = false;
        }
        
        private void HealthDamaged(object sender, HealthObjectDamaged ev)
        {
            if (ev.HealthComponent == Plugin.Singleton.SchematicSetup.SCP1356.gameObject
                    .GetComponentInChildren<HealthComponent>())
            {
                CustomEvents.InvokeDamagingSCP1356(new DamagingSCP1356(ev.Player, Plugin.Singleton.SchematicSetup.SCP1356, ev.Damage, ev.HealthComponent));
            }
        }
        
        private void OnHealthObjectDestroyed(object sender, HealthObjectKilled ev)
        {
            Log.Debug($"Health Object Killed: {ev.GameObject.name} by {ev.Player.Nickname}");

            
            if (ev.GameObject == Plugin.Singleton.SchematicSetup.SCP1356.gameObject)
            {
                Log.Debug($"Detected SCP1356s Death!");
                SCP1356Contained = true;
                ContainSCP1356(ev.Player, Plugin.Singleton.SchematicSetup.SCP1356);
            }
            
            else if (ev.GameObject == Plugin.Singleton.SchematicSetup.SCP1356ChamberHealth)
            {
                var RadCast = Plugin.Singleton.SchematicSetup.radiation;
                SCP1356ChamberOpen = true;
                Log.Debug($"Detected SCP1356s Chamber Glass to Open!");
                var oldhp = RadCast._rMax;
                var oldRange = RadCast._maxParticlesPerTick;
                RadCast.SetRadiationSettings(oldhp * 2, oldRange * 2);
            }
        }

        public void ContainSCP1356(Player player, SchematicObject SCP1356)
        {
            SCP1356?.Destroy();

            string cassieMessage = GetCassieMessage(player);
            string cassieTranslation = GetCassieTranslation(player);

            Exiled.API.Features.Cassie.MessageTranslated(cassieMessage, cassieTranslation);
        }

        private string GetCassieMessage(Player player)
        {
            if (player.Role.Team == Team.FoundationForces)
            {
                if (!string.IsNullOrEmpty(player.UnitName))
                {
                    string unitDesignation = $"NATO_{player.UnitName[0]}";
                    string unitNumber = Regex.Match(player.UnitName, @"\d+").Value;

                    return Translation.SCP1356CassieMessageContainNtf
                        .Replace("{unitDesignation}", unitDesignation)
                        .Replace("{unitNumber}", unitNumber);
                }

                return "Mobile Task Force has contained SCP 1 3 5 6";
            }

            return RoleTranslations.TryGetValue(player.Role.Type, out string customTranslation)
                ? Translation.SCP1356CassieMessageContainOther.Replace("{customTranslation}", customTranslation)
                : "SCP 1 3 5 6 successfully contained, reason unknown.";
        }

        private string GetCassieTranslation(Player player)
        {
            if (player.Role.Team == Team.FoundationForces)
            {
                return !string.IsNullOrEmpty(player.UnitName)
                    ? Translation.SCP1356CassieMessageTranslatedContainNtf.Replace("{unitDesignation}", player.UnitName)
                    : "Mobile Task Force hat SCP-1356 erfolgreich eingedämmt.";
            }

            return RoleTranslationsGer.TryGetValue(player.Role.Type, out string customTranslationGer)
                ? Translation.SCP1356CassieMessageTranslatedContainOther.Replace("{customTranslationGer}",
                    customTranslationGer)
                : $"SCP-1356 erfolgreich eingedämmt durch {player.Role.Type}.";
        }

        private void WarheadContain()
        {
            
        }
    }
}