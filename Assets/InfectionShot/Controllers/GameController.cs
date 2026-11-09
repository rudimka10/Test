using System;
using Zenject;

namespace InfectionShot
{
    /// <summary>Round rules that span features: the "not enough mass left" loss and restart.</summary>
    public sealed class GameController : IInitializable, IDisposable
    {
        private const float Tolerance = 0.001f;

        private readonly GameModel _model;
        private readonly GameConfig _config;
        private readonly PathModel _path;
        private readonly FieldModel _field;
        private readonly FieldView _fieldView;

        public GameController(GameModel model, GameConfig config, PathModel path, FieldModel field, FieldView fieldView)
        {
            _model = model;
            _config = config;
            _path = path;
            _field = field;
            _fieldView = fieldView;
        }

        public void Initialize()
        {
            _model.StateChanged += OnStateChanged;
        }

        public void Dispose()
        {
            _model.StateChanged -= OnStateChanged;
        }

        public void Restart()
        {
            _field.Reset();
            _fieldView.RestoreAll();
            _model.Reset();
        }

        private void OnStateChanged()
        {
            if (_model.State == GameState.Ready) CheckBudget();
        }

        /// <summary>Loses when the cheapest way to clear what is left costs more than the mass the player can still spend.</summary>
        private void CheckBudget()
        {
            float spendable = _model.Spendable;
            float needed = _field.EstimateRemainingMass(_path, _config, spendable);
            if (!float.IsInfinity(needed) && needed <= spendable + Tolerance) return;

            _model.NeededMass = needed;
            _model.Lose(LossReason.NotEnoughMass);
        }
    }
}
