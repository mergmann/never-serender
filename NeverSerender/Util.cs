using System;
using System.Reflection;
using VRage.Utils;
using VRageMath;

namespace NeverSerender
{
    public static class Util
    {
        public static readonly Assembly SystemNumericsVector;
        public static readonly Assembly SystemNumerics;
        public static readonly Type Vector2;
        //public static readonly ConstructorInfo Vector2C = null;
        public static readonly ConstructorInfo Vector2C;
        public static readonly Type Vector3;
        //public static readonly ConstructorInfo Vector3C = null;
        public static readonly ConstructorInfo Vector3C;
        public static readonly Type Matrix4x4;
        //public static readonly ConstructorInfo Matrix4x4C = null;
        public static readonly ConstructorInfo Matrix4x4C;
        //public static readonly PropertyInfo Matrix4x4Identity = Matrix4x4.GetProperty("Identity");

        public static object ConvertVector(Vector2 value)
        {
            return Vector2C.Invoke(new object[] { value.X, value.Y });
        }

        public static object ConvertVector(Vector3 value)
        {
            return Vector3C.Invoke(new object[] { value.X, value.Y, value.Z });
        }

        public static object ConvertMatrix(Matrix value)
        {
            return Matrix4x4C.Invoke(
                new object[] {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34,
                    value.M41, value.M42, value.M43, value.M44
                }
            );
        }

        static Util()
        {
            SystemNumericsVector = Assembly.LoadFrom("System.Numerics.Vectors_old.dll");
            SystemNumerics = Assembly.Load("System.Numerics");
            Vector2 = SystemNumerics.GetType("System.Numerics.Vector2");
            Vector2C = Vector2.GetConstructor(new Type[] { typeof(float), typeof(float) });
            Vector3 = SystemNumerics.GetType("System.Numerics.Vector3");
            Vector3C = Vector3.GetConstructor(new Type[] { typeof(float), typeof(float), typeof(float) });
            Matrix4x4 = SystemNumerics.GetType("System.Numerics.Matrix4x4");
            Matrix4x4C = Matrix4x4.GetConstructor(new Type[]
            {
                typeof(float), typeof(float), typeof(float), typeof(float),
                typeof(float), typeof(float), typeof(float), typeof(float),
                typeof(float), typeof(float), typeof(float), typeof(float),
                typeof(float), typeof(float), typeof(float), typeof(float)
            });
        }
    }
}
