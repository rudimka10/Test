using UnityEngine;

namespace InfectionShot
{
    /// <summary>All balance numbers in one place. Radii are monotonic functions of mass, so they are easy to tune.</summary>
    [CreateAssetMenu(menuName = "InfectionShot/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Mass")]
        public float initialMass = 1f;
        [Tooltip("When the player ball shrinks to this mass the game is lost (Overcharged).")]
        public float criticalMass = 0.2f;
        [Tooltip("Mass moved from the player into the shot every second while holding.")]
        public float chargeRatePerSecond = 0.25f;
        [Tooltip("Mass of the smallest possible shot (a quick tap).")]
        public float minShotMass = 0.07f;
        [Tooltip("The level must be clearable with at most this share of the spendable mass (0.8 = 20% reserve).")]
        public float maxPlannedShare = 0.8f;

        [Header("Sizes")]
        public float playerRadiusBase = 0.25f;
        public float playerRadiusPerMass = 0.5f;
        public float shotRadiusBase = 0.14f;
        public float shotRadiusPerMass = 0.4f;
        public float obstacleRadius = 0.3f;

        [Header("Infection")]
        public float infectionRadiusBase = 0.45f;
        public float infectionRadiusPerMass = 3.8f;
        [Tooltip("Obstacles farther from the epicentre explode a little later (units per second).")]
        public float infectionWaveSpeed = 10f;

        [Header("Shot")]
        public float shotSpeed = 10f;
        public float maxShotDistance = 40f;

        [Header("Route")]
        [Tooltip("Free space kept on each side of the player ball inside the lane.")]
        public float lanePadding = 0.3f;
        [Tooltip("How close the player stops to the nearest obstacle.")]
        public float standoff = 2.2f;
        public float hopLength = 2.4f;
        public float hopDuration = 0.4f;
        public float hopHeight = 0.7f;
        [Tooltip("Distance from the door at which it opens (world units).")]
        public float doorOpenDistance = 5f;
        [Tooltip("The player stops this far before the door centre, which counts as arriving.")]
        public float doorStop = 0.7f;

        public float SpendableMass => initialMass - criticalMass;
        public float PlayerRadius(float mass) => playerRadiusBase + playerRadiusPerMass * mass;
        public float ShotRadius(float shotMass) => shotRadiusBase + shotRadiusPerMass * shotMass;
        public float InfectionRadius(float shotMass) => infectionRadiusBase + infectionRadiusPerMass * shotMass;
        public float LaneHalfWidth(float playerMass) => PlayerRadius(playerMass) + lanePadding;
    }
}
