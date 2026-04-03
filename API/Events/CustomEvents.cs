using System;

namespace SCP1356Main.API.Events
{
    public class CustomEvents
    {
        // Event Handlers
        public static event EventHandler<HealthObjectKilled> OnHealthObjectKilled;
        public static event EventHandler<SCP1356DamagedPlayer> SCP1356DamagedPlayer;
        public static event EventHandler<DamagingSCP1356> DamagingSCP1356;
        public static event EventHandler<SCP1356KillingPlayer> SCP1356KillingPlayer;
        public static event EventHandler<SCP1356Killed> SCP1356Killed;
        public static event EventHandler<RadiationParticleHit> RadiationParticleHit;
        public static event EventHandler<HealthObjectDamaged> HealthObjectDamaged;
        
        // Event Invoker
        public static void Invoke1356PlayerDamage(HealthObjectKilled ev) => OnHealthObjectKilled?.Invoke(null, ev);
        public static void Invoke1356DamagedPlayer(SCP1356DamagedPlayer ev) => SCP1356DamagedPlayer?.Invoke(null, ev);
        public static void InvokeDamagingSCP1356(DamagingSCP1356 ev) => DamagingSCP1356?.Invoke(null, ev);
        public static void InvokeSCP1356KillingPlayer(SCP1356KillingPlayer ev) => SCP1356KillingPlayer?.Invoke(null, ev);
        public static void InvokeHealthObjectDamaged(HealthObjectDamaged ev) => HealthObjectDamaged?.Invoke(null, ev);
        public static void InvokeRadiationParticleHit(RadiationParticleHit ev) => RadiationParticleHit?.Invoke(null, ev);
        
    }
}