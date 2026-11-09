using UnityEngine;

namespace InfectionShot
{
    /// <summary>The player ball. Only shows what the controller tells it: size, ground position and hop height.</summary>
    public sealed class PlayerView : MonoBehaviour
    {
        private float _radius = 0.75f;
        private Vector3 _ground;
        private float _hop;

        public void SetRadius(float radius)
        {
            _radius = radius;
            transform.localScale = Vector3.one * (radius * 2f);
            Apply();
        }

        public void SetPosition(Vector3 ground, float hopHeight)
        {
            _ground = ground;
            _hop = hopHeight;
            Apply();
        }

        private void Apply()
        {
            transform.position = new Vector3(_ground.x, _radius + _hop, _ground.z);
        }
    }
}
