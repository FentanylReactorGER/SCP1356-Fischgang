using System;
using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using SCP1356Main.API.Events;
using SCP1356Main.API.Extensions;
using UnityEngine;
using SCP1356Main.Configs;

namespace SCP1356Main.API.Schematic.Radiation
{
    public class RadiationRaycast : IDisposable
    {
        // CONFIG (mutable now)
        public int _minParticlesPerTick;
        public int _maxParticlesPerTick;
        public float _tickInterval;

        public float _rMax;
        public float _particleRMax;
        public float _particleDMax;

        public List<EffectType> _radiationEffects;
        public float _effectDuration;

        public string _soundPath;

        public float _minSoundCooldown;
        public float _maxSoundCooldown;

        // SCHEMATIC ROOT
        public Transform SourceTransform;

        public LayerMask EnvironmentMask = ~0;

        // RUNTIME
        private bool _running;
        private CoroutineHandle _handle;
        private bool _playingSound;
        private List<Player> HitList = new List<Player>();

        public readonly Dictionary<Player, float> Radiation = new();

        // ✅ CONSTRUCTOR
        public RadiationRaycast(Transform schematicRoot)
        {
            SourceTransform = schematicRoot;

            // Apply default config
            ApplyConfig(Plugin.Singleton.Config);
        }
        // ✅ APPLY FULL CONFIG (MAIN METHOD)
        public void ApplyConfig(Config cfg)
        {
            _minParticlesPerTick = cfg.MinParticlesPerTick;
            _maxParticlesPerTick = cfg.MaxParticlesPerTick;
            _tickInterval = cfg.TickInterval;

            _rMax = cfg.RMax;
            _particleRMax = cfg.ParticleRMax;
            _particleDMax = cfg.ParticleDMax;

            _radiationEffects = cfg.RadiationEffects;
            _effectDuration = cfg.EffectDuration;

            _minSoundCooldown = cfg.SoundCooldownMin;
            _maxSoundCooldown = cfg.SoundCooldownMax;
        }

        // ✅ QUICK EDIT METHOD (OPTIONAL)
        public void SetRadiationSettings(float rMax, int maxParticles)
        {
            _rMax = rMax;
            _maxParticlesPerTick = maxParticles;
        }

        public void Start()
        {
            if (_running)
                return;

            _running = true;
            _handle = Timing.RunCoroutine(RadiationLoop(), Segment.FixedUpdate);
        }

        public void Stop()
        {
            if (!_running)
                return;

            _running = false;

            if (_handle.IsRunning)
                Timing.KillCoroutines(_handle);

            Radiation.Clear();
        }

        public int GetPlayerRadiation(Player player)
        {
            if (Radiation.TryGetValue(player, out var v))
                return GetRadLevel(v);

            return 0;
        }

        public int GetRadLevel(float radiationAmount)
        {
            if (radiationAmount >= 13500) return 5;
            if (radiationAmount >= 8050) return 4;
            if (radiationAmount >= 7000) return 3;
            if (radiationAmount >= 4500) return 2;
            if (radiationAmount >= 2500) return 1;
            return 0;
        }

        private HashSet<Player> _soundCooldown = new();

        private IEnumerator<float> PlaySound(Player player)
        {
            if (_soundCooldown.Contains(player))
                yield break;

            _soundCooldown.Add(player);

            float cd = UnityEngine.Random.Range(_minSoundCooldown, _maxSoundCooldown);

            if (!HitList.Contains(player))
            {
                HitList.Add(player);

                SourceTransform.position.PlayAudioAt(Plugin.Singleton.Config.SoundEncounter, 20, 4);

                Timing.RunCoroutine(HitListCool(player));
            }
            else
            {
                SourceTransform.position.PlayAudioAt(Plugin.Singleton.Config.SoundHit, 20, 4);
            }

            yield return Timing.WaitForSeconds(cd);

            _soundCooldown.Remove(player);
        }
        
        private IEnumerator<float> HitListCool(Player player)
        {
            yield return Timing.WaitForSeconds(UnityEngine.Random.Range(10*_minSoundCooldown, 10*_maxSoundCooldown));
            HitList.Remove(player);
        }

        private IEnumerator<float> RadiationLoop()
        {
            System.Random rand = new System.Random();

            while (_running)
            {
                if (SourceTransform == null)
                {
                    yield return Timing.WaitForSeconds(_tickInterval);
                    continue;
                }

                int particleCount = rand.Next(_minParticlesPerTick, _maxParticlesPerTick + 1);
                Vector3 origin = SourceTransform.position;

                for (int i = 0; i < particleCount; i++)
                {
                    Vector3 dir = UnityEngine.Random.onUnitSphere;

                    if (Physics.Raycast(origin, dir, out RaycastHit hit, _rMax, EnvironmentMask,
                            QueryTriggerInteraction.Ignore))
                    {
                     //   CustomEvents.InvokeRadiationParticleHit(new RadiationParticleHit(hit, SourceTransform, EnvironmentMask));
                        Player hitPlayer = Player.Get(hit.collider.gameObject);
                        if (hitPlayer == null ||
                            !hitPlayer.IsAlive ||
                            hitPlayer.Role.Team == Team.SCPs ||
                            hitPlayer.IsGodModeEnabled)
                            continue;

                        float distance = hit.distance;
                        
                        float radiation = _particleRMax * (1f - Mathf.Pow(distance / _rMax, 3f));

                        if (radiation <= 0f)
                            continue;

                        if (Radiation.ContainsKey(hitPlayer))
                            Radiation[hitPlayer] += radiation;
                        else
                            Radiation[hitPlayer] = radiation;
                        
                        float damage = _particleDMax * (1f - Mathf.Pow(distance / _rMax, 2f));

                        if (damage > 0f)
                        {
                            float hurtAmount = Mathf.Min(damage, hitPlayer.Health);

                            if (hurtAmount > 0f)
                            {
                             //   CustomEvents.Invoke1356DamagedPlayer(new SCP1356DamagedPlayer(hitPlayer, SourceTransform, hurtAmount));
                                hitPlayer.Hurt(hurtAmount, "SCP-1356 Radiation");
                                Timing.RunCoroutine(PlaySound(hitPlayer));
                                foreach (EffectType effect in _radiationEffects)
                                    hitPlayer.EnableEffect(effect, (int)_effectDuration);
                            }
                        }
                    }
                }

                yield return Timing.WaitForSeconds(_tickInterval);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}