using UnityEngine;

namespace InfectionShot
{
    /// <summary>The coloured lane the player hops along. It runs from the player to the door and shrinks together with the ball.</summary>
    public sealed class LaneView : MonoBehaviour
    {
        [SerializeField] private float smoothing = 8f;

        private Vector3 _from;
        private Vector3 _to;
        private float _targetHalfWidth = 1f;
        private float _halfWidth = 1f;

        public void SetSegment(Vector3 from, Vector3 to)
        {
            _from = from;
            _to = to;
            Apply();
        }

        public void SetHalfWidth(float halfWidth, bool immediate)
        {
            _targetHalfWidth = halfWidth;
            if (immediate) _halfWidth = halfWidth;
            Apply();
        }

        private void Update()
        {
            if (Mathf.Approximately(_halfWidth, _targetHalfWidth)) return;
            _halfWidth = Mathf.Lerp(_halfWidth, _targetHalfWidth, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            Apply();
        }

        private void Apply()
        {
            Vector3 delta = _to - _from;
            float length = delta.magnitude;
            if (length < 0.01f) return;
            transform.position = (_from + _to) * 0.5f + Vector3.up * 0.03f;
            transform.rotation = Quaternion.LookRotation(delta / length, Vector3.up);
            transform.localScale = new Vector3(_halfWidth * 2f, 0.02f, length);
        }
    }
}
