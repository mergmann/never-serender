
using System.Numerics;

namespace NeverSerender
{
    public class Util2
    {
        public static Vector2 ConvertVector(VRageMath.Vector2 value)
        {
            return new Vector2(value.X, value.Y);
        }

        public static Vector3 ConvertVector(VRageMath.Vector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        public static Matrix4x4 ConvertMatrix(VRageMath.Matrix value)
        {
            return new Matrix4x4
            (
                value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34,
                value.M41, value.M42, value.M43, value.M44
            );
        }
    }
}
