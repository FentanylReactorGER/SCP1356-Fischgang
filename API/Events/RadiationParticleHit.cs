using UnityEngine;

namespace SCP1356Main.API.Events
{
    public class RadiationParticleHit
    {
        public RaycastHit RaycastHit { get; }
        public Transform SourceTransform { get; }
        public LayerMask EnvironmentMask { get; }
        
        public RadiationParticleHit(RaycastHit hit, Transform sourceTransform, LayerMask environmentMask)
        {
            RaycastHit = hit;
            SourceTransform  = sourceTransform;
            EnvironmentMask = environmentMask;
        }
    }
}