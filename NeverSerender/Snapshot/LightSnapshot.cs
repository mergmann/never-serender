using System;
using VRageMath;

namespace NeverSerender.Snapshot
{
    public struct LightSnapshot : IEquatable<LightSnapshot>
    {
        public bool Remove { get; set; }
        public uint Id { get; set; }
        public MatrixD Matrix { get; set; }
        public Vector3 Color { get; set; }
        public float Intensity { get; set; }
        public Vector2? Cone { get; set; }

        public bool Equals(LightSnapshot other)
        {
            return Remove == other.Remove
                   && Id == other.Id
                   && Matrix.Equals(other.Matrix)
                   && Color.Equals(other.Color)
                   && Intensity.Equals(other.Intensity)
                   && Nullable.Equals(Cone, other.Cone);
        }

        public override bool Equals(object obj)
        {
            return obj is LightSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Remove.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Id;
                hashCode = (hashCode * 397) ^ Matrix.GetHashCode();
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                hashCode = (hashCode * 397) ^ Intensity.GetHashCode();
                hashCode = (hashCode * 397) ^ Cone.GetHashCode();
                return hashCode;
            }
        }
    }
}