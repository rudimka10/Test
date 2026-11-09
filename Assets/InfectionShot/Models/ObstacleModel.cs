using UnityEngine;

namespace InfectionShot
{
    /// <summary>One destructible obstacle. Blocking ones sit on the route and stop shots; the rest are scenery that can still be infected.</summary>
    public sealed class ObstacleModel
    {
        public ObstacleModel(int id, Vector3 position, float radius, int group, bool blocking)
        {
            Id = id;
            Position = position;
            Radius = radius;
            Group = group;
            Blocking = blocking;
            Active = true;
        }

        public int Id { get; }
        public Vector3 Position { get; }
        public float Radius { get; }
        public int Group { get; }
        public bool Blocking { get; }
        public bool Active { get; private set; }

        public void Deactivate() => Active = false;
        public void Reactivate() => Active = true;
    }
}
