using SixLabors.ImageSharp.PixelFormats;
using System;
using VRageMath;
using VRageRender;

namespace NeverSerender
{
    // Port of PostprocessColorizeExportedTexture.hlsl
    public static class RenderTexture
    {
        private const float EPSILON = 1e-10f;

        private static float Saturate(float value) => Math.Max(0f, Math.Min(value, 1f));

        private static Vector2 Saturate(Vector2 value) => new Vector2(Saturate(value.X), Saturate(value.Y));

        private static Vector3 Saturate(Vector3 value) => new Vector3(Saturate(value.X), Saturate(value.Y), Saturate(value.Z));

        private static float Lerp(float a, float b, float s) => (1f - s) * a + s * b;

        private static float Frac(float value) => value - (float)Math.Truncate(value);

        private static Vector3 HueToRGB(float hue)
        {
            var r = Math.Abs(hue * 6f - 3f) - 1f;
            var g = 2 - Math.Abs(hue * 6f - 2f);
            var b = 2 - Math.Abs(hue * 6f - 4f);
            return Saturate(new Vector3(r, g, b));
        }

        private static Vector3 RGBToHSV(Vector3 rgb)
        {
            var k = new Vector4(0f, -1f / 3f, 2f / 3f, -1f);
            var p = Vector4.Lerp(new Vector4(rgb.Z, rgb.Y, k.W, k.Z), new Vector4(rgb.Y, rgb.Z, k.X, k.Y), rgb.Z >= rgb.Y ? 1f : 0f);
            var q = Vector4.Lerp(new Vector4(p.X, p.Y, p.W, rgb.X), new Vector4(rgb.X, p.Y, p.Z, p.X), p.X >= rgb.X ? 1f : 0f);
            var d = q.X - Math.Min(q.W, q.Y);
            return new Vector3(Math.Abs(q.Z + (q.W - q.Y) / (6.0f * d + EPSILON)), d / (q.X + EPSILON), q.X);
        }

        public static Vector3 Colorize1(Vector3 texcolor, Vector3 hsvmask, float coloring)
        {
            if (hsvmask.X == 0f && hsvmask.Y == -1f && hsvmask.Z == -1f)
                return texcolor;

            var coloringc = HueToRGB(hsvmask.X);
            var hsv = RGBToHSV(Vector3.Lerp(Vector3.One, coloringc, coloring) * texcolor);

            hsv.X = 0;
            var fhsv = hsv + hsvmask * new Vector3(1f, 1f, 0.5f);
            fhsv.X = Frac(fhsv.X);

            var gray2 = 1f - Saturate((hsvmask.Y + 1f) * 10f);
            var yz = Vector2.Lerp(
                Saturate(new Vector2(fhsv.Y, fhsv.Z)),
                Saturate(new Vector2(hsv.Y + hsvmask.Y, hsv.Z + hsvmask.Z)),
                gray2
            );
            fhsv.Y = yz.X;
            fhsv.Z = yz.Y;

            float gray3 = 1F - Saturate((hsvmask.Y + 0.9f) * 10f);
            fhsv.Y = Lerp(Saturate(fhsv.Y), Saturate(hsv.Y + hsvmask.Y), gray3);

            return Vector3.Lerp(texcolor, fhsv.HsvToRgb(), coloring);
        }

        public static Rgba32 Process(Rgba32 colorMetal, Rgba32 addMaps, Gray8 alphaMask, Vector3 colorize)
        {
            var inColor = new Vector3(colorMetal.R / 255f, colorMetal.G / 255f, colorMetal.B / 255f);
            var rgb = Colorize1(inColor, colorize, addMaps.A / 255f);
            var r = (byte)(rgb.X * 255);
            var g = (byte)(rgb.Y * 255);
            var b = (byte)(rgb.Z * 255);
            return new Rgba32(r, g, b, alphaMask.PackedValue);
        }
    }
}
