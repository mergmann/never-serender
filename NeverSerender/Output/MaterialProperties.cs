using System.Collections.Generic;

namespace NeverSerender.Output
{
    public class MaterialProperties
    {
        public string Name { get; set; }
        public RenderMode RenderMode { get; set; }
        public IDictionary<TextureKind, uint> Textures { get; set; }
    }
}