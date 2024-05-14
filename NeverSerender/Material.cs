using SharpGLTF.Materials;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NeverSerender
{
    public class Material
    {
        public Image<Rgba32> Color { get; set; }
        public Image<Rgb24> Normal { get; set; }
        public Image<Rgb24> RoughMetal { get; set; }
    }
}
