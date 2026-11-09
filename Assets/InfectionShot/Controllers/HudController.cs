using System;
using Zenject;

namespace InfectionShot
{
    /// <summary>Presents model values on the HUD.</summary>
    public sealed class HudController : IInitializable, IDisposable
    {
        private readonly GameModel _model;
        private readonly GameConfig _config;
        private readonly GameController _game;
        private readonly HudView _view;

        private bool _hasFired;

        public HudController(GameModel model, GameConfig config, GameController game, HudView view)
        {
            _model = model;
            _config = config;
            _game = game;
            _view = view;
        }

        public void Initialize()
        {
            _model.StateChanged += OnStateChanged;
            _model.MassChanged += OnMassChanged;
            _model.ResetDone += OnReset;
            _view.RestartClicked += _game.Restart;
            _view.SetCriticalFraction(_config.criticalMass / _config.initialMass);
            OnMassChanged();
            OnStateChanged();
        }

        public void Dispose()
        {
            _model.StateChanged -= OnStateChanged;
            _model.MassChanged -= OnMassChanged;
            _model.ResetDone -= OnReset;
            _view.RestartClicked -= _game.Restart;
        }

        private void OnReset()
        {
            _hasFired = false;
            _view.HideResult();
        }

        private void OnMassChanged()
        {
            _view.SetBar(_model.PlayerMass / _config.initialMass, _model.ChargedMass / _config.initialMass);
        }

        private void OnStateChanged()
        {
            if (_model.State == GameState.Flying) _hasFired = true;

            _view.SetHintVisible(!_hasFired && (_model.State == GameState.Ready || _model.State == GameState.Charging));
            switch (_model.State)
            {
                case GameState.Ready: _view.SetStatus("Ready"); break;
                case GameState.Charging: _view.SetStatus("Charging..."); break;
                case GameState.Flying: _view.SetStatus("Fire!"); break;
                case GameState.Advancing: _view.SetStatus("Moving on"); break;
                case GameState.Won:
                    _view.SetStatus("Done");
                    _view.ShowResult("You win!", "The door is open and the ball is through.");
                    break;
                case GameState.Lost:
                    _view.SetStatus("Lost");
                    _view.ShowResult(
                        _model.Reason == LossReason.Overcharged ? "Overcharged" : "Not enough mass",
                        _model.Reason == LossReason.Overcharged
                            ? "The ball gave away too much of itself."
                            : "Spare mass: " + _model.Spendable.ToString("0.00") + ". Even the cheapest way to clear the rest needs "
                                + (float.IsInfinity(_model.NeededMass) ? "more than that" : _model.NeededMass.ToString("0.00")) + ".");
                    break;
            }
        }
    }
}
