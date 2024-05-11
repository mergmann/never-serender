using VRageMath;

namespace NeverSerender
{
    public static class Util
    {
        public static System.Numerics.Vector2 ConvertVector(Vector2 value)
        {
            return new System.Numerics.Vector2(value.X, value.Y);
        }

        public static System.Numerics.Vector3 ConvertVector(Vector3 value)
        {
            return new System.Numerics.Vector3(value.X, value.Y, value.Z);
        }

        public static System.Numerics.Matrix4x4 ConvertMatrix(Matrix value)
        {
            return new System.Numerics.Matrix4x4(
                value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34,
                value.M41, value.M42, value.M43, value.M44
            );
        }
    }
}
