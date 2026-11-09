using UnityEngine;

namespace InfectionShot
{
    /// <summary>The door opens once, when the player gets within the configured distance.</summary>
    public sealed class DoorModel
    {
        private readonly float _openDistance;
        private readonly Vector3 _position;

        public DoorModel(GameConfig config, PathModel path)
        {
            _openDistance = config.doorOpenDistance;
            _position = path.End;
        }

        public bool IsOpen { get; private set; }

        /// <summary>Returns true only on the call that opens the door.</summary>
        public bool TryOpen(Vector3 playerPosition)
        {
            if (IsOpen) return false;
            float dx = playerPosition.x - _position.x;
            float dz = playerPosition.z - _position.z;
            if (Mathf.Sqrt(dx * dx + dz * dz) > _openDistance) return false;
            IsOpen = true;
            return true;
        }

        public void Close() => IsOpen = false;
    }
}
