using System;
using UnityEngine;
using Zenject;

namespace InfectionShot
{
    /// <summary>Hold on the player ball: mass flows into a growing shot. Release: the shot is fired at the door.</summary>
    public sealed class ChargeController : IInitializable, ITickable, IDisposable
    {
        private readonly GameModel _model;
        private readonly GameConfig _config;
        private readonly PathModel _path;
        private readonly PointerInputView _pointer;
        private readonly ShotView _shotView;
        private readonly ShotController _shots;
        private readonly FieldModel _field;

        public ChargeController(GameModel model, GameConfig config, PathModel path, FieldModel field, PointerInputView pointer, ShotView shotView, ShotController shots)
        {
            _model = model;
            _config = config;
            _path = path;
            _field = field;
            _pointer = pointer;
            _shotView = shotView;
            _shots = shots;
        }

        public void Initialize()
        {
            _pointer.Down += OnDown;
            _pointer.Up += OnUp;
            _pointer.Cancel += OnCancel;
            _model.StateChanged += OnStateChanged;
        }

        public void Dispose()
        {
            _pointer.Down -= OnDown;
            _pointer.Up -= OnUp;
            _pointer.Cancel -= OnCancel;
            _model.StateChanged -= OnStateChanged;
        }

        public void Tick()
        {
            if (_model.State != GameState.Charging) return;
            Transfer(_config.chargeRatePerSecond * Time.deltaTime);
        }

        private void OnDown(Vector3 world)
        {
            if (_model.State != GameState.Ready) return;

            // A press only counts on the player ball; the margin keeps it comfortable for a finger.
            float reach = _config.PlayerRadius(_model.PlayerMass) * 1.3f + 0.4f;
            float dx = world.x - _model.PlayerPosition.x;
            float dz = world.z - _model.PlayerPosition.z;
            if (dx * dx + dz * dz > reach * reach) return;

            _model.SetState(GameState.Charging);
            Transfer(_config.minShotMass);
        }

        private void OnUp()
        {
            if (_model.State != GameState.Charging) return;
            float mass = _model.TakeCharge();
            _model.SetState(GameState.Flying);
            _shots.Fire(mass, LaunchPoint(mass));
        }

        private void OnCancel()
        {
            if (_model.State != GameState.Charging) return;
            _model.RefundCharge();
            _model.SetState(GameState.Ready);
        }

        private void OnStateChanged()
        {
            if (_model.State == GameState.Lost || _model.State == GameState.Ready || _model.State == GameState.Won) _shotView.Hide();
        }

        private void Transfer(float amount)
        {
            bool stillAlive = _model.Transfer(amount);
            ShowPreview();
            if (!stillAlive) _model.Lose(LossReason.Overcharged);
        }

        private void ShowPreview()
        {
            float mass = _model.ChargedMass;
            Vector3 launch = LaunchPoint(mass);
            float radius = _config.ShotRadius(mass);
            _shotView.Show(launch, radius);

            // The blast happens where the shot first touches an obstacle, so the ring is drawn there. No hit means no blast and no ring.
            ObstacleModel hit = ShotMath.FirstHit(launch, _path.Direction, radius, _field.All, _config.maxShotDistance, out float travel);
            if (hit == null) _shotView.HideRing();
            else _shotView.ShowRing(launch + _path.Direction * travel, _config.InfectionRadius(mass));
        }

        private Vector3 LaunchPoint(float shotMass)
        {
            float distance = _config.PlayerRadius(_model.PlayerMass) + _config.ShotRadius(shotMass) * 0.8f;
            Vector3 ground = _model.PlayerPosition + _path.Direction * distance;
            return new Vector3(ground.x, 0f, ground.z);
        }
    }
}
