using VRageMath;

namespace NeverSerender.Tools
{
    public static class ColorTools
    {
        public static Vector3UByte PackColorMask(Vector3 color)
        {
            var hue = (byte)MathHelper.Clamp(color.X * 255.0, 0.0, 255.0);
            var sat = (byte)MathHelper.Clamp((color.Y + 1.0) * 127.5, 0.0, 255.0);
            var val = (byte)MathHelper.Clamp((color.Z + 1.0) * 127.5, 0.0, 255.0);

            return new Vector3UByte(hue, sat, val);
        }

        public static Vector3UByte? PackColorMask(Vector3? color)
        {
            if (!color.HasValue)
                return null;

            return PackColorMask(color.Value);
        }
    }
}