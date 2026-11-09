using System;
using UnityEngine;
using Zenject;

namespace InfectionShot
{
    /// <summary>
    /// After a shot, the player hops forward along the lane centre until the nearest remaining obstacle is a safe distance ahead.
    /// Opens the door at 5 m and wins on arrival.
    /// </summary>
    public sealed class PathController : IInitializable, ITickable, IDisposable
    {
        private readonly GameModel _model;
        private readonly GameConfig _config;
        private readonly PathModel _path;
        private readonly FieldModel _field;
        private readonly DoorModel _door;
        private readonly DoorView _doorView;
        private readonly PlayerView _playerView;

        private bool _hopping;
        private float _from;
        private float _to;
        private float _time;

        public PathController(GameModel model, GameConfig config, PathModel path, FieldModel field, DoorModel door, DoorView doorView, PlayerView playerView)
        {
            _model = model;
            _config = config;
            _path = path;
            _field = field;
            _door = door;
            _doorView = doorView;
            _playerView = playerView;
        }

        public void Initialize()
        {
            _model.ResetDone += OnReset;
        }

        public void Dispose()
        {
            _model.ResetDone -= OnReset;
        }

        public void Tick()
        {
            if (_model.State != GameState.Advancing) return;
            if (!_hopping && !BeginHop()) return;

            _time += Time.deltaTime / _config.hopDuration;
            float k = Mathf.Clamp01(_time);
            float eased = k * k * (3f - 2f * k);
            float distance = Mathf.Lerp(_from, _to, eased);
            Vector3 ground = _path.PointAt(distance);
            _model.SetPlayerPosition(ground, distance);
            _playerView.SetPosition(ground, Mathf.Sin(k * Mathf.PI) * _config.hopHeight);

            if (_door.TryOpen(ground)) _doorView.SetOpen(true, false);
            if (k >= 1f) _hopping = false;
        }

        /// <summary>Starts the next hop, or finishes the advance when there is nowhere left to go. Returns true if a hop began.</summary>
        private bool BeginHop()
        {
            float stop = _path.Length - _config.doorStop;
            float target = stop;
            if (_field.TryGetNearestBlocking(_path, out float obstacle)) target = Mathf.Min(obstacle - _config.standoff, stop);

            float here = _model.PlayerDistance;
            if (target <= here + 0.05f)
            {
                if (here >= stop - 0.05f) _model.Win();
                else _model.SetState(GameState.Ready);
                return false;
            }

            _from = here;
            _to = Mathf.Min(here + _config.hopLength, target);
            _time = 0f;
            _hopping = true;
            return true;
        }

        private void OnReset()
        {
            _hopping = false;
            _door.Close();
            _doorView.SetOpen(false, true);
        }
    }
}
