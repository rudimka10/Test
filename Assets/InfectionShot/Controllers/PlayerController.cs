using System;
using Zenject;

namespace InfectionShot
{
    /// <summary>Keeps the player ball and its lane in sync with the model.</summary>
    public sealed class PlayerController : IInitializable, IDisposable
    {
        private readonly GameModel _model;
        private readonly GameConfig _config;
        private readonly PathModel _path;
        private readonly PlayerView _view;
        private readonly LaneView _lane;

        public PlayerController(GameModel model, GameConfig config, PathModel path, PlayerView view, LaneView lane)
        {
            _model = model;
            _config = config;
            _path = path;
            _view = view;
            _lane = lane;
        }

        public void Initialize()
        {
            _model.MassChanged += OnMassChanged;
            _model.PositionChanged += OnPositionChanged;
            _model.ResetDone += OnReset;
            OnReset();
            _lane.SetHalfWidth(_config.LaneHalfWidth(_model.PlayerMass), true);
        }

        public void Dispose()
        {
            _model.MassChanged -= OnMassChanged;
            _model.PositionChanged -= OnPositionChanged;
            _model.ResetDone -= OnReset;
        }

        private void OnReset()
        {
            _model.SetPlayerPosition(_path.PointAt(0f), 0f);
            _view.SetRadius(_config.PlayerRadius(_model.PlayerMass));
            _lane.SetHalfWidth(_config.LaneHalfWidth(_model.PlayerMass), true);
        }

        private void OnMassChanged()
        {
            _view.SetRadius(_config.PlayerRadius(_model.PlayerMass));
            _lane.SetHalfWidth(_config.LaneHalfWidth(_model.PlayerMass), false);
        }

        private void OnPositionChanged()
        {
            _view.SetPosition(_model.PlayerPosition, 0f);
            _lane.SetSegment(_model.PlayerPosition, _path.End);
        }
    }
}
