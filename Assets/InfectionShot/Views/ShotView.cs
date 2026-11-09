using UnityEngine;

namespace InfectionShot
{
    /// <summary>The shot ball (it is split off the player while charging), its infection-radius ring and the expanding blast ring.</summary>
    public sealed class ShotView : MonoBehaviour
    {
        private const int RingPoints = 48;

        [SerializeField] private Transform ball;
        [SerializeField] private LineRenderer radiusRing;
        [SerializeField] private LineRenderer blastRing;

        private readonly Vector3[] _points = new Vector3[RingPoints];
        private float _blastTime = -1f;
        private float _blastRadius;
        private Vector3 _blastCenter;

        private const float BlastDuration = 0.45f;

        private void Awake()
        {
            ball.gameObject.SetActive(false);
            radiusRing.enabled = false;
            blastRing.enabled = false;
        }

        public void Show(Vector3 ground, float radius)
        {
            ball.gameObject.SetActive(true);
            ball.localScale = Vector3.one * (radius * 2f);
            ball.position = new Vector3(ground.x, radius, ground.z);
        }

        /// <summary>Shows where the blast will happen and how far it reaches.</summary>
        public void ShowRing(Vector3 center, float infectionRadius)
        {
            radiusRing.enabled = true;
            DrawRing(radiusRing, new Vector3(center.x, 0.05f, center.z), infectionRadius);
        }

        public void HideRing() => radiusRing.enabled = false;

        public void Hide()
        {
            ball.gameObject.SetActive(false);
            radiusRing.enabled = false;
        }

        public void PlayBlast(Vector3 center, float radius)
        {
            _blastCenter = new Vector3(center.x, 0.06f, center.z);
            _blastRadius = radius;
            _blastTime = 0f;
            blastRing.enabled = true;
        }

        public void ClearBlast()
        {
            _blastTime = -1f;
            blastRing.enabled = false;
        }

        private void Update()
        {
            if (_blastTime < 0f) return;
            _blastTime += Time.deltaTime;
            float k = _blastTime / BlastDuration;
            if (k >= 1f)
            {
                ClearBlast();
                return;
            }

            DrawRing(blastRing, _blastCenter, _blastRadius * Mathf.Lerp(0.4f, 1f, 1f - (1f - k) * (1f - k)));
            Color c = blastRing.startColor;
            c.a = 1f - k;
            blastRing.startColor = c;
            blastRing.endColor = c;
        }

        private void DrawRing(LineRenderer line, Vector3 center, float radius)
        {
            for (int i = 0; i < RingPoints; i++)
            {
                float a = (i / (float)RingPoints) * Mathf.PI * 2f;
                _points[i] = new Vector3(center.x + Mathf.Cos(a) * radius, center.y, center.z + Mathf.Sin(a) * radius);
            }

            line.positionCount = RingPoints;
            line.SetPositions(_points);
        }
    }
}
