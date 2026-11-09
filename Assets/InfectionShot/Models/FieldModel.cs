using System.Collections.Generic;
using UnityEngine;

namespace InfectionShot
{
    /// <summary>All obstacles of the level and the queries the rules need.</summary>
    public sealed class FieldModel
    {
        private readonly List<ObstacleModel> _all;
        private readonly List<ObstacleModel> _groupBuffer = new List<ObstacleModel>(16);
        private readonly int _groupCount;

        public FieldModel(List<ObstacleModel> obstacles)
        {
            _all = obstacles;
            for (int i = 0; i < _all.Count; i++)
                if (_all[i].Blocking && _all[i].Group + 1 > _groupCount) _groupCount = _all[i].Group + 1;
        }

        public IReadOnlyList<ObstacleModel> All => _all;

        public void Reset()
        {
            for (int i = 0; i < _all.Count; i++) _all[i].Reactivate();
        }

        /// <summary>Distance along the route to the nearest remaining blocking obstacle.</summary>
        public bool TryGetNearestBlocking(PathModel path, out float distance)
        {
            distance = float.MaxValue;
            bool found = false;
            for (int i = 0; i < _all.Count; i++)
            {
                ObstacleModel o = _all[i];
                if (!o.Active || !o.Blocking) continue;
                float d = path.Project(o.Position);
                if (d < distance) distance = d;
                found = true;
            }

            return found;
        }

        /// <summary>Cheapest total shot mass needed to clear what is left of the route, one economical shot per remaining group.</summary>
        public float EstimateRemainingMass(PathModel path, GameConfig config, float maxMass)
        {
            float total = 0f;
            for (int g = 0; g < _groupCount; g++)
            {
                _groupBuffer.Clear();
                for (int i = 0; i < _all.Count; i++)
                {
                    ObstacleModel o = _all[i];
                    if (o.Active && o.Blocking && o.Group == g) _groupBuffer.Add(o);
                }

                if (_groupBuffer.Count == 0) continue;
                total += ShotMath.MinimumTotalMass(_groupBuffer, path.Start, path.Direction, config, maxMass);
                if (float.IsInfinity(total)) return total;
            }

            return total;
        }
    }
}
