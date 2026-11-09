using UnityEngine;

namespace InfectionShot
{
    /// <summary>One shared particle system for every obstacle burst, so nothing is instantiated at runtime.</summary>
    public sealed class FxView : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private int burstCount = 10;

        public void Burst(Vector3 position, Color color)
        {
            var emit = new ParticleSystem.EmitParams { position = position, startColor = color, applyShapeToPosition = true };
            particles.Emit(emit, burstCount);
        }
    }
}
