using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfectionShot
{
    /// <summary>Turns touch (or the mouse in the Editor) into world-space press/release events on the ground plane.</summary>
    public sealed class PointerInputView : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;

        private bool _pressed;
        private Plane _ground = new Plane(Vector3.up, Vector3.zero);

        public event Action<Vector3> Down;
        public event Action Up;
        public event Action Cancel;

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return;

            if (pointer.press.wasPressedThisFrame) Press(pointer.position.ReadValue());
            if (pointer.press.wasReleasedThisFrame) Release();
        }

        /// <summary>Automation hook: behaves exactly like a press (used to drive the game from the Editor without window focus).</summary>
        public void SimulateDown(Vector3 worldPoint)
        {
            if (_pressed) return;
            _pressed = true;
            Down?.Invoke(worldPoint);
        }

        public void SimulateUp() => Release();

        private void Press(Vector2 screenPosition)
        {
            if (_pressed) return;
            Ray ray = viewCamera.ScreenPointToRay(screenPosition);
            if (!_ground.Raycast(ray, out float distance)) return;
            _pressed = true;
            Down?.Invoke(ray.GetPoint(distance));
        }

        private void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            Up?.Invoke();
        }

        private void Abort()
        {
            if (!_pressed) return;
            _pressed = false;
            Cancel?.Invoke();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Abort();
        }

        private void OnDisable()
        {
            Abort();
        }
    }
}
