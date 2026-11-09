using System.Collections.Generic;
using UnityEngine;

namespace InfectionShot
{
    /// <summary>Pure planar geometry for shots: first hit along a line, who is inside the blast, cheapest shot for a group.</summary>
    public static class ShotMath
    {
        /// <summary>Sweeps a circle along a ray and returns the first active blocking obstacle it touches (null if none within maxDistance).</summary>
        public static ObstacleModel FirstHit(Vector3 origin, Vector3 direction, float shotRadius, IReadOnlyList<ObstacleModel> obstacles, float maxDistance, out float travel)
        {
            ObstacleModel best = null;
            float bestT = float.MaxValue;
            for (int i = 0; i < obstacles.Count; i++)
            {
                ObstacleModel o = obstacles[i];
                if (!o.Active || !o.Blocking) continue;

                float rx = o.Position.x - origin.x;
                float rz = o.Position.z - origin.z;
                float along = rx * direction.x + rz * direction.z;
                float perpSqr = rx * rx + rz * rz - along * along;
                float reach = shotRadius + o.Radius;
                if (perpSqr > reach * reach) continue;

                float t = along - Mathf.Sqrt(reach * reach - perpSqr);
                if (t < 0f)
                {
                    if (along < -reach) continue;
                    t = 0f;
                }

                if (t > maxDistance || t >= bestT) continue;
                bestT = t;
                best = o;
            }

            travel = best != null ? bestT : maxDistance;
            return best;
        }

        /// <summary>Collects every active obstacle whose centre is within radius of the blast centre.</summary>
        public static void Select(IReadOnlyList<ObstacleModel> obstacles, Vector3 center, float radius, List<ObstacleModel> result)
        {
            result.Clear();
            float limit = radius * radius;
            for (int i = 0; i < obstacles.Count; i++)
            {
                ObstacleModel o = obstacles[i];
                if (!o.Active) continue;
                float dx = o.Position.x - center.x;
                float dz = o.Position.z - center.z;
                if (dx * dx + dz * dz <= limit) result.Add(o);
            }
        }

        /// <summary>
        /// Smallest shot mass that clears every obstacle of the list with one shot flying along the route.
        /// The list holds the remaining blocking obstacles of one group. Returns +infinity if no mass up to maxMass is enough.
        /// </summary>
        public static float MinimumMass(IReadOnlyList<ObstacleModel> group, Vector3 origin, Vector3 direction, GameConfig config, float maxMass, List<ObstacleModel> scratch)
        {
            const float step = 0.005f;
            for (float mass = config.minShotMass; mass <= maxMass + 0.0001f; mass += step)
            {
                ObstacleModel first = FirstHit(origin, direction, config.ShotRadius(mass), group, 1000f, out float travel);
                // A small shot can slip past obstacles that sit off the aim line; a bigger one will touch them.
                if (first == null) continue;

                Vector3 center = origin + direction * travel;
                Select(group, center, config.InfectionRadius(mass), scratch);
                if (scratch.Count >= CountActive(group)) return mass;
            }

            return float.PositiveInfinity;
        }

        /// <summary>
        /// Cheapest total mass to clear a whole group with any number of shots fired one after another along the route.
        /// A tight cluster can be cheaper as several small shots than as one big one, so the loss check must allow it.
        /// Exhaustive over subsets of the group, which is tiny (a handful of obstacles).
        /// </summary>
        public static float MinimumTotalMass(IReadOnlyList<ObstacleModel> group, Vector3 origin, Vector3 direction, GameConfig config, float maxMass)
        {
            int n = group.Count;
            if (n == 0) return 0f;
            if (n > 16) return MinimumMass(group, origin, direction, config, maxMass, new List<ObstacleModel>());

            var memo = new float[1 << n];
            for (int i = 0; i < memo.Length; i++) memo[i] = -1f;
            return Solve((1 << n) - 1, group, origin, direction, config, maxMass, memo);
        }

        private static float Solve(int mask, IReadOnlyList<ObstacleModel> group, Vector3 origin, Vector3 direction, GameConfig config, float maxMass, float[] memo)
        {
            if (mask == 0) return 0f;
            if (memo[mask] >= 0f) return memo[mask];

            var remaining = new List<ObstacleModel>(group.Count);
            var bitOf = new List<int>(group.Count);
            for (int i = 0; i < group.Count; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                remaining.Add(group[i]);
                bitOf.Add(i);
            }

            float best = float.PositiveInfinity;
            const float step = 0.005f;
            for (float mass = config.minShotMass; mass <= maxMass + 0.0001f && mass < best; mass += step)
            {
                ObstacleModel first = FirstHit(origin, direction, config.ShotRadius(mass), remaining, 1000f, out float travel);
                if (first == null) continue;

                Vector3 center = origin + direction * travel;
                float radius = config.InfectionRadius(mass);
                float limit = radius * radius;
                int left = mask;
                for (int k = 0; k < remaining.Count; k++)
                {
                    float dx = remaining[k].Position.x - center.x;
                    float dz = remaining[k].Position.z - center.z;
                    if (dx * dx + dz * dz <= limit) left &= ~(1 << bitOf[k]);
                }

                if (left == mask) continue;
                float total = mass + Solve(left, group, origin, direction, config, maxMass, memo);
                if (total < best) best = total;
            }

            memo[mask] = best;
            return best;
        }

        private static int CountActive(IReadOnlyList<ObstacleModel> list)
        {
            int count = 0;
            for (int i = 0; i < list.Count; i++) if (list[i].Active) count++;
            return count;
        }
    }
}
