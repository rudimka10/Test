using System;
using UnityEngine;

namespace InfectionShot
{
    public enum GameState { Ready, Charging, Flying, Advancing, Won, Lost }

    public enum LossReason { None, Overcharged, NotEnoughMass }

    /// <summary>Runtime state of the round: game state, player mass, charge held in the shot, player position on the route.</summary>
    public sealed class GameModel
    {
        private const float Epsilon = 0.0005f;

        private readonly GameConfig _config;

        public GameModel(GameConfig config)
        {
            _config = config;
            PlayerMass = config.initialMass; 
        }

        public GameState State { get; private set; } = GameState.Ready;
        public LossReason Reason { get; private set; } = LossReason.None;

        /// <summary>For a "not enough mass" loss: the cheapest cost of what is left, shown to the player.</summary>
        public float NeededMass { get; set; }
        public float PlayerMass { get; private set; }
        public float ChargedMass { get; private set; }
        public Vector3 PlayerPosition { get; private set; }
        public float PlayerDistance { get; private set; }

        /// <summary>Mass the player can still spend before reaching the critical size.</summary>
        public float Spendable => PlayerMass - _config.criticalMass;

        public event Action StateChanged;
        public event Action MassChanged;
        public event Action PositionChanged;
        public event Action ResetDone;

        public void SetState(GameState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke();
        }

        public void SetPlayerPosition(Vector3 position, float distance)
        {
            PlayerPosition = position;
            PlayerDistance = distance;
            PositionChanged?.Invoke();
        }

        /// <summary>Moves mass from the player into the shot. Returns false once the player has reached the critical size.</summary>
        public bool Transfer(float amount)
        {
            PlayerMass -= amount;
            ChargedMass += amount;
            MassChanged?.Invoke();
            return PlayerMass > _config.criticalMass + Epsilon;
        }

        public float TakeCharge()
        {
            float charged = ChargedMass;
            ChargedMass = 0f;
            MassChanged?.Invoke();
            return charged;
        }

        public void RefundCharge()
        {
            PlayerMass += ChargedMass;
            ChargedMass = 0f;
            MassChanged?.Invoke();
        }

        public void Lose(LossReason reason)
        {
            if (State == GameState.Won || State == GameState.Lost) return;
            Reason = reason;
            SetState(GameState.Lost);
        }

        public void Win()
        {
            if (State == GameState.Lost) return;
            SetState(GameState.Won);
        }

        public void Reset()
        {
            PlayerMass = _config.initialMass;
            ChargedMass = 0f;
            Reason = LossReason.None;
            ResetDone?.Invoke();
            MassChanged?.Invoke();
            SetState(GameState.Ready);
        }
    }
}
