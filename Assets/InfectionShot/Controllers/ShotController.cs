using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace InfectionShot
{
    /// <summary>
    /// Flies the single shot along the route to its first obstacle, then runs the infection blast around that impact point.
    /// The hit is computed up front (a deterministic swept circle), the view just follows.
    /// </summary>
    public sealed class ShotController : IInitializable, ITickable, IDisposable
    {
        private readonly GameModel _model;
        private readonly GameConfig _config;
        private readonly PathModel _path;
        private readonly FieldModel _field;
        private readonly FieldView _fieldView;
        private readonly ShotView _shotView;
        private readonly FxView _fx;
        private readonly List<ObstacleModel> _hits = new List<ObstacleModel>(64);

        private bool _active;
        private float _mass;
        private Vector3 _origin;
        private float _travel;
        private float _distance;
        private ObstacleModel _hit;

        public ShotController(GameModel model, GameConfig config, PathModel path, FieldModel field, FieldView fieldView, ShotView shotView, FxView fx)
        {
            _model = model;
            _config = config;
            _path = path;
            _field = field;
            _fieldView = fieldView;
            _shotView = shotView;
            _fx = fx;
        }

        public void Initialize()
        {
            _model.ResetDone += OnReset;
        }

        public void Dispose()
        {
            _model.ResetDone -= OnReset;
        }

        public void Fire(float mass, Vector3 origin)
        {
            _mass = mass;
            _origin = origin;
            _hit = ShotMath.FirstHit(origin, _path.Direction, _config.ShotRadius(mass), _field.All, _config.maxShotDistance, out _travel);
            _distance = 0f;
            _active = true;
            _shotView.HideRing();
            _shotView.Show(origin, _config.ShotRadius(mass));
        }

        public void Tick()
        {
            if (!_active) return;
            _distance = Mathf.Min(_distance + _config.shotSpeed * Time.deltaTime, _travel);
            _shotView.Show(_origin + _path.Direction * _distance, _config.ShotRadius(_mass));
            if (_distance >= _travel) Resolve();
        }

        private void Resolve()
        {
            _active = false;
            _shotView.Hide();

            if (_hit != null)
            {
                Vector3 center = _origin + _path.Direction * _travel;
                float radius = _config.InfectionRadius(_mass);
                ShotMath.Select(_field.All, center, radius, _hits);
                for (int i = 0; i < _hits.Count; i++)
                {
                    ObstacleModel o = _hits[i];
                    o.Deactivate();
                    float dx = o.Position.x - center.x;
                    float dz = o.Position.z - center.z;
                    float delay = Mathf.Sqrt(dx * dx + dz * dz) / _config.infectionWaveSpeed;
                    _fieldView.Infect(o.Id, delay, _fx);
                }

                _shotView.PlayBlast(center, radius);
            }

            _model.SetState(GameState.Advancing);
        }

        private void OnReset()
        {
            _active = false;
            _shotView.Hide();
            _shotView.ClearBlast();
        }
    }
}
