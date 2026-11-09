using UnityEngine;

namespace InfectionShot
{
    /// <summary>The straight route from the start to the door on the X/Z plane. The player always stands on it, so the aim direction never changes.</summary>
    public sealed class PathModel
    {
        public PathModel(Vector3 start, Vector3 end)
        {
            Start = new Vector3(start.x, 0f, start.z);
            End = new Vector3(end.x, 0f, end.z);
            Vector3 delta = End - Start;
            Length = delta.magnitude;
            Direction = delta / Length;
        }

        public Vector3 Start { get; }
        public Vector3 End { get; }
        public Vector3 Direction { get; }
        public float Length { get; }

        public Vector3 PointAt(float distance) => Start + Direction * distance;

        public float Project(Vector3 point) => Vector3.Dot(point - Start, Direction);
    }
}
