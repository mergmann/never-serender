using SharpGLTF.Materials;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NeverSerender
{
    public class Material
    {
        public void ToGLTF()
        {
            new MaterialBuilder().WithChannelImage(KnownChannel.BaseColor, ImageBuilder.From(new byte[] { }));
        }

        public string Name { get; set; }
        public Image<Rgba32> Color { get; set; }
        public Image<Rgb24> Normal { get; set; }
        public Image<Rgb24> RoughMetal { get; set; }
    }
}
