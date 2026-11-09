using UnityEngine;

namespace InfectionShot
{
    /// <summary>Two panels that slide apart when the door opens.</summary>
    public sealed class DoorView : MonoBehaviour
    {
        [SerializeField] private Transform leftPanel;
        [SerializeField] private Transform rightPanel;
        [SerializeField] private float openOffset = 1.4f;
        [SerializeField] private float speed = 3f;

        private float _closedX;
        private float _k;
        private bool _open;

        private void Awake()
        {
            _closedX = Mathf.Abs(leftPanel.localPosition.x);
            Apply();
        }

        public void SetOpen(bool open, bool immediate)
        {
            _open = open;
            if (immediate) _k = open ? 1f : 0f;
            Apply();
        }

        private void Update()
        {
            float target = _open ? 1f : 0f;
            if (Mathf.Approximately(_k, target)) return;
            _k = Mathf.MoveTowards(_k, target, speed * Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            float x = _closedX + openOffset * _k;
            Vector3 l = leftPanel.localPosition;
            Vector3 r = rightPanel.localPosition;
            leftPanel.localPosition = new Vector3(-x, l.y, l.z);
            rightPanel.localPosition = new Vector3(x, r.y, r.z);
        }
    }
}
