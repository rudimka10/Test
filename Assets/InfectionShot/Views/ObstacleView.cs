using UnityEngine;

namespace InfectionShot
{
    /// <summary>One obstacle on screen. Data used by the composition root is serialized, so it is valid before Awake.</summary>
    public sealed class ObstacleView : MonoBehaviour
    {
        private const float PopDuration = 0.18f;

        [SerializeField] private int group = -1;
        [SerializeField] private bool blocking;
        [SerializeField] private Color burstColor = new Color(0.35f, 0.75f, 0.3f);

        private Vector3 _baseScale;
        private bool _scaleKnown;
        private float _delay = -1f;
        private float _pop = -1f;
        private FxView _fx;

        public int Group => group;
        public bool Blocking => blocking;

        public void Infect(float delay, FxView fx)
        {
            Remember();
            _fx = fx;
            _delay = delay;
        }

        public void Restore()
        {
            Remember();
            _delay = -1f;
            _pop = -1f;
            transform.localScale = _baseScale;
            gameObject.SetActive(true);
        }

        private void Remember()
        {
            if (_scaleKnown) return;
            _baseScale = transform.localScale;
            _scaleKnown = true;
        }

        private void Update()
        {
            if (_delay >= 0f)
            {
                _delay -= Time.deltaTime;
                if (_delay > 0f) return;
                _delay = -1f;
                _pop = 0f;
                if (_fx != null) _fx.Burst(transform.position + Vector3.up * 0.4f, burstColor);
            }

            if (_pop < 0f) return;
            _pop += Time.deltaTime;
            float k = _pop / PopDuration;
            if (k >= 1f)
            {
                _pop = -1f;
                gameObject.SetActive(false);
                return;
            }

            // Brief swell, then collapse: reads as "it blew up".
            float scale = k < 0.3f ? Mathf.Lerp(1f, 1.25f, k / 0.3f) : Mathf.Lerp(1.25f, 0f, (k - 0.3f) / 0.7f);
            transform.localScale = _baseScale * scale;
        }
    }
}
